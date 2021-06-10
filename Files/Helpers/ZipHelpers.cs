using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Files.Helpers
{
    public static class ZipHelpers
    {
        public static async Task ExtractArchive(StorageFile archive, StorageFolder destinationFolder, IProgress<float> progressDelegate)
        {
            using (ZipFile zipFile = new ZipFile(await archive.OpenStreamForReadAsync()))
            {
                zipFile.IsStreamOwner = true;

                List<ZipEntry> directoryEntries = new List<ZipEntry>();
                List<ZipEntry> fileEntries = new List<ZipEntry>();

                foreach (ZipEntry entry in zipFile)
                {
                    if (entry.IsFile)
                    {
                        fileEntries.Add(entry);
                    }
                    else
                    {
                        directoryEntries.Add(entry);
                    }
                }

                // Create the directory tree using fast FTP

                var connection = await AppServiceConnectionHelper.Instance;

                string foldersString = string.Empty;
                string filesString = string.Empty;
                List<string> directories = directoryEntries.Select((item) => Path.Combine(destinationFolder.Path, item.Name)).ToList();
                List<string> files = fileEntries.Select((item) => Path.Combine(destinationFolder.Path, item.Name)).ToList();

                foreach (var item in directories)
                {
                    foldersString += $"{item}|";
                }
                foreach (var item in files)
                {
                    filesString += $"{item}|";
                }

                foldersString = foldersString.Remove(foldersString.Length - 1);
                filesString = filesString.Remove(filesString.Length - 1);

                await connection.SendMessageAsync(new ValueSet()
                {
                    { "Arguments", "FileOperation" },
                    { "fileop", "CreateDirectoryTree" },
                    { "paths", foldersString }
                });
                await connection.SendMessageAsync(new ValueSet()
                {
                    { "Arguments", "FileOperation" },
                    { "fileop", "CreateFilesTree" },
                    { "paths", filesString }
                });

                // Create files and fill them

                byte[] buffer = new byte[4096];
                long entriesAmount = fileEntries.Count;
                long entriesFinished = 0L;

                foreach (var entry in fileEntries)
                {
                    string filePath = Path.Combine(destinationFolder.Path, entry.Name.Replace('/', '\\'));

                    StorageFile receivedFile = await StorageItemHelpers.ToStorageItem<StorageFile>(filePath);

                    using (Stream destinationStream = (await receivedFile.OpenAsync(FileAccessMode.ReadWrite)).AsStreamForWrite())
                    {
                        int currentBlockSize = 0;

                        using (Stream entryStream = zipFile.GetInputStream(entry))
                        {
                            while ((currentBlockSize = await entryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await destinationStream.WriteAsync(buffer, 0, buffer.Length);
                            }
                        }
                    }

                    entriesFinished++;
                    float percentage = (float)((float)entriesFinished / (float)entriesAmount) * 100.0f;
                    progressDelegate?.Report(percentage);
                }
            }
        }
    }
}
