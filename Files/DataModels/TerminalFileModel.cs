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

        [JsonProperty("defaultTerminalPath")]
        public string DefaultTerminalPath { get; set; }

        [JsonProperty("terminals")]
        public List<Terminal> Terminals { get; set; } = new List<Terminal>();

        public Terminal GetDefaultTerminal()
        {
            Terminal terminal = Terminals.FirstOrDefault(x => x.Path.Equals(DefaultTerminalPath, StringComparison.OrdinalIgnoreCase));
            if (terminal != null)
            {
                return terminal;
            }
            else
            {
                ResetToDefaultTerminal();
            }

            return Terminals.First();
        }

        public void ResetToDefaultTerminal()
        {
            DefaultTerminalPath = "cmd.exe";
        }

        public async Task<bool> AddOrRemoveTerminal(Terminal terminal, string packageName)
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
                if (Terminals.Remove(Terminals.FirstOrDefault(x => x.Path.Equals(terminal.Path, StringComparison.OrdinalIgnoreCase))))
                {
                    if (string.IsNullOrWhiteSpace(DefaultTerminalPath))
                    {
                        ResetToDefaultTerminal();
                    }
                    else if (DefaultTerminalPath.Equals(terminal.Path, StringComparison.OrdinalIgnoreCase))
                    {
                        ResetToDefaultTerminal();
                    }

                    isChanged = true;
                }
            }
            return isChanged;
        }
    }
}