using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace Files.App.DataModels
{
    public class TerminalFileModel
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("DefaultTerminalName")]
        public string DefaultTerminalName { get; set; }

        [JsonPropertyName("terminals")]
        public ObservableCollection<Terminal> Terminals { get; set; } = new ObservableCollection<Terminal>();

        public Terminal GetDefaultTerminal()
        {
            Terminal terminal = Terminals.FirstOrDefault(x => x.Name.Equals(DefaultTerminalName, StringComparison.OrdinalIgnoreCase));
            if (terminal is not null)
            {
                return terminal;
            }
            else
            {
                ResetToDefaultTerminal();
            }

            return Terminals.FirstOrDefault();
        }

        public void ResetToDefaultTerminal()
        {
            DefaultTerminalName = Terminals.Any(t => t.Name == "Windows Terminal") ? "Windows Terminal" : "CMD";
        }

        public void AddTerminal(Terminal terminal)
        {
            //Ensure terminal is not already in List
            if (!Terminals.Any(x => x.Name.Equals(terminal.Name, StringComparison.OrdinalIgnoreCase)))
            {
                Terminals.Add(terminal);
            }
        }

        public void RemoveTerminal(Terminal terminal)
        {
            Terminal? existingTerminal = Terminals.FirstOrDefault(x => x.Name.Equals(terminal.Name, StringComparison.OrdinalIgnoreCase));
            if (existingTerminal is not null && Terminals.Remove(existingTerminal))
            {
                if (string.IsNullOrWhiteSpace(DefaultTerminalName) ||
					DefaultTerminalName.Equals(terminal.Name, StringComparison.OrdinalIgnoreCase))
                {
                    ResetToDefaultTerminal();
                }
            }
        }
    }
}