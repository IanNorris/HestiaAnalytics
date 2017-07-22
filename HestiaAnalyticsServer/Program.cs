using HestiaAnalyticsVision;
using HestiaCore;
using Microsoft.ProjectOxford.Vision.Contract;
using Newtonsoft.Json;
using SighthoundAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace HestiaAnalyticsServer
{
	class Program
	{
		static Settings Settings;
		static SightHoundListener SightHoundListener;
		static FrameExtractor FrameProcessor;

		static int Index = 0;

		static void Main(string[] args)
		{
			Settings = Settings.LoadSettings("HestiaAnalytics");

			FrameProcessor = new FrameExtractor(Settings.MotionAnalysis);

			var CognitiveEndpoint = new SecureEndpoint(
					Settings.AzureCognitiveAPI.RegionName + ".api.cognitive.microsoft.com",
					Settings.AzureCognitiveAPI.CertificateAuthorityThumbprint
				);
			CognitiveEndpoint.AddHeader("Ocp-Apim-Subscription-Key", Settings.AzureCognitiveAPI.SubscriptionKey);

			/*List<string> InputFiles = new List<string>();
			foreach( var Filename in Directory.EnumerateFiles(@"W:\VideoOut", "*.json") )
			{
				InputFiles.Add(File.ReadAllText(Filename));
			}
			
			var ResultOutput = MergeClipJsonResults.MergeAndFilter( InputFiles, Settings.Filter );*/

			/*using (var TextWriter = new StringWriter())
			using (var Writer = new Newtonsoft.Json.JsonTextWriter(TextWriter))
			{
				var Serializer = Newtonsoft.Json.JsonSerializer.Create();

				Serializer.Serialize(Writer, ResultOutput);
				System.Console.WriteLine(TextWriter.GetStringBuilder().ToString());
			}

			string ResultString = JsonConvert.SerializeObject(ResultOutput, Formatting.Indented);
			System.Console.WriteLine(ResultString);

			return;*/

			bool PersonFound = false;
			string ClipCameraName = "";
			int APICallCount = 0;
			List<AnalysisResult> ClipResults = new List<AnalysisResult>();

			FrameProcessor.OnClipStart += (string Filename) =>
			{
				PersonFound = false;
				System.Console.WriteLine( $"New clip processing started for {Filename}" );
				ClipResults.Clear();
				APICallCount = 0;
			};

			FrameProcessor.OnClipEnd += (string Filename) =>
			{
				if( ClipResults.Count > 0 )
				{
					var Result = MergeClipJsonResults.MergeAndFilter(ClipResults, Settings.Filter);

					string ResultString = JsonConvert.SerializeObject(Result, Formatting.Indented);
					System.Console.WriteLine(ResultString);

					System.Console.WriteLine($"Clip processing finished, {APICallCount} API calls made.");
				}
				else
				{
					System.Console.WriteLine($"Clip processing finished, no motion detected.");
				}
			};

			FrameProcessor.OnMotionDetectedFrame += (Bitmap frame, System.Drawing.Rectangle rect, long frameTimestamp) =>
			{
				var Result = MultiStageImageClassificationQuery.ClassifyImage( Settings.Filter, CognitiveEndpoint, @"W:\VideoOut", $"{ClipCameraName}_{frameTimestamp}", frame, rect, frameTimestamp);

				ClipResults.Add( Result.Result );

				APICallCount += Result.QueriesMade;
				
				if( Result.ContainsPerson && !PersonFound)
				{
					PersonFound = true;

					System.Console.Out.WriteLine($"Person found in camera {ClipCameraName}");
				}
			};

			/*Stopwatch timer = new Stopwatch();
			timer.Start();

			FrameExtractor FE = new FrameExtractor(@"W:\SighthoundClips\2017_06_13\07_18_54_00.mp4");

			var Endpoint = new SecureEndpoint(
				Settings.AzureCognitiveAPI.RegionName + ".api.cognitive.microsoft.com",
				Settings.AzureCognitiveAPI.CertificateAuthorityThumbprint
			);
			Endpoint.AddHeader("Ocp-Apim-Subscription-Key", Settings.AzureCognitiveAPI.SubscriptionKey);

			int Index = 0;
			foreach (Bitmap bmp in FE.ObjectFrames)
			{
				using (MemoryStream MS = new MemoryStream())
				{
					bmp.Save(MS, ImageFormat.Jpeg);


					var X = HestiaAnalyticsVision.AnalyzeImageQuery.CreateFromBytes(
						Endpoint,
						new AnalyzeImageQuery.VisualFeatures[]
						{
							AnalyzeImageQuery.VisualFeatures.Tags,
							AnalyzeImageQuery.VisualFeatures.Faces
						},
						new AnalyzeImageQuery.Details[] { },
						MS.ToArray()
					);

					X.WriteToFile($"W:\\VideoOut\\Result_{Index++}.json");
				}
			}

			timer.Stop();

			return;*/

			SightHoundListener = new SightHoundListener(Settings.SighthoundAPI);
			
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
				
				ClipCameraName = clip.CameraName;
				FrameProcessor.Run( Filename, ClipStart );
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
