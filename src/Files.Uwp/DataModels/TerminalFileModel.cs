﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Files.Uwp.DataModels
{
    public class TerminalFileModel
    {
        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("DefaultTerminalName")]
        public string DefaultTerminalName { get; set; }

        [JsonProperty("terminals")]
        public List<Terminal> Terminals { get; set; } = new List<Terminal>();

        public Terminal GetDefaultTerminal()
        {
            Terminal terminal = Terminals.FirstOrDefault(x => x.Name.Equals(DefaultTerminalName, StringComparison.OrdinalIgnoreCase));
            if (terminal != null)
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
            if (Terminals.Any(x => x.Name == "Windows Terminal"))
            {
                DefaultTerminalName = "Windows Terminal";
            }
            else
            {
                DefaultTerminalName = "CMD";
            }
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
            Terminal existingTerminal = Terminals.FirstOrDefault(x => x.Name.Equals(terminal.Name, StringComparison.OrdinalIgnoreCase));
            if (existingTerminal != null && Terminals.Remove(existingTerminal))
            {
                if (string.IsNullOrWhiteSpace(DefaultTerminalName))
                {
                    ResetToDefaultTerminal();
                }
                else if (DefaultTerminalName.Equals(terminal.Name, StringComparison.OrdinalIgnoreCase))
                {
                    ResetToDefaultTerminal();
                }
            }
        }
    }
}