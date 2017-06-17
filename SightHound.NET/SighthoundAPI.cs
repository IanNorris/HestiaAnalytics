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

	// GetClipUriRequest

	[IsRemoteArrayAttribute]
	public class GetClipUriRequest
	{
		public string CameraName;
		public Timestamp Start;
		public Timestamp End;
		public int UriId;
		public string ContentType;
		public ObjectIds ObjectIds;
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
	}
}
