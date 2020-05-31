using System.Collections.Generic;

namespace Files.DataModels
{
    internal class TerminalFileModel
    {
        public int Version { get; set; }

        public List<TerminalModel> Terminals { get; set; }
    }
}