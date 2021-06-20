using Files.Common;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;

namespace FilesFullTrust
{
    internal class LogWriter : ILogWriter
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

        public async Task WriteLineToLog(string text)
        {
            if (logFile is null)
            {
                return;
            }
            await FileIO.AppendTextAsync(logFile, $"\n{text}");
            Debug.WriteLine($"Logged event: {text}");
        }
    }
}