using System;
using System.Text;
using System.Xml;
using HestiaCore;
using HestiaCore.Source.Serialization;

namespace SighthoundAPI
{
	public class SighthoundRPC
	{
		public static SecureXMLRPCResponse<T> SendRPCCommand<T>( SecureEndpoint Endpoint, string Method, Object Data )
		{
			StringBuilder SB = new StringBuilder();
			using( XmlWriter Doc = XmlWriter.Create( SB, new XmlWriterSettings { Encoding = Encoding.ASCII, Indent = true, OmitXmlDeclaration = true } ) )
			{
				Doc.WriteStartDocument();
				{
					Doc.WriteStartElement( "methodCall" );
					{
						Doc.WriteStartElement( "methodName" );
						{
							Doc.WriteValue( Method );
						}
						Doc.WriteEndElement();

						Doc.WriteStartElement( "params" );
						if( Data != null )
						{
							XmlSerializer.ObjectToXMLRPC( Doc, Data );
						}
						Doc.WriteEndElement();
					}
					Doc.WriteEndElement();
				}
				Doc.WriteEndDocument();
			}

			string StringOut = SB.ToString();

			return Endpoint.CreatePostXMLRPC<T>( "/xmlrpc/", null, StringOut );
		}
	}
}
