using HestiaCore;
using SighthoundAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SighthoundAPI
{
	public class SightHoundListener
	{
		private class Rule
		{
			
		}

		private class Camera
		{
			public bool Enabled;
			public string Status;
			public Dictionary<string, Rule> Rules;
		}

		private SecureEndpoint SighthoundEndpoint;
		private SighthoundSettings Settings;
		private Thread WorkerThread;
		private bool Running = true;

		private int ClipId = 0;

		//Camera data
		private long LastStateGatherTime = 0;
		private long LastClipGatherTime = 1497313459933; //Should be DateTimeOffset.Now.ToUnixTimeMilliseconds()
		private Dictionary<string, Camera> Cameras;

		public delegate void OnCameraStatusChangedDelegate(string Camera, bool OldEnabled, bool NewEnabled, string OldStatus, string NewStatus, bool FirstTime);
		public OnCameraStatusChangedDelegate OnCameraStatusChanged;

		public delegate void OnCameraRuleChangedDelegate(string Camera, string Rule, bool OldEnabled, bool NewEnabled, bool FirstTIme);
		public OnCameraRuleChangedDelegate OnCameraRuleChanged;

		public delegate void OnNewClipDelegate( Clip Clip );
		public OnNewClipDelegate OnNewClip;

		public delegate void OnDownloadClipDelegate(Clip Clip);
		public OnDownloadClipDelegate OnDownloadClip;

		public delegate void OnDownloadedClipDelegate(Clip Clip, string Filename);
		public OnDownloadedClipDelegate OnDownloadedClip;

		private void ThreadMain()
		{
			while( Running )
			{
				long CurrentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
				if ( CurrentTime - LastStateGatherTime > Settings.UpdateStateIntervalMS )
				{
					GatherInitialState();
					LastStateGatherTime = CurrentTime;
				}

				HashSet<int> MergedClipObjects = new HashSet<int>();
				List<Clip> MergedClips = new List<Clip>();

				foreach ( var Camera in Cameras )
				{
					foreach( var Rule in Camera.Value.Rules )
					{
						GetClipsForRuleRequest ClipRequest = new GetClipsForRuleRequest();
						ClipRequest.CameraName = Camera.Key;
						ClipRequest.Count = 1000;
						ClipRequest.From = (double)LastClipGatherTime / 1000.0f;
						ClipRequest.OldestFirst = true;
						ClipRequest.Page = 0;
						ClipRequest.RuleName = Rule.Key;
						var ClipPromise = Sighthound.GetClipsForRule(SighthoundEndpoint, ClipRequest);
						var Clips = ClipPromise.Get();

						while( Clips.Clips.Count > 0 )
						{
							Clip MergedClip = new Clip();
							MergedClip.CameraName = Clips.Clips[0].CameraName;
							MergedClip.FriendlyTime = Clips.Clips[0].FriendlyTime;
							MergedClip.ObjectIds = Clips.Clips[0].ObjectIds;
							MergedClip.Start = Clips.Clips[0].Start;
							MergedClip.End = Clips.Clips[0].End;
							MergedClip.ThumbnailTime = Clips.Clips[0].ThumbnailTime;
							
							int Index = 0;
							while( Index < Clips.Clips.Count )
							{
								if(Clips.Clips[Index].End.UnixTime - MergedClip.End.UnixTime <= Settings.ClipStichingGapMS )
								{
									foreach( var Id in Clips.Clips[Index].ObjectIds )
									{
										MergedClipObjects.Add(Id);
									}

									MergedClip.End = Clips.Clips[Index].End;
									Clips.Clips.RemoveAt(Index);
								}
								else
								{
									Index++;
								}
							}

							bool Found = false;
							foreach( var ExistingClip in MergedClips )
							{
								if( ExistingClip.CameraName != MergedClip.CameraName )
								{
									continue;
								}

								if( Math.Max(MergedClip.Start.UnixTime, ExistingClip.Start.UnixTime) - Math.Min(MergedClip.Start.UnixTime, ExistingClip.Start.UnixTime) > Settings.ClipStichingGapMS )
								{
									continue;
								}

								if( Math.Max( MergedClip.End.UnixTime, ExistingClip.End.UnixTime ) - Math.Min( MergedClip.End.UnixTime, ExistingClip.End.UnixTime ) > Settings.ClipStichingGapMS )
								{
									continue;
								}

								//Object is within the range

								// Merge the merged clip ids into the existing clip
								foreach( var Id in ExistingClip.ObjectIds )
								{
									MergedClipObjects.Add(Id);
								}
								ExistingClip.ObjectIds.Clear();
								ExistingClip.ObjectIds.AddRange(MergedClipObjects);

								ExistingClip.Start = new Timestamp(Math.Min(MergedClip.Start.UnixTime, ExistingClip.Start.UnixTime));
								ExistingClip.End = new Timestamp(Math.Max(MergedClip.End.UnixTime, ExistingClip.End.UnixTime));

								Found = true;
								break;
							}

							if( !Found )
							{
								MergedClip.ObjectIds.Clear();
								MergedClip.ObjectIds.AddRange(MergedClipObjects);
								MergedClipObjects.Clear();

								MergedClips.Add( MergedClip );
							}
						}
					}
				}
				
				foreach( var Clip in MergedClips )
				{
					OnNewClip( Clip );
				}

				int SkipClips = 2;

				foreach (var Clip in MergedClips)
				{
					OnDownloadClip(Clip);

					GetClipUriRequest ClipRequest = new GetClipUriRequest();
					ClipRequest.CameraName = Clip.CameraName;
					ClipRequest.ContentType = "video/h264";
					ClipRequest.Start = Clip.Start;
					ClipRequest.End = Clip.End;
					ClipRequest.UriId = ClipId;
					ClipRequest.Objects = new ObjectIds { ObjectIdArray = Clip.ObjectIds };

					int RetryCount = 5;
					bool Retry = false;

					if( SkipClips-- > 0 )
					{
						ClipId++;
						continue;
					}

					do
					{
						try
						{
							SighthoundEndpoint.SetTimeout(3 * 60 * 1000);
							var ClipPromise = Sighthound.GetClipUri(SighthoundEndpoint, ClipRequest);
							var ClipUri = ClipPromise.Get();

							var DownloadPromise = SighthoundEndpoint.CreateGetRequest(ClipUri.Uri, null);

							var ClipUnixTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
							ClipUnixTime = ClipUnixTime.AddMilliseconds(ClipUri.Start.UnixTime);

							string TargetDirectory = Path.Combine(Settings.ClipCache, ClipUnixTime.ToString("yyyy_MM_dd"));

							string ClipFriendlyTime = ClipUnixTime.ToString("HH_mm_ss");

							Directory.CreateDirectory(TargetDirectory);

							string Filename = Path.Combine(TargetDirectory, $"{ClipId}_{ClipFriendlyTime}.mp4");

							DownloadPromise.WriteToFile(Filename);

							OnDownloadedClip(Clip, Filename);

							Retry = false;

							ClipId++;
						}
						catch (AggregateException ExOuter)
						{
							WebException Ex = ExOuter.InnerException as WebException;

							if( Ex != null )
							{
								if ( ((HttpWebResponse)(Ex.Response)).StatusCode == HttpStatusCode.GatewayTimeout )
								{
									Retry = true;
									System.Console.Error.WriteLine($"Failed, {RetryCount} retries left.");
								}
								else
								{
									throw ExOuter;
								}
							}
							else
							{
								throw ExOuter;
							}
						}
						finally
						{
							SighthoundEndpoint.ResetTimeoutToDefault();
						}
					} while (Retry && RetryCount-- > 0);
				}

				LastClipGatherTime = CurrentTime;

				Thread.Sleep( Settings.UpdateIntervalMS );
			}
		}

		public SightHoundListener( SighthoundSettings Settings )
		{
			this.Settings = Settings;

			SighthoundRPC.EndpointUri = Settings.EndpointUri;

			SighthoundEndpoint = new SecureEndpoint(Settings.Hostname, Settings.CertificateAuthorityThumbprint, Settings.Port, Settings.Secure);
			SighthoundEndpoint.AddBasicAuthenticationHeader(Settings.Username, Settings.Password);
		}

		public void Start()
		{
			WorkerThread = new Thread( new ThreadStart( ThreadMain ) );
			WorkerThread.Start();
		}

		public void Stop()
		{
			Running = false;
			WorkerThread.Join();
		}

		public void Wait()
		{
			WorkerThread.Join();
		}

		public void GatherInitialState()
		{
			Dictionary<string, Camera> OldCameras = Cameras;

			LastStateGatherTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

			//Update camera list
			{
				var CameraNamesPromise = Sighthound.GetCameraNames( SighthoundEndpoint );
				var CameraNames = CameraNamesPromise.Get();

				Cameras = new Dictionary<string, Camera>();
				foreach( var Cam in CameraNames.CameraNames )
				{
					var CameraStatusPromise = Sighthound.GetCameraStatus(SighthoundEndpoint, Cam );
					var CameraStatus = CameraStatusPromise.Get();

					if( (CameraStatus.Enabled.HasValue ? CameraStatus.Enabled.Value : false) && CameraStatus.Status == "connecting" )
					{
						Camera Camera = new Camera();
						Camera.Enabled = CameraStatus.Enabled.HasValue ? CameraStatus.Enabled.Value : false;
						Camera.Status = CameraStatus.Status;

						Cameras[Cam] = Camera;

						Camera OldCamera;
						if( OldCameras != null && OldCameras.TryGetValue( Cam, out OldCamera ) )
						{
							if( OldCamera.Enabled != Camera.Enabled || OldCamera.Status != Camera.Status )
							{
								OnCameraStatusChanged(Cam, OldCamera.Enabled, Camera.Enabled, OldCamera.Status, Camera.Status, OldCameras == null);
							}
						}
						else
						{
							OnCameraStatusChanged(Cam, false, Camera.Enabled, "off", Camera.Status, OldCameras == null);
						}
					}
					else
					{
						Camera OldCamera;
						if (OldCameras != null && OldCameras.TryGetValue(Cam, out OldCamera))
						{
							OnCameraStatusChanged(Cam, OldCamera.Enabled, false, OldCamera.Status, "off", OldCameras == null);
						}
					}
				}
			}

			//Update rule list
			{
				foreach( var Camera in Cameras )
				{
					var ResponsePromise = Sighthound.GetRulesForCamera(SighthoundEndpoint, Camera.Key );
					var Response = ResponsePromise.Get();

					if( !Camera.Value.Enabled )
					{
						continue;
					}

					Camera.Value.Rules = new Dictionary<string, Rule>();
					foreach (var RuleName in Response.RuleNames)
					{
						var RuleInfoPromise = Sighthound.GetRuleInfo(SighthoundEndpoint, RuleName);
						var RuleInfo = RuleInfoPromise.Get();
						if(RuleInfo != null )
						{
							bool IsEnabled = RuleInfo.Enabled;

							if (RuleName == "All objects")
							{
								IsEnabled = Settings.AllowAllObjectsRule;
							}

							if( IsEnabled )
							{
								Rule Rule = new Rule();

								Camera.Value.Rules[RuleName] = Rule;
							}
						}
					}
				}
			}
		}

		/*
		
	var Endpoint = 

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

	*/

	}
}
