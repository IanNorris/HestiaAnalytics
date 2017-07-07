using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Vision.Contract;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace HestiaAnalyticsVision
{
	public static class MergeClipJsonResults
	{
		public static bool IsItemIgnored( string Name, double Confidence, AnalyzeImageFilterSettings Filter )
		{
			bool ShouldIgnore = false;
			if (Filter.Ignore != null)
			{
				foreach (var Exclude in Filter.Ignore)
				{
					//Regex
					if (Exclude.StartsWith("/") && Exclude.EndsWith("/"))
					{
						string ExcludeRE = Exclude.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)[0];
						if (Regex.Match(Name, ExcludeRE).Success)
						{
							ShouldIgnore = true;
							break;
						}
					}
					else
					{
						string ExcludeStr = Exclude.ToLower();

						if (ExcludeStr == Name)
						{
							ShouldIgnore = true;
							break;
						}
					}
				}
			}

			bool FoundConfidence = false;
			if (Filter.MinimumConfidence != null)
			{
				foreach (var ExcludePair in Filter.MinimumConfidence)
				{
					string Exclude = ExcludePair.Key;
					//Regex
					if (Exclude.StartsWith("/") && Exclude.EndsWith("/"))
					{
						string ExcludeRE = Exclude.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)[1];
						if (Regex.Match(Name, ExcludeRE).Success)
						{
							FoundConfidence = true;

							if (Confidence < ExcludePair.Value)
							{
								ShouldIgnore = true;
								break;
							}
						}
					}
					else
					{
						string ExcludeStr = Exclude.ToLower();

						if (ExcludeStr == Name)
						{
							FoundConfidence = true;

							if (Confidence < ExcludePair.Value)
							{
								ShouldIgnore = true;
								break;
							}
						}
					}
				}
			}

			if (!FoundConfidence && Confidence < Filter.MinConfidence)
			{
				ShouldIgnore = true;
			}

			return ShouldIgnore;
		}

		public static AnalysisResult MergeAndFilter( List<string> Frames, AnalyzeImageFilterSettings Filter )
		{
			AnalysisResult MergedResult = new AnalysisResult();
			// RequestId, Metadata, ImageType, Color will be null

			List<Category> NewCategories = new List<Category>();
			List<Face> NewFaces = new List<Face>();
			List<Tag> NewTags = new List<Tag>();
			List<string> NewDescTags = new List<string>();
			List<Caption> NewCaptions = new List<Caption>();

			foreach ( string Frame in Frames )
			{
				AnalysisResult FrameData = null;

				if (Frame == null || Frame.Length == 0)
				{
					continue;
				}

				try
				{
					FrameData = JsonConvert.DeserializeObject<AnalysisResult>(Frame);
				}
				catch( Exception e )
				{
					System.Console.Error.WriteLine(e.ToString());
				}

				if(FrameData == null )
				{
					continue;
				}

				if( FrameData.Adult != null )
				{
					if( MergedResult.Adult == null )
					{
						MergedResult.Adult = new Adult();
					}

					MergedResult.Adult.IsAdultContent |= FrameData.Adult.IsAdultContent;
					MergedResult.Adult.AdultScore = Math.Max( MergedResult.Adult.AdultScore, FrameData.Adult.AdultScore );
					MergedResult.Adult.IsRacyContent |= FrameData.Adult.IsRacyContent;
					MergedResult.Adult.RacyScore = Math.Max( MergedResult.Adult.RacyScore, FrameData.Adult.RacyScore );
				}
				
				if ( FrameData.Categories != null )
				{
					foreach( var Obj in FrameData.Categories )
					{
						string Name = Obj.Name.ToLower();

						bool Found = false;
						foreach( var ExistingObj in NewCategories )
						{
							if( ExistingObj.Name == Obj.Name )
							{
								if( Obj.Score > ExistingObj.Score )
								{
									ExistingObj.Detail = Obj.Detail;
									ExistingObj.Score = Obj.Score;
								}
								Found = true;
							}
						}
						
						if( !Found )
						{
							if ( !Filter.Ignore.Contains(Obj.Name) && !IsItemIgnored(Obj.Name, Obj.Score, Filter) )
							{
								NewCategories.Add( Obj );
							}
						}
					}
				}

				if (FrameData.Faces != null)
				{
					NewFaces.AddRange( FrameData.Faces );
				}
				
				if (FrameData.Tags != null)
				{
					foreach (var Tag in FrameData.Tags)
					{
						bool Found = false;
						foreach (var ExistingTag in NewTags)
						{
							if (ExistingTag.Name == Tag.Name)
							{
								if (Tag.Confidence > ExistingTag.Confidence)
								{
									ExistingTag.Confidence = Tag.Confidence;
									ExistingTag.Hint = Tag.Hint;
								}
								Found = true;
							}
						}

						if (!Found)
						{
							if( !Filter.Ignore.Contains(Tag.Name) && !IsItemIgnored(Tag.Name, Tag.Confidence, Filter))
							{
								NewTags.Add(Tag);
							}
						}
					}
				}

				if(FrameData.Description != null )
				{						
					foreach ( var Tag in FrameData.Description.Tags )
					{
						if (!Filter.Ignore.Contains(Tag) && !IsItemIgnored(Tag, 1.0, Filter) && !NewDescTags.Contains(Tag))
						{
							NewDescTags.Add(Tag);
						}
					}

					foreach( var Caption in FrameData.Description.Captions )
					{
						if( Caption.Confidence > Filter.MinDescriptionConfidence )
						{
							bool Found = false;
							foreach( var Existing in NewCaptions )
							{
								if( Existing.Text == Caption.Text )
								{
									Found = true;
									if( Caption.Confidence > Existing.Confidence )
									{
										Existing.Confidence = Caption.Confidence;
									}
									break;
								}
							}

							if (!Found)
							{
								NewCaptions.Add(Caption);
							}
						}
					}
				}
			}

			MergedResult.Categories = NewCategories.Count > 0 ? NewCategories.ToArray() : null;
			MergedResult.Faces = NewFaces.Count > 0 ? NewFaces.ToArray() : null;
			MergedResult.Tags = NewTags.Count > 0 ? NewTags.ToArray() : null;

			bool HasDescription = NewDescTags.Count > 0 || NewCaptions.Count > 0;
			if( HasDescription )
			{
				MergedResult.Description = new Description();
				MergedResult.Description.Tags = NewDescTags.ToArray();
				MergedResult.Description.Captions = NewCaptions.ToArray();
			}

			return MergedResult;
		}
	}
}
