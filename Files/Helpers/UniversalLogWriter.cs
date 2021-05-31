using Files.Common;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Helpers
{
    /// <summary>
    /// UWP Implementation of ILogger
    /// </summary>
    public class UniversalLogWriter : ILogWriter
    {
        StorageFile logFile;
        private bool initialized = false;

        public async Task InitializeAsync(string name)
        {
            if(!initialized)
            {
                logFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(name, CreationCollisionOption.OpenIfExists);
                initialized = true;
            }
        }

        public async void WriteLineToLog(string text)
        {
            if(logFile is null)
            {
                return;
            }

            await FileIO.AppendTextAsync(logFile, $"\n{text}");
            Debug.WriteLine($"Logged event: {text}");
        }
    }
}
