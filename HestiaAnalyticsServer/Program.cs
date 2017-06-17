using HestiaCore;
using SighthoundAPI;
using System.Collections.Generic;

namespace HestiaAnalyticsServer
{
	class Program
	{
		static Settings Settings;

		static void Main(string[] args)
		{
			Settings = Settings.LoadSettings("HestiaAnalytics");

			/*var Endpoint = new SecureEndpoint(
				Settings.AzureCognitiveAPI.RegionName + ".api.cognitive.microsoft.com",
				Settings.AzureCognitiveAPI.CertificateAuthorityThumbprint 
			);
			Endpoint.AddHeader("Ocp-Apim-Subscription-Key", Settings.AzureCognitiveAPI.SubscriptionKey);

			//var X = HestiaAnalyticsVision.GenerateThumbnailQuery.CreateFromFile( Endpoint, 256, 256, true, @"W:\Dropbox\Temp\VisionTests\vlcsnap-error925.jpg");
			var X = HestiaAnalyticsVision.AnalyzeImageQuery.CreateFromFile(
				Endpoint, 
				new AnalyzeImageQuery.VisualFeatures[]
				{
					AnalyzeImageQuery.VisualFeatures.Categories,
					AnalyzeImageQuery.VisualFeatures.Tags,
					AnalyzeImageQuery.VisualFeatures.Description,
					AnalyzeImageQuery.VisualFeatures.Faces,
					AnalyzeImageQuery.VisualFeatures.Color,
					AnalyzeImageQuery.VisualFeatures.Adult
				}, 
				new AnalyzeImageQuery.Details[]
				{
					AnalyzeImageQuery.Details.Celebrities,
					AnalyzeImageQuery.Details.Landmarks
				},
				//@"W:\Dropbox\Temp\VisionTests\vlcsnap-error732.jpg" 
				//@"W:\Dropbox\Temp\VisionTests\vlcsnap-error880-sm.jpg"
				@"W:\car.jpg"
			);

			//X.WriteToFile( @"W:\Output.jpg" );
			X.WriteToFile(@"W:\Output.json");*/

			var Endpoint = new SecureEndpoint( Settings.SighthoundAPI.Hostname, Settings.SighthoundAPI.CertificateAuthorityThumbprint, Settings.SighthoundAPI.Port );
			Endpoint.AddBasicAuthenticationHeader( Settings.SighthoundAPI.Username, Settings.SighthoundAPI.Password );

			/*var Response = SightHoundVideoListener.SighthoundRPC.SendRPCCommand(Endpoint, "remoteGetClipsForRule", new System.Collections.Generic.List<SightHoundVideoListener.SighthoundRPCValue>
			{
				new SighthoundRPCValue("Front Door"), //camera
				new SighthoundRPCValue("People"), //rule
				new SighthoundRPCValue(1497310149.982), //when
				new SighthoundRPCValue(25), //a
				new SighthoundRPCValue(0), //b
				new SighthoundRPCValue(false), //oldestFirst
			});*/

			/*ExpandoObject objectIds = new { objectIds = new int[] { 8456, 8455 } }.CreateExpando();

			var Response = SightHoundVideoListener.SighthoundRPC.SendRPCCommand(Endpoint, "remoteGetClipUri", new System.Collections.Generic.List<SightHoundVideoListener.SighthoundRPCValue>
			{
				new SighthoundRPCValue("Front Door"), //camera
				new SighthoundRPCValue( new int[]{ 1497364274, 51 } ), //start time (and ms)
				new SighthoundRPCValue( new int[]{ 1497364509, 0} ), //start time (and ms)
				new SighthoundRPCValue( 804 ), //URL
				new SighthoundRPCValue( "video/h264" ), //type
				new SighthoundRPCValue( objectIds ), //type
			});

			Response.Wait();*/

			{
				var ResponsePromise = Methods.GetCameraNames(Endpoint);
				var Response = ResponsePromise.Get();
			}

			{
				var ResponsePromise = Methods.GetRulesForCamera(Endpoint, new GetRulesForCameraRequest { CameraName = "Front Door" } );
				var Response = ResponsePromise.Get();
			}

			{
				var Request = new GetClipsForRuleRequest();
				Request.CameraName = "Front Door";
				Request.RuleName = "People";
				Request.From = 1497364274.0;
				Request.Count = 25;
				Request.Page = 0;
				Request.OldestFirst = false;

				var ResponsePromise = Methods.GetRulesForCamera(Endpoint, Request);
				var Response = ResponsePromise.Get();
			}

			{
				GetClipUriRequest Request = new GetClipUriRequest();
				Request.CameraName = "Front Door";
				Request.Start.Seconds = 1497364274;
				Request.Start.Milliseconds = 51;
				Request.End.Seconds = 1497364509;
				Request.End.Milliseconds = 0;
				Request.UriId = 804;
				Request.ContentType = "video/h264";
				Request.Objects.ObjectIdArray = new List < int > { 8456, 8455 };

				var ResponsePromise = Methods.GetClipUri( Endpoint, Request );
				var Response = ResponsePromise.Get();
			}

			
		}
	}
}
