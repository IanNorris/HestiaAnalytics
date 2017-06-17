using HestiaCore;
using HestiaCore.Source.Serialization;
using System;
using System.Collections.Generic;

namespace SighthoundAPI
{
	// Helper types

	public class GenericStringRequest
	{
		public string Name;
	}

	public class GenericStatusResponse
	{
		public bool Success;
	}

	public class GenericToggleRequest
	{
		public string Name;
		public bool Enable;
	}

	[IsRemoteArrayAttribute]
	public struct Timestamp
	{
		public int Seconds;
		public int Milliseconds;
	}

	[IsRemoteStructAttribute]
	public struct ObjectIds
	{
		[RemoteParameterName("objectIds")]
		public List<int> ObjectIdArray;
	}

	//Ping

	public class PingResponse
	{
		public string Id;
	}

	// GetCameraNames

	public class GetCamerNamesResponse
	{
		public bool Unknown;
		public List<string> CameraNames;
	}

	// GetRulesForCamera

	public class GetRulesForCameraResponse
	{
		public bool Unknown;
		public List<string> RuleNames;
	}

	// GetClipsForRule

	public class GetClipsForRuleRequest
	{
		public string CameraName;
		public string RuleName;
		public double From;
		public int Count;
		public int Page;
		public bool OldestFirst;
	}

	[IsRemoteArrayAttribute]
	public class Clip
	{
		public string CameraName;
		public Timestamp Start;
		public Timestamp End;
		public Timestamp ThumbnailTime;
		public string FriendlyTime;
		public List<int> ObjectIds;
	}

	public class GetClipsForRuleResponse
	{
		public bool Unknown;
		public List<Clip> Clips;
		public int ClipCount;
	}

	// GetClipUriRequest

	[IsRemoteArrayAttribute]
	public class GetClipUriRequest
	{
		public string CameraName;
		public Timestamp Start;
		public Timestamp End;
		public int UriId;
		public string ContentType;
		public ObjectIds Objects;
	}

	public class GetClipUriResponse
	{
		public bool Unknown;
		public string Uri;
		public Timestamp Start;
		public Timestamp End;
	}

	// GetCameraStatus

	public class GetCameraStatusResponse
	{
		public string Status;
		public bool? Enabled;
	}

	// GetLiveCameras

	[IsRemoteArrayAttribute]
	public class LiveCamera
	{
		public string CameraName;
		public string Unknown;
	}

	public class GetLiveCamerasResponse
	{
		public bool Unknown;
		public List<LiveCamera> Cameras;
	}

	// GetLiveCameraUri

	public enum VideoType
	{
		Jpeg,
		H264
	}

	public class GetLiveCameraUriRequest
	{
		public string CameraName;
		public int RequestId;
		public string MimeType; // "image/jpeg" or "video/h264"
	}

	public class GetLiveCameraUriResponse
	{
		public bool Unknown;
		public string Uri;
	}

	// GetRuleInfo

	public class GetRuleInfoResponse
	{
		public string RuleName;
		public string RuleNameDuplicate;
		public string Schedule;
		public bool Enabled;
		public List<string> ResponseTypes;
	}

	// GetClipThumbnails

	[IsRemoteArrayAttribute]
	public class ImageDimensions
	{
		public int Width;
		public int Height;
	}

	[IsRemoteStructAttribute]
	public class ImageDimensionsWrapper
	{
		[RemoteParameterName("maxSize")]
		public ImageDimensions ThumbnailDimensions;
	}

	[IsRemoteArrayAttribute]
	public class ThumbnailClipRequest
	{
		public string CameraName;
		public Timestamp ThumbnailTime;
	}

	public class GetClipThumbnailsRequest
	{
		public List<ThumbnailClipRequest> Clips;
		public string MimeType;

		public ImageDimensionsWrapper ThumbnailDimensions;
	}

	public class GetClipThumbnailsResponse
	{
		public bool Unknown;
		public List<string> Uris;
	}

	//API

	public static class Sighthound
	{
		private static Random PRNG = new Random();

		public static SecureXMLRPCResponse<PingResponse> Ping(SecureEndpoint Endpoint)
		{
			return SighthoundRPC.SendRPCCommand<PingResponse>(Endpoint, "ping", null);
		}

		public static SecureXMLRPCResponse<GetCamerNamesResponse> GetCameraNames(SecureEndpoint Endpoint)
		{
			return SighthoundRPC.SendRPCCommand<GetCamerNamesResponse>(Endpoint, "remoteGetCameraNames", null);
		}

