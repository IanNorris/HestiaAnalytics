using HestiaCore;
using SighthoundAPI;
using System;
using System.Collections.Generic;
using System.Linq;
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

		//Camera data
		private long TimeOfLastStateGather = 0;
		private long TimeOfLastClipGather = 0;
		private Dictionary<string, Camera> Cameras;

		public delegate void OnCameraStatusChangedDelegate(string Camera, bool OldEnabled, bool NewEnabled, string OldStatus, string NewStatus, bool FirstTime);
		public OnCameraStatusChangedDelegate OnCameraStatusChanged;

		public delegate void OnCameraRuleChangedDelegate(string Camera, string Rule, bool OldEnabled, bool NewEnabled, bool FirstTIme);
		public OnCameraRuleChangedDelegate OnCameraRuleChanged;

		private void ThreadMain()
		{
			while( Running )
			{
				long CurrentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
				if ( CurrentTime - TimeOfLastStateGather > Settings.UpdateStateIntervalMS )
				{
					GatherInitialState();
				}

				Thread.Sleep( Settings.UpdateIntervalMS );
			}
		}

		public SightHoundListener( SighthoundSettings Settings )
		{
			this.Settings = Settings;

			SighthoundEndpoint = new SecureEndpoint(Settings.Hostname, Settings.CertificateAuthorityThumbprint, Settings.Port);
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

			TimeOfLastStateGather = DateTimeOffset.Now.ToUnixTimeMilliseconds();

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
