using ManyConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HestiaAnalyticsVision;
using System.Drawing;
using System.IO;
using System.Diagnostics;

namespace HestiaAnalyticsCLI
{
	public class ExtractObjectFrames : ConsoleCommand
	{
		public string InputFilePath { get; set; }

		public ExtractObjectFrames()
		{
			IsCommand("ExtractObjectFrames", "Extract a set of frames of any moving objects in the video.");

			HasRequiredOption("i|input=", "The path of the input video file.", p => InputFilePath = p );
		}

		public override int Run(string[] RemainingArguments)
		{
			/*int Index = 0;
			(FrameExtractor FE = new FrameExtractor("rtsp://192.168.85.48");

			FE.OnMotionDetectedFrame += (image, rect) =>
			{
				image.Save($"W:\\VideoOut\\Cropped_{Index++}.jpg");
			};

			while( true )
			{
				System.Threading.Thread.Sleep(100);
			}

			Stopwatch overallWatch = new Stopwatch();*/

			/*overallWatch.Start();

			int Index = 0;

			var Files = Directory.EnumerateFiles(@"W:\SighthoundClips\2017_06_18", "*.mp4");
			int Total = Files.Count();
			foreach (var File in Files)
			{
				if( Index < 372 )
				{
					Index++;
					continue;
				}

				int SubIndex = 0;

				System.Console.Out.Write( $"{Index}/{Total} - {File}");

				Stopwatch watch = new Stopwatch();

				watch.Start();
				FrameExtractor FE = new FrameExtractor(File);
				
				foreach (Bitmap bmp in FE.ObjectFrames)
				{
					bmp.Save($"W:\\VideoOut\\Cropped_{Index}_{SubIndex}.jpg");
					SubIndex++;
				}

				watch.Stop();

				System.Console.Out.WriteLine( $" - {SubIndex} - {watch.Elapsed.ToString()}" );

				Index++;
			}

			overallWatch.Stop();

			System.Console.Out.WriteLine($"Total took: {overallWatch.Elapsed.ToString()}");*/

			return 0;
		}
	}
}
