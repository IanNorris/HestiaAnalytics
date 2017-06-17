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
    public class GenerateThumbnailQuery
    {
		public static SecureResponse CreateFromFile( SecureEndpoint Endpoint, int DesiredWidth, int DesiredHeight, bool SmartCrop, string Filename )
		{
			byte[] FileData = File.ReadAllBytes( Filename );

			return CreateFromBytes(Endpoint, DesiredWidth, DesiredHeight, SmartCrop, FileData);
		}

		public static SecureResponse CreateFromBytes(SecureEndpoint Endpoint, int DesiredWidth, int DesiredHeight, bool SmartCrop, byte[] FileData )
		{
			return Endpoint.CreatePostRequestOctetStream("/vision/v1.0/generateThumbnail", new NameValueCollection { { "width", DesiredWidth.ToString() }, { "height", DesiredHeight.ToString() }, { "smartCropping", SmartCrop.ToString() } }, FileData);
		}
	}
}
