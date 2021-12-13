using Files.Common;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using static Vanara.PInvoke.Kernel32;

namespace FilesFullTrust
{
    internal class LogWriter : ILogWriter
    {
        private string logFilePath;
        private bool initialized = false;

        public async Task InitializeAsync(string name)
        {
            if (!initialized)
            {
                logFilePath = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, name);
                initialized = true;
            }
            await Task.CompletedTask;
        }

        public async Task WriteLineToLogAsync(string text)
        {
            await Task.Run(() => WriteLineToLog(text));
            Debug.WriteLine($"Logged event: {text}");
        }

        public void WriteLineToLog(string text)
        {
            if (logFilePath is null)
            {
                return;
            }
            using SafeHFILE hStream = CreateFile(logFilePath,
                FileAccess.GENERIC_WRITE, System.IO.FileShare.Read, null, System.IO.FileMode.OpenOrCreate, Vanara.PInvoke.FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
            if (hStream.IsInvalid)
            {
                return;
            }
            byte[] buff = Encoding.UTF8.GetBytes("\n" + text);
            SetFilePointer(hStream, 0, IntPtr.Zero, System.IO.SeekOrigin.End);
            WriteFile(hStream, buff, (uint)buff.Length, out var dwBytesWritten, IntPtr.Zero);

            Debug.WriteLine($"Logged event: {text}");
        }
    }
}