		public static SecureXMLRPCResponse<GetRulesForCameraResponse> GetRulesForCamera(SecureEndpoint Endpoint, string CameraName)
		{
			GenericStringRequest Request = new GenericStringRequest { Name = CameraName };
			return SighthoundRPC.SendRPCCommand<GetRulesForCameraResponse>(Endpoint, "remoteGetRulesForCamera", Request);
		}

		public static SecureXMLRPCResponse<GetClipsForRuleResponse> GetClipsForRule(SecureEndpoint Endpoint, GetClipsForRuleRequest Request)
		{
			return SighthoundRPC.SendRPCCommand<GetClipsForRuleResponse>(Endpoint, "remoteGetClipsForRule", Request);
		}

		public static SecureXMLRPCResponse<GenericStatusResponse> EnableRule(SecureEndpoint Endpoint, string RuleName, bool Enable)
		{
			GenericToggleRequest Request = new GenericToggleRequest { Name = RuleName, Enable = Enable };
			return SighthoundRPC.SendRPCCommand<GenericStatusResponse>(Endpoint, "remoteEnableRule", Request);
		}

		public static SecureXMLRPCResponse<GenericStatusResponse> EnableCamera(SecureEndpoint Endpoint, string CameraName, bool Enable)
		{
			GenericToggleRequest Request = new GenericToggleRequest { Name = CameraName, Enable = Enable };
			return SighthoundRPC.SendRPCCommand<GenericStatusResponse>(Endpoint, "enableCamera", Request);
		}

		//NOTE: There is a good chance this will fall over in the deserialization code if there are no cameras registered.
		public static SecureXMLRPCResponse<GetLiveCamerasResponse> GetLiveCameras(SecureEndpoint Endpoint)
		{
			return SighthoundRPC.SendRPCCommand<GetLiveCamerasResponse>(Endpoint, "remoteGetLiveCameras", null);
		}

		public static SecureXMLRPCResponse<GetClipUriResponse> GetClipUri(SecureEndpoint Endpoint, GetClipUriRequest Request)
		{
			return SighthoundRPC.SendRPCCommand<GetClipUriResponse>(Endpoint, "remoteGetClipUri", Request);
		}

		public static SecureXMLRPCResponse<GetLiveCameraUriResponse> GetLiveCameraUri(SecureEndpoint Endpoint, string CameraName, VideoType VideoType )
		{
			string MimeType = VideoType == VideoType.Jpeg ? "image/jpeg" : "video/h264";

			GetLiveCameraUriRequest Request = new GetLiveCameraUriRequest { CameraName = CameraName, RequestId = PRNG.Next() % 1000, MimeType = MimeType };

			//Example usage:
			// /camera/52715/image.jpg?height=225&width=225&date=1497721985885
			// /camera/52715/98a7cc473f3f4475f6197a1ac44e03b8.m3u8

			return SighthoundRPC.SendRPCCommand<GetLiveCameraUriResponse>(Endpoint, "remoteGetCameraUri", Request );
		}

		public static SecureXMLRPCResponse<GetCameraStatusResponse> GetCameraStatus(SecureEndpoint Endpoint, string CameraName)
		{
			GenericStringRequest Request = new GenericStringRequest { Name = CameraName };
			return SighthoundRPC.SendRPCCommand<GetCameraStatusResponse>(Endpoint, "getCameraStatusAndEnabled", Request);
		}

		public static SecureXMLRPCResponse<GetRuleInfoResponse> GetRuleInfo(SecureEndpoint Endpoint, string RuleName)
		{
			GenericStringRequest Request = new GenericStringRequest { Name = RuleName };
			return SighthoundRPC.SendRPCCommand<GetRuleInfoResponse>(Endpoint, "getRuleInfo", Request);
		}

		public static SecureXMLRPCResponse<GetClipThumbnailsResponse> GetClipThumbnails(SecureEndpoint Endpoint, List<ThumbnailClipRequest> ThumbnailClips, int Width, int Height )
		{
			GetClipThumbnailsRequest Request = new GetClipThumbnailsRequest();
			Request.Clips = ThumbnailClips;
			Request.MimeType = "image/jpeg";
			Request.ThumbnailDimensions = new ImageDimensionsWrapper();
			Request.ThumbnailDimensions.ThumbnailDimensions = new ImageDimensions() { Width = Width, Height = Height };
			
			return SighthoundRPC.SendRPCCommand<GetClipThumbnailsResponse>(Endpoint, "remoteGetThumbnailUris", Request);
		}

		//getClipThumbnailURIs (use getClipThumbnailURIsNEW as ref)
	}
}
