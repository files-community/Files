using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Helpers
{
    public static class ZipHelpers
    {
        public static async Task ExtractArchive(StorageFile archive, StorageFolder destinationFolder, Action<float> progressDelegate, string password = null)
        {
            using (ZipFile zipFile = new ZipFile(await archive.OpenStreamForReadAsync()))
            {
                StorageFolder currentFolder = destinationFolder;
                if (!string.IsNullOrEmpty(password))
                {
                    zipFile.Password = password;
                }

                foreach (ZipEntry entry in zipFile)
                {
                    if (!entry.IsFile)
                    {
                        continue;
                    }

                    string fileName = Path.GetFileName(entry.Name);
                    string dirName = Path.GetDirectoryName(entry.Name);

                    byte[] buffer = new byte[4096];
                   
                    if (!string.IsNullOrEmpty(dirName))
                    {
                        currentFolder = await currentFolder.CreateFolderAsync(Path.GetFileName(dirName));
                    }

                    using (Stream zipStream = zipFile.GetInputStream(entry))
                    {
                        using (Stream fileStream = await (await currentFolder.CreateFileAsync(fileName)).OpenStreamForWriteAsync())
                        {
                            StreamUtils.Copy(
                                zipStream,
                                fileStream,
                                buffer,
                                new ProgressHandler((s, e) =>
                                {
                                    progressDelegate?.Invoke(e.PercentComplete);
                                }),
                                TimeSpan.FromSeconds(1),
                                null,
                                "ExtractProgressEvent");
                        }
                    }
                }

                zipFile.IsStreamOwner = true;
            }
        }
    }
}
