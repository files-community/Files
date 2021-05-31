using Files.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Common
{
    public class Logger
    {       
        ILogWriter LogWriter { get; }

        public Logger(ILogWriter logWriter, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            LogWriter = logWriter;
        }

        public void Error(Exception ex, string formattedException, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Log(type: "ERROR", caller: caller, message: $"{ex.Message}\n\t{formattedException}");
        }

        public void Error(Exception ex, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Log(type: "ERROR", caller: caller, message: $"{ex.Message}\n\t{ex}");
        }

        public void Error(string error, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Log(type: "ERROR", caller: caller, message: error);
        }

        public void Info(string info, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Log(type: "INFO", caller: caller, message: info);
        }

        public void Warn(Exception ex, string warning, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Log(type: "WARN", caller: caller, message: $"{warning}\n\t{ex}");
        }

        public void Warn(string warning, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Log(type: "WARN", caller: caller, message: warning);
        }

        public void Info(Exception ex, string info, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Log(type: "INFO", caller: caller, message: $"{info}\n\t{ex}");
        }

        public void Info(string info, object obj, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Log(type: "INFO", caller: caller, message: string.Format(info, obj));
        }

        public void Warn(Exception ex, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Log(type: "WARN", caller: caller, message: $"{ex.Message}\n\t{ex}");
        }

        private void Log(string type, string caller, string message)
        {
            try
            {
                LogWriter.WriteLineToLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}|{type}|{caller}|{message}");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Writing to log file failed with the following exception:\n{e}");
            }
        }
    }
}
