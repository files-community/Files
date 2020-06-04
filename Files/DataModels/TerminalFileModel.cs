using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Files.DataModels
{
    public class TerminalFileModel
    {
        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("defaultTerminalId")]
        public int DefaultTerminalId { get; set; }

        [JsonProperty("terminals")]
        public List<TerminalModel> Terminals { get; set; } = new List<TerminalModel>();

        public TerminalModel GetDefaultTerminal()
        {
            if (DefaultTerminalId != 0)
            {
                return Terminals.Single(x => x.Id == DefaultTerminalId);
            }
            return Terminals.First();
        }
    }
}