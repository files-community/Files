using Newtonsoft.Json;

namespace Files.DataModels
{
    public class Terminal
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("arguments")]
        public string Arguments { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }
    }
}