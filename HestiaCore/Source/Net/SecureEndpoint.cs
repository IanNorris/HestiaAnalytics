using System;
using System.Collections.Specialized;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Net;
using System.Web;
using System.IO;
using System.Text;

namespace HestiaCore
{
	public class SecureEndpoint
    {
		public SecureEndpoint( string Hostname, string CertificateAuthorityThumbprint, uint Port = 443, bool Secure = true )
		{
			this.Hostname = Hostname;
			this.Port = Port;
			this.CertificateAuthorityThumbprint = CertificateAuthorityThumbprint.Replace( " ", "" );
			this.Headers = new NameValueCollection();
			this.Timeout = 0;
			this.Secure = Secure;
		}
		
		public void AddHeader( string Key, string Value )
		{
			Headers.Add( Key, Value );
		}

		public void SetTimeout( int Timeout )
		{
			this.Timeout = Timeout;
		}

		public void ResetTimeoutToDefault()
		{
			this.Timeout = 0;
		}

		public void AddBasicAuthenticationHeader( string Username, string Password )
		{
			String EncodedAuth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(Username + ":" + Password));
			AddHeader("Authorization", "Basic " + EncodedAuth);
		}

		private HttpWebRequest PrepareRequest( string URI, NameValueCollection QueryString )
		{
			UriBuilder Uri = new UriBuilder( Secure ? "https" : "http", Hostname, (int)Port, URI);

			if (QueryString != null)
			{
				var QueryStringBuilder = HttpUtility.ParseQueryString(string.Empty);
				for (int QS = 0; QS < QueryString.Count; QS++)
				{
					QueryStringBuilder.Add(QueryString.GetKey(QS), QueryString.Get(QS));
				}

				Uri.Query = QueryStringBuilder.ToString();
			}

			HttpWebRequest WebRequest = HttpWebRequest.CreateHttp(Uri.Uri);
			WebRequest.ServerCertificateValidationCallback = CheckCertificateThumbprints;
			WebRequest.Headers.Add(Headers);

			if( Timeout > 0 )
			{
				WebRequest.Timeout = Timeout;
			}

			return WebRequest;
		}

		public SecureResponse CreateGetRequest( string URI, NameValueCollection QueryString )
		{
			HttpWebRequest WebRequest = PrepareRequest( URI, QueryString );
			WebRequest.Method = WebRequestMethods.Http.Get;
			
			return new SecureResponse(WebRequest.GetResponseAsync());
		}

		public SecureResponse CreatePostRequestOctetStream( string URI, NameValueCollection QueryString, byte[] PostData )
		{
			HttpWebRequest WebRequest = PrepareRequest( URI, QueryString );
			WebRequest.Method = WebRequestMethods.Http.Post;

			WebRequest.ContentType = "application/octet-stream";
			WebRequest.ContentLength = PostData.Length;

			using (var Stream = WebRequest.GetRequestStream())
			{
				Stream.Write(PostData, 0, PostData.Length);
			}

			return new SecureResponse( WebRequest.GetResponseAsync() );
		}

		public SecureXMLRPCResponse<T> CreatePostXMLRPC<T>(string URI, NameValueCollection QueryString, string Data )
		{
			HttpWebRequest WebRequest = PrepareRequest(URI, QueryString);
			WebRequest.Method = WebRequestMethods.Http.Post;

			byte[] Bytes = Encoding.ASCII.GetBytes(Data);

			WebRequest.ContentType = "text/xml";
			WebRequest.ContentLength = Bytes.Length;

			

			using (var Stream = WebRequest.GetRequestStream())
			{
				Stream.Write( Bytes, 0, Bytes.Length );
			}

			return new SecureXMLRPCResponse<T>(WebRequest.GetResponseAsync());
		}

		private bool CheckCertificateThumbprints(object Sender, X509Certificate Certificate, X509Chain Chain, SslPolicyErrors SSLPolicyErrors)
		{
			foreach( var Cert in Chain.ChainElements )
			{
				if( String.Equals( Cert.Certificate.Thumbprint, CertificateAuthorityThumbprint, StringComparison.InvariantCultureIgnoreCase ) )
				{
					return true;
				}
			}
			
			return false;
		}

		private string Hostname;
		private string CertificateAuthorityThumbprint;
		private NameValueCollection Headers;
		private uint Port;
		private int Timeout;
		private bool Secure;
	}
}
