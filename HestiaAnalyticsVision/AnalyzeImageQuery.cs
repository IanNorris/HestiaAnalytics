using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HestiaCore;
using System.Collections.Specialized;
using System.Net;
using System.IO;

namespace HestiaAnalyticsVision
{
    public class AnalyzeImageQuery
    {
		public enum VisualFeatures
		{
			Categories,     //Price tier 1? (unconfirmed)
			Tags,           //Price tier 1
			Description,	//Price tier 3
			Faces,			//Price tier 1
			ImageType,      //Price tier 1
			Color,          //Price tier 1
			Adult           //Price tier 2
		}

		public enum Details
		{
			Celebrities,
			Landmarks
		}

		public static SecureResponse CreateFromFile( SecureEndpoint Endpoint, VisualFeatures[] VisualFeatures, Details[] Details, string Filename, string Language = "en" )
		{
			byte[] FileData = File.ReadAllBytes( Filename );

			return CreateFromBytes( Endpoint, VisualFeatures, Details, FileData, Language );
		}

		public static SecureResponse CreateFromBytes(SecureEndpoint Endpoint, VisualFeatures[] VisualFeatures, Details[] Details, byte[] FileData, string Language = "en" )
		{
			string FeaturesString = string.Join( ",", VisualFeatures );
			string DetailsString = string.Join( ",", Details );

			return Endpoint.CreatePostRequestOctetStream( "/vision/v1.0/analyze", new NameValueCollection { { "visualFeatures", FeaturesString }, { "details", DetailsString }, { "language", Language } }, FileData );
		}
	}
}
