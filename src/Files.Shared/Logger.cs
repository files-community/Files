using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Shared
{
    public class Logger : ILogger
    {
        private readonly ILogWriter writer;

        private readonly SemaphoreSlim semaphoreSlim = new(1);

        public Logger(ILogWriter writer) => this.writer = writer;

        public void Info(string info, [CallerMemberName] string caller = "")
            => LogAsync(type: "INFO", caller: caller, message: info);
        public void Info(string info, object obj, [CallerMemberName] string caller = "")
            => LogAsync(type: "INFO", caller: caller, message: string.Format(info, obj));
        public void Info(Exception ex, string info = "", [CallerMemberName] string caller = "")
            => LogAsync(type: "INFO", caller: caller, message: $"{info}\n\t{ex}");

        public void Warn(string warning, [CallerMemberName] string caller = "")
            => LogAsync(type: "WARN", caller: caller, message: warning);
        public void Warn(Exception ex, string warning = "", [CallerMemberName] string caller = "")
            => LogAsync(type: "WARN", caller: caller, message: $"{warning}\n\t{ex}");

        public void Error(string error, [CallerMemberName] string caller = "")
            => LogAsync(type: "ERROR", caller: caller, message: error);
        public void Error(Exception ex, string error = "", [CallerMemberName] string caller = "")
            => LogAsync(type: "ERROR", caller: caller, message: $"{error}\n\t{ex}");
        public void UnhandledError(Exception ex, string error = "", [CallerMemberName] string caller = "")
            => LogSync(type: "ERROR", caller: caller, message: $"{error}\n\t{ex}");

        private void LogSync(string type, string caller, string message)
        {
            semaphoreSlim.Wait();
            try
            {
                writer.WriteLineToLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}|{type}|{caller}|{message}");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Writing to log file failed with the following exception:\n{e}");
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private async void LogAsync(string type, string caller, string message, int attemptNumber = 0)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                await writer.WriteLineToLogAsync($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}|{type}|{caller}|{message}");
            }
            catch (IOException e) when (e is not FileNotFoundException)
            {
                if (attemptNumber < 5) // check the attempt count to prevent a stack overflow exception
                {
                    // Log is likely in use by another process instance, so wait then try again
                    await Task.Delay(50);
                    LogAsync(type, caller, message, attemptNumber + 1);
                }
                else
                {
                    Debug.WriteLine($"Writing to log file failed after 5 attempts with the following exception:\n{e}");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Writing to log file failed with the following exception:\n{e}");
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
    }
}
