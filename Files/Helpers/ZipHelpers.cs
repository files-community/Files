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
using Windows.Storage.Streams;

namespace Files.Helpers
{
    public static class ZipHelpers
    {
        public static async Task ExtractArchive(StorageFile archive, StorageFolder destinationFolder, Action<float> progressDelegate)
        {
            using (ZipInputStream zipStream = new ZipInputStream(await archive.OpenStreamForReadAsync()))
            {
                IStorageFolder currentFolder = destinationFolder;
                ZipEntry theEntry;
                while ((theEntry = zipStream.GetNextEntry()) != null)
                {
                    string directoryName = Path.GetDirectoryName(theEntry.Name);
                    string fileName = Path.GetFileName(theEntry.Name);

                    // Create directory
                    if (!string.IsNullOrEmpty(directoryName))
                    {
                        currentFolder = await currentFolder.CreateFolderAsync(Path.GetFileName(directoryName), CreationCollisionOption.OpenIfExists);
                    }

                    byte[] buffer = new byte[4096];
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        StorageFile createdFile = await currentFolder.CreateFileAsync(fileName);
                        using (IRandomAccessStream fileStream = await createdFile.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            StreamUtils.Copy(zipStream, fileStream.AsStream(), buffer);
                        }
                    }
                }
            }
        }
    }
}
