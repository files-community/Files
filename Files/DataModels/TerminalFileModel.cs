using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public void AddTerminal(Terminal terminal)
        {
            //Ensure terminal is not already in List
            if (Terminals.FirstOrDefault(x => x.Path.Equals(terminal.Path, StringComparison.OrdinalIgnoreCase)) == null)
            {
                Terminals.Add(terminal);
            }
        }

        public void RemoveTerminal(Terminal terminal)
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
            }
        }
    }
}