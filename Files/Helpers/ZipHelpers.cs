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

                var wnt = new WindowsNameTransform(destinationFolder.Path);

                var directories = new List<string>();
                directories.AddRange(directoryEntries.Select((item) => wnt.TransformDirectory(item.Name)));
                directories.AddRange(fileEntries.Select((item) => Path.GetDirectoryName(wnt.TransformFile(item.Name))));
                foreach (var dir in directories.Distinct().OrderBy(x => x.Length))
                {
                    if (!NativeFileOperationsHelper.CreateDirectoryFromApp(dir, IntPtr.Zero))
                    {
                        var dirName = destinationFolder.Path;
                        foreach (var component in dir.Substring(destinationFolder.Path.Length).Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
                        {
                            dirName = Path.Combine(dirName, component);
                            NativeFileOperationsHelper.CreateDirectoryFromApp(dirName, IntPtr.Zero);
                        }
                    }
                }

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

                    string filePath = wnt.TransformFile(entry.Name);

                    var hFile = NativeFileOperationsHelper.CreateFileForWrite(filePath);
                    if (hFile.IsInvalid)
                    {
                        return; // TODO: handle error
                    }

                    using (FileStream destinationStream = new FileStream(hFile, FileAccess.ReadWrite))
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
