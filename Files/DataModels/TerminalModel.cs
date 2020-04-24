using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.DataModels
{
    public class TerminalModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public string icon { get; set; }

        public string arguments { get; set; }
    }
}