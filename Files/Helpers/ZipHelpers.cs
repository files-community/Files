using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Files.Helpers
{
    public static class ZipHelpers
    {
        public static async Task ExtractArchive(StorageFile archive, StorageFolder destinationFolder, IProgress<float> progressDelegate, CancellationToken cancellationToken)
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


                if (cancellationToken.IsCancellationRequested) // Check if cancelled
                {
                    return;
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

                // Create folders
                await connection.SendMessageAsync(new ValueSet()
                {
                    { "Arguments", "FileOperation" },
                    { "fileop", "CreateDirectoryTree" },
                    { "paths", foldersString }
                });
                // Create files
                await connection.SendMessageAsync(new ValueSet()
                {
                    { "Arguments", "FileOperation" },
                    { "fileop", "CreateFilesTree" },
                    { "paths", filesString }
                });

                if (cancellationToken.IsCancellationRequested) // Check if cancelled
                {
                    return;
                }

                // Fill files

                byte[] buffer = new byte[4096];
                int entriesAmount = fileEntries.Count;
                int entriesFinished = 0;

                foreach (var entry in fileEntries)
                {
                    if (cancellationToken.IsCancellationRequested) // Check if cancelled
                    {
                        return;
                    }

                    string filePath = Path.Combine(destinationFolder.Path, entry.Name.Replace('/', '\\'));

                    HandleContext handleContext = new HandleContext(filePath);

                    using (FileStream destinationStream = new FileStream(handleContext.hFile, FileAccess.ReadWrite))
                    {
                        int currentBlockSize = 0;

                        using (Stream entryStream = zipFile.GetInputStream(entry))
                        {
                            while ((currentBlockSize = await entryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await destinationStream.WriteAsync(buffer, 0, buffer.Length);

                                if (cancellationToken.IsCancellationRequested) // Check if cancelled
                                {
                                    return;
                                }
                            }
                        }
                    }
                    // We don't close handleContext because FileStream.Dispose() already does that

                    entriesFinished++;
                    float percentage = (float)((float)entriesFinished / (float)entriesAmount) * 100.0f;
                    progressDelegate?.Report(percentage);
                }
            }
        }
    }
}
