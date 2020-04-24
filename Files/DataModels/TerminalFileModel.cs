using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.DataModels
{
    internal class TerminalFileModel
    {
        public int Version { get; set; }

        public List<TerminalModel> Terminals { get; set; }
    }
}