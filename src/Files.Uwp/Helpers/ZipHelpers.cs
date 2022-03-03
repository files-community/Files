using Files.Filesystem.StorageItems;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Helpers
{
    public static class ZipHelpers
    {
        public static async Task ExtractArchive(BaseStorageFile archive, BaseStorageFolder destinationFolder, IProgress<float> progressDelegate, CancellationToken cancellationToken)
        {
            ZipFile zipFile = await Filesystem.FilesystemTasks.Wrap(async () => new ZipFile(await archive.OpenStreamForReadAsync()));
            if (zipFile == null)
            {
                return;
            }
            using (zipFile)
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
                var zipEncoding = ZipStorageFolder.DetectFileEncoding(zipFile);

                var directories = new List<string>();
                try
                {
                    directories.AddRange(directoryEntries.Select((entry) => wnt.TransformDirectory(ZipStorageFolder.DecodeEntryName(entry, zipEncoding))));
                    directories.AddRange(fileEntries.Select((entry) => Path.GetDirectoryName(wnt.TransformFile(ZipStorageFolder.DecodeEntryName(entry, zipEncoding)))));
                }
                catch (InvalidNameException ex)
                {
                    App.Logger.Warn(ex, $"Error transforming zip names into: {destinationFolder.Path}\n" +
                        $"Directories: {string.Join(", ", directoryEntries.Select(x => x.Name))}\n" +
                        $"Files: {string.Join(", ", fileEntries.Select(x => x.Name))}");
                    return;
                }

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

                    if (cancellationToken.IsCancellationRequested) // Check if canceled
                    {
                        return;
                    }
                }

                if (cancellationToken.IsCancellationRequested) // Check if canceled
                {
                    return;
                }

                // Fill files

                byte[] buffer = new byte[4096];
                int entriesAmount = fileEntries.Count;
                int entriesFinished = 0;

                foreach (var entry in fileEntries)
                {
                    if (cancellationToken.IsCancellationRequested) // Check if canceled
                    {
                        return;
                    }
                    if (entry.IsCrypted)
                    {
                        App.Logger.Info($"Skipped encrypted zip entry: {entry.Name}");
                        continue; // TODO: support password protected archives
                    }

                    string filePath = wnt.TransformFile(ZipStorageFolder.DecodeEntryName(entry, zipEncoding));

                    var hFile = NativeFileOperationsHelper.CreateFileForWrite(filePath);
                    if (hFile.IsInvalid)
                    {
                        return; // TODO: handle error
                    }

                    // We don't close hFile because FileStream.Dispose() already does that
                    using (FileStream destinationStream = new FileStream(hFile, FileAccess.Write))
                    {
                        int currentBlockSize = 0;

                        try
                        {
                            using (Stream entryStream = zipFile.GetInputStream(entry))
                            {
                                while ((currentBlockSize = await entryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await destinationStream.WriteAsync(buffer, 0, currentBlockSize);

                                    if (cancellationToken.IsCancellationRequested) // Check if cancelled
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            App.Logger.Warn(ex, $"Error extracting file: {filePath}");
                            return; // TODO: handle error
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