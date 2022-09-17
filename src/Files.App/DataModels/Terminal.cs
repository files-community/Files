using System.Text.Json.Serialization;

namespace Files.App.DataModels
{
    public class Terminal
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("arguments")]
        public string Arguments { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }
    }
}