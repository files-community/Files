using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.CommandLine
{
    internal class ParsedCommand
    {
        public ParsedCommandType Type { get; set; }

        public string Payload { get; set; }
    }
}