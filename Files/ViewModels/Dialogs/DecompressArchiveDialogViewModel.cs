using Files.Helpers;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;

namespace Files.ViewModels.Dialogs
{
    public class DecompressArchiveDialogViewModel : ObservableObject
    {
        private StorageFile _archive;

        private StorageFolder _rootZipFolder;

        private StorageFolder _destinationFolder;

        public ICommand StartExtractingCommand { get; private set; }

        public DecompressArchiveDialogViewModel(StorageFile archive, StorageFolder destinationFolder)
        {
            this._archive = archive;
            this._destinationFolder = destinationFolder;

            // Create commands
            StartExtractingCommand = new RelayCommand(StartExtracting);
        }

        private async void StartExtracting()
        {
            // Check if archive still exists
            if (!StorageItemHelpers.Exists(_archive.Path))
            {
                return;
            }

            StorageFolder childDestinationFolder = await _destinationFolder.CreateFolderAsync(Path.GetFileNameWithoutExtension(_archive.Path), CreationCollisionOption.OpenIfExists);
            await ZipHelpers.ExtractArchive(_archive, childDestinationFolder, (progress) => Debug.WriteLine($"COMPLETED: {progress}%"));

            return;
            using (ZipInputStream zipStream = new ZipInputStream(await _archive.OpenStreamForReadAsync()))
            {
                ZipEntry entry;
                long fileSize = await StorageItemHelpers.GetFileSize(_archive);
                fileSize = (long)((float)fileSize * 1.3f); // This is a rough estimation for the output size

                _rootZipFolder = await _destinationFolder.CreateFolderAsync(Path.GetFileNameWithoutExtension(_archive.Path));
                StorageFolder currentFolder = _rootZipFolder;

                while ((entry = zipStream.GetNextEntry()) != null)
                {
                    string dirName = Path.GetDirectoryName(entry.Name);
                    string fileName = Path.GetFileName(entry.Name);

                    if (!string.IsNullOrEmpty(dirName))
                    {
                        currentFolder = await currentFolder.CreateFolderAsync(Path.GetFileName(dirName));
                    }

                    if (!string.IsNullOrEmpty(fileName))
                    {
                        using (Stream fileStream = await (await currentFolder.CreateFileAsync(fileName)).OpenStreamForWriteAsync())
                        {
                            const int size = 4096;
                            byte[] data = new byte[size];
                            long totalBytes = 0L;

                            while (true)
                            {
                                int read = await zipStream.ReadAsync(data, 0, data.Length);
                                totalBytes += read;

                                float percentage = (float)totalBytes * 100.0f / (float)fileSize;
                                Debug.WriteLine($"Completed: {percentage}%");

                                if (read > 0)
                                {
                                    await fileStream.WriteAsync(data, 0, read);
                                }
                                else
                                {
                                    percentage = 100.0f;
                                    Debug.WriteLine($"Completed: {percentage}%");
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
