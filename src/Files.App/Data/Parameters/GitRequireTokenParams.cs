using System.Text.Json.Serialization;

namespace Files.App.Data.Parameters
{
	internal class GitRequireTokenParams
	{
		[JsonPropertyName("client_id")]
		public string ClientId { get; init; }

		[JsonPropertyName("scope")]
		public string Scope { get; init; }

		public GitRequireTokenParams(string clientId = "", string scope = "repo")
		{
			ClientId = clientId;
			Scope = scope;
		}
	}
}
