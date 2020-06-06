using Files.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public void ResetToDefaultTerminal()
        {
            DefaultTerminalId = 1;
        }

        public async Task<bool> AddTerminal(TerminalModel terminal, string packageName)
        {
            bool isChanged = false;
            bool isInstalled = await PackageHelper.IsAppInstalledAsync(packageName);
            //Ensure terminal is not already in List
            if (Terminals.FirstOrDefault(x => x.Path.Equals(terminal.Path, StringComparison.OrdinalIgnoreCase)) == null && isInstalled)
            {
                Terminals.Add(terminal);
                isChanged = true;
            }
            else if (!isInstalled)
            {
                Terminals.Remove(Terminals.FirstOrDefault(x => x.Path.Equals(terminal.Path, StringComparison.OrdinalIgnoreCase)));
                ResetToDefaultTerminal();
                isChanged = true;
            }
            return isChanged;
        }
    }
}