using HestiaCore;
using Microsoft.ProjectOxford.Vision.Contract;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HestiaAnalyticsVision
{
	public class MultiStageImageClassificationResult
	{
		public AnalysisResult Result;
		public bool ContainsPerson;
		public int QueriesMade;
	}

	public static class MultiStageImageClassificationQuery
	{
		private static ImageCodecInfo GetEncoderInfo( ImageFormat Format )
		{
			ImageCodecInfo[] Codecs = ImageCodecInfo.GetImageDecoders();

			foreach( ImageCodecInfo Codec in Codecs )
			{
				if( Codec.FormatID == Format.Guid )
				{
					return Codec;
				}
			}
			return null;
		}

		public static MultiStageImageClassificationResult ClassifyImage( AnalyzeImageFilterSettings Settings, SecureEndpoint CognitiveEndpoint, string CacheQueryFolder, string UniqueFrameId, Bitmap frame, System.Drawing.Rectangle rect, long frameTimestamp )
		{
			MultiStageImageClassificationResult Result = new MultiStageImageClassificationResult();
			Result.ContainsPerson = false;
			Result.QueriesMade = 0;
			List<AnalysisResult> ResultResponses = new List<AnalysisResult>();

			long ImageQuality = Settings.JpegQuality;
			var JpegEncoder = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
			var JpegEncoderParams = new EncoderParameters() { Param = new[] { new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, ImageQuality) } };

			using (MemoryStream MS = new MemoryStream())
			{
				using (Bitmap NewBitmap = frame.Clone(rect, frame.PixelFormat))
				{
					NewBitmap.Save(MS, JpegEncoder, JpegEncoderParams);

					var Query = HestiaAnalyticsVision.AnalyzeImageQuery.CreateFromBytes(
						CognitiveEndpoint,
						new AnalyzeImageQuery.VisualFeatures[]
						{
							AnalyzeImageQuery.VisualFeatures.Tags
						},
						new AnalyzeImageQuery.Details[] { },
						MS.ToArray()
					);

					if (CacheQueryFolder != null)
					{
						NewBitmap.Save( Path.Combine(CacheQueryFolder, UniqueFrameId + ".jpg"), JpegEncoder, JpegEncoderParams);
					}

					string AnalysisResultString = Query.GetAsString();
					if (CacheQueryFolder != null)
					{
						File.WriteAllText(Path.Combine(CacheQueryFolder, UniqueFrameId + $"_{Result.QueriesMade}.json"), AnalysisResultString);
					}
					
					Result.QueriesMade++;
					
					var FrameData = JsonConvert.DeserializeObject<AnalysisResult>(AnalysisResultString);

					ResultResponses.Add(FrameData);

					if (FrameData != null )
					{
						if (FrameData.Categories != null)
						{
							foreach (var Category in FrameData.Categories)
							{
								if (Category.Name == "person")
								{
									Result.ContainsPerson = true;
								}
							}
						}

						if (FrameData.Tags != null)
						{
							foreach (var Tag in FrameData.Tags)
							{
								if (Tag.Name == "person")
								{
									Result.ContainsPerson = true;
								}
							}
						}
					}

					if( Result.ContainsPerson )
					{
						var FaceQuery = HestiaAnalyticsVision.AnalyzeImageQuery.CreateFromBytes(
							CognitiveEndpoint,
							new AnalyzeImageQuery.VisualFeatures[] { AnalyzeImageQuery.VisualFeatures.Faces },
							new AnalyzeImageQuery.Details[] { },
							MS.ToArray()
						);

						string FaceQueryString = FaceQuery.GetAsString();

						var FaceQueryData = JsonConvert.DeserializeObject<AnalysisResult>(FaceQueryString);

						ResultResponses.Add(FaceQueryData);

					}

					Result.Result = MergeClipJsonResults.MergeAndFilter(ResultResponses, Settings);

					if (CacheQueryFolder != null)
					{
						var SerializeFinal = JsonConvert.SerializeObject(Result.Result);

						File.WriteAllText(Path.Combine(CacheQueryFolder, UniqueFrameId + $"_F.json"), SerializeFinal);
					}

					return Result;
				}
			}
		}
	}
}
