using System.Text.Json.Serialization;

namespace Files.App.Data.Parameters
{
	internal class GitConfirmLoginParams
	{
		[JsonPropertyName("client_id")]
		public string ClientId { get; init; }

		[JsonPropertyName("device_code")]
		public string DeviceCode { get; init; }

		[JsonPropertyName("grant_type")]
		public string GrantType { get; init; }

		public GitConfirmLoginParams(string clientId = "", string deviceCode = "") 
		{
			ClientId = clientId;
			DeviceCode = deviceCode;
			GrantType = "urn:ietf:params:oauth:grant-type:device_code";
		}
	}
}
