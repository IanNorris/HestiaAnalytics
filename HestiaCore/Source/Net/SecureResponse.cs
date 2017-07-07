using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HestiaCore.Source.Serialization;

namespace HestiaCore
{
	public class SecureResponse
	{
		public SecureResponse( System.Threading.Tasks.Task<System.Net.WebResponse> Task )
		{
			this.Response = Task;
		}

		public void Wait()
		{
			if( !Response.IsCompleted )
			{
				Response.Wait();
			}
		}

		public WebResponse GetResponse()
		{
			return Response.Result;
		}

		public void WriteToFile( string Filename )
		{
			Wait();

			using( MemoryStream MemStream = new MemoryStream() )
			{
				Response.Result.GetResponseStream().CopyTo( MemStream );

				File.WriteAllBytes(Filename, MemStream.ToArray());
			}
		}

		public string GetAsString()
		{
			Wait();

			using (MemoryStream MemStream = new MemoryStream())
			{
				Response.Result.GetResponseStream().CopyTo(MemStream);

				return Encoding.ASCII.GetString( MemStream.GetBuffer() );
			}
		}

		public T ConvertXMLRPCToObject<T>()
		{
			Wait();

			return XmlSerializer.XMLRPCToObject<T>(GetResponse().GetResponseStream());
		}

		private System.Threading.Tasks.Task<System.Net.WebResponse> Response;
	}

	public class SecureXMLRPCResponse<T> : SecureResponse
	{
		public SecureXMLRPCResponse(System.Threading.Tasks.Task<System.Net.WebResponse> Task ) : base(Task ){}

		public T Get()
		{
			return ConvertXMLRPCToObject<T>();
		}
	}
}
