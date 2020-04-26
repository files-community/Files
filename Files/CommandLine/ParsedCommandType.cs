using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.CommandLine
{
    internal enum ParsedCommandType
    {
        Unknown,
        OpenDirectory,
        OpenPath,
        ExplorerShellCommand
    }
}