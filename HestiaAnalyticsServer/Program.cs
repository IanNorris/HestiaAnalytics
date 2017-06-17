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

			List<string> CameraNames;

			{
				var ResponsePromise = Sighthound.GetCameraNames(Endpoint);
				var Response = ResponsePromise.Get();
				CameraNames = Response.CameraNames;
			}

			{
				var ResponsePromise = Sighthound.GetRulesForCamera(Endpoint, "Front Door" );
				var Response = ResponsePromise.Get();
			}

			{
				var ResponsePromise = Sighthound.EnableRule(Endpoint, "People in Front Door", true);
				var Response = ResponsePromise.Get();
			}

			{
				var ResponsePromise = Sighthound.EnableCamera(Endpoint, "Front Door", true);
				var Response = ResponsePromise.Get();
			}

			{
				var ResponsePromise = Sighthound.GetLiveCameras(Endpoint);
				var Response = ResponsePromise.Get();
			}

			{
				var ResponsePromise = Sighthound.GetLiveCameraUri(Endpoint, "Front Door", VideoType.Jpeg );
				var Response = ResponsePromise.Get();
			}

			{
				var ResponsePromise = Sighthound.GetRuleInfo(Endpoint, "Unknown objects outside Bush in Front Door");
				var Response = ResponsePromise.Get();
			}

			{
				var Request = new GetClipsForRuleRequest();
				Request.CameraName = "Front Door";
				Request.RuleName = "Unknown objects outside Bush in Front Door";
				Request.From = 1497722643.242;
				Request.Count = 25;
				Request.Page = 0;
				Request.OldestFirst = false;

				var ResponsePromise = Sighthound.GetClipsForRule(Endpoint, Request);
				var Response = ResponsePromise.Get();

				{
					List<ThumbnailClipRequest> Clips = new List<ThumbnailClipRequest>();
					
					foreach( var Clip in Response.Clips )
					{
						ThumbnailClipRequest NewClip = new ThumbnailClipRequest();
						NewClip.CameraName = Clip.CameraName;
						NewClip.ThumbnailTime = Clip.ThumbnailTime;
						Clips.Add(NewClip);
					}

					var ResponsePromiseq = Sighthound.GetClipThumbnails(Endpoint, Clips, 320, 240 );
					var Responseq = ResponsePromiseq.Get();
				}
			}

			{
				foreach(var Cam in CameraNames)
				{
					var ResponsePromise = Sighthound.GetCameraStatus(Endpoint, Cam);
					var Response = ResponsePromise.Get();
				}
			}

			{
				foreach (var Cam in CameraNames)
				{
					var ResponsePromise = Sighthound.GetCameraStatus(Endpoint, Cam);
					var Response = ResponsePromise.Get();
				}
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

				var ResponsePromise = Sighthound.GetClipUri( Endpoint, Request );
				var Response = ResponsePromise.Get();
			}


		}
	}
}
