using Files.Common;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using static Files.Helpers.NativeFileOperationsHelper;

namespace Files.Helpers
{
    /// <summary>
    /// UWP Implementation of ILogger
    /// </summary>
    public class UniversalLogWriter : ILogWriter
    {
        private StorageFile logFile;
        private bool initialized = false;

        public async Task InitializeAsync(string name)
        {
            if (!initialized)
            {
                logFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(name, CreationCollisionOption.OpenIfExists);
                initialized = true;
            }
        }

        public async Task WriteLineToLogAsync(string text)
        {
            if (logFile is null)
            {
                return;
            }
            using var stream = await logFile.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders);
            using var outputStream = stream.GetOutputStreamAt(stream.Size);
            using var dataWriter = new DataWriter(outputStream);
            dataWriter.WriteString("\n" + text);
            await dataWriter.StoreAsync();
            await outputStream.FlushAsync();

            Debug.WriteLine($"Logged event: {text}");
        }

        public void WriteLineToLog(string text)
        {
            if (logFile is null)
            {
                return;
            }
            IntPtr hStream = CreateFileFromApp(logFile.Path,
                GENERIC_WRITE, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, (uint)File_Attributes.BackupSemantics, IntPtr.Zero);
            if (hStream.ToInt64() == -1)
            {
                return;
            }
            byte[] buff = Encoding.UTF8.GetBytes("\n" + text);
            int dwBytesWritten;
            unsafe
            {
                fixed (byte* pBuff = buff)
                {
                    SetFilePointer(hStream, 0, IntPtr.Zero, FILE_END);
                    WriteFile(hStream, pBuff, buff.Length, &dwBytesWritten, IntPtr.Zero);
                }
            }
            CloseHandle(hStream);

            Debug.WriteLine($"Logged event: {text}");
        }
    }
}