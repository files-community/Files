using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Files.DataModels
{
    public class TerminalModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }

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