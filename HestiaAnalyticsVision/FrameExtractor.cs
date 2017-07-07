using Accord.Video.FFMPEG;
using System.Drawing;
using System.Collections.Generic;
using System;
using Accord.Vision.Motion;
using System.IO;

namespace HestiaAnalyticsVision
{
	public class FrameExtractor
	{
		private VideoFileReader Reader;

		private MotionDetector Detector;

		private MotionAnalysisSettings Settings;

		public delegate void OnClipEventDelegate( string Filename );
		public delegate void OnMotionDetectedFrameDelegate(Bitmap Frame, Rectangle ObjectArea, float frameTimestamp);
		public OnMotionDetectedFrameDelegate OnMotionDetectedFrame;
		public OnClipEventDelegate OnClipStart;
		public OnClipEventDelegate OnClipEnd;

		public FrameExtractor( MotionAnalysisSettings Settings )
		{
			this.Settings = Settings;

			var BackgroundDetector = new SimpleBackgroundModelingDetector(true, true);
			BackgroundDetector.FramesPerBackgroundUpdate = Settings.FramesPerBackgroundUpdate;

			//Avoid div0
			if(Settings.MotionFramesPerAnalysis == 0)
			{
				Settings.MotionFramesPerAnalysis = 1;
			}

			Detector = new MotionDetector(BackgroundDetector, new BlobCountingObjectsProcessing( Settings.MinMotionWidth, Settings.MinMotionHeight, false));
		}

		public void Run(string Filename)
		{
			Reader = new VideoFileReader();

			try
			{
				Reader.Open(Filename);
			}
			catch (IOException)
			{
				return;
			}

			OnClipStart(Filename);

			int Index = 0;

			int RealFrameIndex = 0;

			Bitmap NextFrame;
			while((NextFrame = Reader.ReadVideoFrame())!=null)
			{
				float motionLevel = Detector.ProcessFrame(NextFrame);
				
				// check objects' count
				if (Detector.MotionProcessingAlgorithm is BlobCountingObjectsProcessing)
				{
					var detector = (BlobCountingObjectsProcessing)Detector.MotionProcessingAlgorithm;

					int MinX = int.MaxValue;
					int MinY = int.MaxValue;
					int MaxX = int.MinValue;
					int MaxY = int.MinValue;

					foreach( var Rect in detector.ObjectRectangles )
					{
						MinX = Math.Min( Rect.X, MinX );
						MinY = Math.Min(Rect.Y, MinY);

						MaxX = Math.Max(Rect.X + Rect.Width, MaxX);
						MaxY = Math.Max(Rect.Y + Rect.Height, MaxY);
					}

					MinX = Math.Max(MinX - Settings.BorderPaddingPixels, 0);
					MinY = Math.Max(MinY - Settings.BorderPaddingPixels, 0);

					MaxX = Math.Min(MaxX + Settings.BorderPaddingPixels, NextFrame.Width);
					MaxY = Math.Min(MaxY + Settings.BorderPaddingPixels, NextFrame.Height);

					if ( detector.ObjectRectangles.Length > 0 )
					{
						if (Index % Settings.MotionFramesPerAnalysis == 0)
						{
							Rectangle SelectedArea = new Rectangle(MinX, MinY, MaxX - MinX, MaxY - MinY);

							OnMotionDetectedFrame(NextFrame, SelectedArea, (float)RealFrameIndex / (float)Reader.FrameRate);
						}
						Index++;
					}

					RealFrameIndex++;
				}
			}

			OnClipEnd(Filename);
		}
	}
}
