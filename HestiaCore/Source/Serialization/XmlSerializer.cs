using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.XPath;
using System.Reflection;
using System.Xml;

namespace HestiaCore.Source.Serialization
{
	[IsRemoteStructAttribute]
	public class Fault : Exception
	{
		public Fault() : base() {}

		public Fault( int Code, string Desc ) : base()
		{
			FaultCode = Code;
			Description = Desc;
		}

		[RemoteParameterNameAttribute("faultCode")]
		public int FaultCode;

		[RemoteParameterNameAttribute("faultString")]
		public string Description;

		public override string Message
		{
			get
			{
				return $"{FaultCode}: {Description}";
			}
		}
	}

	[AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct)]
	public class IsRemoteArrayAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class IsRemoteStructAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Field)]
	public class RemoteParameterNameAttribute : Attribute
	{
		public RemoteParameterNameAttribute( string Name )
		{
			this.Name = Name;
		}

		public string Name { get; }
	}

	public static class XmlSerializer
	{
		private static object XPathNavigatorToObject(XPathNavigator Child)
		{
			if( Child.Name == "value")
			{
				Child.MoveToFirstChild();
			}

			if (Child.Name == "array")
			{
				XPathNavigator ArrayNavigator = Child.CreateNavigator();
				XPathNodeIterator ArrayIterator = ArrayNavigator.Select("data/value");

				return XPathIteratorToList(ArrayIterator);
			}
			else if (Child.Name == "struct")
			{
				XPathNavigator ArrayNavigator = Child.CreateNavigator();
				XPathNodeIterator ArrayIterator = ArrayNavigator.Select("member/value");

				return XPathIteratorToList(ArrayIterator);
			}
			else if (Child.Name == "string")
			{
				return Child.Value;
			}
			else if (Child.Name == "int")
			{
				return int.Parse(Child.Value);
			}
			else if (Child.Name == "double")
			{
				return double.Parse(Child.Value);
			}
			else if (Child.Name == "boolean")
			{
				return (bool)(Child.Value == "1" ? true : false);
			}
			else if (Child.Name == "timeDate.iso8601")
			{
				return DateTime.Parse(Child.Value);
			}
			else if(Child.Name == "nil")
			{
				return null;
			}

			throw new Exception($"Unrecognized type {Child.Name}");
		}

		private static List<object> XPathIteratorToList( XPathNodeIterator ChildrenIter )
		{
			List<object> Children = new List<object>();
			
			while (ChildrenIter.MoveNext())
			{
				ChildrenIter.Current.MoveToFirstChild();
				Children.Add( XPathNavigatorToObject( ChildrenIter.Current ) );
			}

			return Children;
		}

		public static List<object> ObjectsFromXMLRPC(Stream XMLStream)
		{
			XPathDocument Document = new XPathDocument(XMLStream);
			XPathNavigator Navigator = Document.CreateNavigator();

			Navigator.MoveToRoot();
			Navigator.MoveToFirstChild();
			
			XPathNodeIterator PathIter = Navigator.Select("params/param/value/array/data/value");
			if( PathIter.Count > 0 )
			{
				return XPathIteratorToList( PathIter );
			}
			else
			{
				XPathNodeIterator ValueIter = Navigator.Select("params/param/value");

				if (ValueIter.Count > 0)
				{
					ValueIter.MoveNext();

					List<object> Children = new List<object>();

					Children.Add(XPathNavigatorToObject(ValueIter.Current));
					return Children;
				}
				else
				{
					XPathNodeIterator FaultIter = Navigator.Select("fault");

					FaultIter.MoveNext(); //method
					FaultIter.Current.MoveToFirstChild(); //fault
					FaultIter.Current.MoveToFirstChild(); //value

					throw ListOfObjectsToObject<Fault>( (List<object>)XPathNavigatorToObject(FaultIter.Current));
				}
			}
		}

		public static TOutput NullConverter<TInput, TOutput>( object input )
		{
			return (TOutput)input;
		}


		public static T ListOfObjectsToObject<T>(List<object> Data)
		{
			int DataIndex = 0;

			Type Type = typeof(T);
			T Result = (T)Activator.CreateInstance(Type);

			//This is necessary for structs otherwise SetValue doesn't work.
			object BoxedResult = Result;

			var Fields = Type.GetFields(BindingFlags.Instance | BindingFlags.Public);
			foreach (FieldInfo Field in Fields)
			{
				if( DataIndex >= Data.Count )
				{
					if(		Data.Count == 2 
						&&	(Data[0].GetType() == typeof(bool)) 
						&&	(Data[1].GetType() == typeof(string)))
					{
						bool? Success = (bool)Data[0];
						if( ((string)Data[1]).Contains("exception") )
						{
							throw new Exception( (string)Data[1] );
						}
					}

					throw new Exception($"Not enough data available to fille {Type.Name}. There are {Fields.Length} but only {Data.Count} were provided.");
				}

				Type FieldType = Field.FieldType;
				object FieldData = Data[DataIndex];
				if(FieldData == null )
				{
					Field.SetValue( BoxedResult, null );
					DataIndex++;
					continue;
				}

				//Special case for nullable types. 
				if ( FieldType.IsGenericType && FieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					FieldType = FieldType.GetGenericArguments()[0];
				}

				if (FieldType == FieldData.GetType())
				{
					Field.SetValue( BoxedResult, FieldData );
				}
				else if( FieldData.GetType() == typeof(List<object>) )
				{
					if( FieldType.IsGenericType && FieldType.GetGenericTypeDefinition() == typeof(List<>) )
					{
						MethodInfo AddMethod = typeof(List<>).MakeGenericType( new Type[] { FieldType.GetGenericArguments()[0] } ).GetMethod("Add");
						var NewList = Activator.CreateInstance(FieldType);

						var ListData = (List<object>)FieldData;
						foreach( var Item in ListData)
						{
							if (Item.GetType().IsGenericType && Item.GetType().GetGenericTypeDefinition() == typeof(List<>))
							{
								MethodInfo ToStructMethod = typeof(XmlSerializer).GetMethod("ListOfObjectsToObject").MakeGenericMethod(new Type[] { FieldType.GetGenericArguments()[0] });
								object StructResult = ToStructMethod.Invoke(null, new object[] { Item });

								AddMethod.Invoke(NewList, new object[] { StructResult } );
							}
							else
							{
								AddMethod.Invoke(NewList, new object[] { Item });
							}
						}
					
						Field.SetValue( BoxedResult, NewList);
					}
					else
					{
						MethodInfo ToStructMethod = typeof(XmlSerializer).GetMethod("ListOfObjectsToObject").MakeGenericMethod(new Type[] { FieldType });
						object StructResult = ToStructMethod.Invoke(null, new object[] { FieldData });

						Field.SetValue(BoxedResult, StructResult);
					}
				}
				else
				{
					throw new Exception($"Member {Field.Name} expected type {Field.FieldType.Name} but got {FieldData.GetType().Name}.");
				}


				DataIndex++;
			}

			if( DataIndex != Fields.Length )
			{
				throw new Exception($"Not all parameters of {Type.Name} were filled. They need to be public.");
			}

			return (T)BoxedResult;
		}

		public static T XMLRPCToObject<T>( Stream XMLStream )
		{
			List<object> Data = ObjectsFromXMLRPC( XMLStream );

			if( Data.Count == 0 || (Data.Count == 1 && Data[0] == null) )
			{
				return default(T);
			}

			return ListOfObjectsToObject<T>( Data );
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		private static string TypeToXMLRPCTypeName( Type Type )
		{
			if( Type == typeof(int) ) { return "int"; }
			if( Type == typeof(double) ) { return "double"; }
			if( Type == typeof(string) ) { return "string"; }
			if (Type == typeof(bool)) { return "boolean"; }

			throw new Exception($"Unrecognised type {Type.Name}");
		}

		private static void ObjectToXMLRPCParameter<T>(XmlWriter Writer, T Object)
		{
			Type ObjectType = Object.GetType();

			Writer.WriteStartElement("value");

			if( ObjectType.GetCustomAttribute<IsRemoteStructAttribute>() != null )
			{
				var Fields = ObjectType.GetFields(BindingFlags.Instance | BindingFlags.Public);

				Writer.WriteStartElement("struct");

				foreach (FieldInfo Field in Fields)
				{
					object FieldValue = Field.GetValue(Object);

					Writer.WriteStartElement("member");

					var NameAttrib = Field.GetCustomAttribute<RemoteParameterNameAttribute>();
					if( NameAttrib != null )
					{
						Writer.WriteStartElement("name");
						Writer.WriteValue( NameAttrib.Name );
						Writer.WriteEndElement();
					}

					ObjectToXMLRPCParameter(Writer, FieldValue);
					Writer.WriteEndElement();
				}

				Writer.WriteEndElement();
			}
			else if (ObjectType.GetCustomAttribute<IsRemoteArrayAttribute>() != null)
			{
				var Fields = ObjectType.GetFields(BindingFlags.Instance | BindingFlags.Public);

				Writer.WriteStartElement("array");
				Writer.WriteStartElement("data");
				{
					foreach (FieldInfo Field in Fields)
					{
						object FieldValue = Field.GetValue(Object);

						ObjectToXMLRPCParameter(Writer, FieldValue);
					}
				}
				Writer.WriteEndElement();
				Writer.WriteEndElement();
			}
			else if (ObjectType.IsGenericType && ObjectType.GetGenericTypeDefinition() == typeof(List<>))
			{
				Writer.WriteStartElement("array");
				Writer.WriteStartElement("data");

				foreach (var V in (System.Collections.IEnumerable)Object)
				{
					ObjectToXMLRPCParameter(Writer, V);
				}
				Writer.WriteEndElement();
				Writer.WriteEndElement();
			}
			else
			{
				Writer.WriteStartElement(TypeToXMLRPCTypeName(ObjectType));

				if( Object.GetType() == typeof(bool) )
				{
					Writer.WriteValue( Convert.ToBoolean(Object) ? "1" : "0" );
				}
				else
				{
					Writer.WriteValue( Object );
				}

				Writer.WriteEndElement();
			}

			Writer.WriteEndElement();
		}

		public static void ObjectToXMLRPC<T>( XmlWriter Writer, T Object )
		{
			Type ObjectType = Object.GetType();
			var Fields = ObjectType.GetFields( BindingFlags.Instance | BindingFlags.Public );

			foreach (FieldInfo Field in Fields)
			{
				object FieldValue = Field.GetValue(Object);

				Writer.WriteStartElement("param");
				ObjectToXMLRPCParameter(Writer, FieldValue);
				Writer.WriteEndElement();
			}
		}
	}
}
