using HestiaCore;
using HestiaCore.Source.Serialization;
using System.Collections.Generic;

namespace SighthoundAPI
{
	// Helper types

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

	public class GetRulesForCameraRequest
	{
		public string CameraName;
	}

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
		public Timestamp AnotherTimestamp;
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

	public static class Methods
	{
		public static SecureXMLRPCResponse<PingResponse> Ping(SecureEndpoint Endpoint) { return SighthoundRPC.SendRPCCommand<PingResponse>(Endpoint, "ping", null); }
		public static SecureXMLRPCResponse<GetCamerNamesResponse> GetCameraNames(SecureEndpoint Endpoint) { return SighthoundRPC.SendRPCCommand<GetCamerNamesResponse>(Endpoint, "remoteGetCameraNames", null); }
		public static SecureXMLRPCResponse<GetClipUriResponse> GetClipUri( SecureEndpoint Endpoint, GetClipUriRequest Request ) { return SighthoundRPC.SendRPCCommand< GetClipUriResponse >(Endpoint, "remoteGetClipUri", Request); }
		public static SecureXMLRPCResponse<GetRulesForCameraResponse> GetRulesForCamera(SecureEndpoint Endpoint, GetRulesForCameraRequest Request) { return SighthoundRPC.SendRPCCommand<GetRulesForCameraResponse>(Endpoint, "remoteGetRulesForCamera", Request); }
		public static SecureXMLRPCResponse<GetClipsForRuleResponse> GetRulesForCamera(SecureEndpoint Endpoint, GetClipsForRuleRequest Request) { return SighthoundRPC.SendRPCCommand<GetClipsForRuleResponse>(Endpoint, "remoteGetClipsForRule", Request); }
	}
}
