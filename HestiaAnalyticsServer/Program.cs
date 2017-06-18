using HestiaAnalyticsVision;
using HestiaCore;
using SighthoundAPI;
using System;
using System.Collections.Generic;

namespace HestiaAnalyticsServer
{
	class Program
	{
		static Settings Settings;
		static SightHoundListener SightHoundListener;
		static FrameExtractor FrameExtractor;

		static void Main(string[] args)
		{
			Settings = Settings.LoadSettings("HestiaAnalytics");

			SightHoundListener = new SightHoundListener(Settings.SighthoundAPI);
			FrameExtractor = new FrameExtractor();


			SightHoundListener.OnCameraStatusChanged += (name, oldEnabled, enabled, oldStatus, status, firstTime) =>
			{
				string OldEnabledString = oldEnabled ? "enabled" : "disabled";
				string NewEnabledString = enabled ? "enabled" : "disabled";
				if( firstTime )
				{
					System.Console.Out.WriteLine($"Camera \"{name}\" was found, now {NewEnabledString} and {status}.");
				}
				else
				{
					System.Console.Out.WriteLine($"Camera \"{name}\" was {OldEnabledString} and {oldStatus}, now {NewEnabledString} and {status}." );
				}
			};

			SightHoundListener.OnNewClip += (Clip clip) =>
			{
				var ClipUnixTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				ClipUnixTime = ClipUnixTime.AddMilliseconds(clip.Start.UnixTime);
				ClipUnixTime = ClipUnixTime.ToLocalTime();

				System.Console.Out.WriteLine($"Camera \"{clip.CameraName}\" had activity at {ClipUnixTime.ToString("t")} for {Timestamp.GetFriendlyDelta(clip.Start, clip.End)}.");
			};

			SightHoundListener.OnDownloadClip += (Clip clip) =>
			{
				System.Console.Out.WriteLine($"Downloading clip from camera \"{clip.CameraName}\" at {clip.FriendlyTime}...");
			};

			SightHoundListener.OnDownloadedClip += (Clip clip, long ClipStart, long ClipSubIndex, string Filename) =>
			{
				System.Console.Out.WriteLine($"Downloaded clip from camera \"{clip.CameraName}\" at {clip.FriendlyTime} to {Filename}.");
			};

			SightHoundListener.Start();

			SightHoundListener.Wait();
			
			//Never gets here
			SightHoundListener.Stop();


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




		}
	}
}
