using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Files.Common
{
    public interface ILogWriter
    {
        Task WriteLineToLog(string text);
        Task InitializeAsync(string name);
    }
}
