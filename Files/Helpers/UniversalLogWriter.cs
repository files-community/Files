using Files.Common;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

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

        public async Task WriteLineToLog(string text)
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
    }
}