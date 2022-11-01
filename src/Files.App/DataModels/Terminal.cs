using System.Text.Json.Serialization;

namespace Files.App.DataModels
{
	public class Terminal
	{
		public Terminal()
		{
		}
		
		public Terminal(string name, string path, string arguments = "", string icon = "")
		{
			Name = name;
			Path = path;
			Arguments = arguments;
			Icon = icon;
		}

		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("path")]
		public string Path { get; set; } = string.Empty;

		[JsonPropertyName("arguments")]
		public string Arguments { get; set; } = string.Empty;

		[JsonPropertyName("icon")]
		public string Icon { get; set; } = string.Empty;
	}
}