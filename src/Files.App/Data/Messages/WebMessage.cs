namespace Files.App.Data.Messages
{
	/// <summary>
	/// Represents a messenger for JavaScript web messages (event listeners).
	/// </summary>
	public sealed class WebMessage
	{
		/// <summary>
		/// The classification type of the web message (to differentiate from different event listeners).
		/// </summary>	
		[JsonPropertyName("type")]
		public string Type { get; set; }

		/// <summary>
		/// The key (or payload) associated with the web message.
		/// </summary>
		[JsonPropertyName("key")]
		public string Key { get; set;  }
	}
}
