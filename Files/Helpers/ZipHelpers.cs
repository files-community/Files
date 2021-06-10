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
                List<ZipEntry> directoryEntries = new List<ZipEntry>();
                List<ZipEntry> fileEntries = new List<ZipEntry>();

                ZipEntry entry;
                while ((entry = zipStream.GetNextEntry()) != null)
                {
                    if (entry.IsFile)
                    {
                        fileEntries.Add(entry);
                    }
                    else
                    {
                        directoryEntries.Add(entry);
                    }


                    //string directoryName = Path.GetDirectoryName(entry.Name);
                    //string fileName = Path.GetFileName(entry.Name);

                    //// Create directory
                    //if (!string.IsNullOrEmpty(directoryName))
                    //{
                    //    currentFolder = await currentFolder.CreateFolderAsync(Path.GetFileName(directoryName), CreationCollisionOption.OpenIfExists);
                    //}

                    //byte[] buffer = new byte[4096];
                    //if (!string.IsNullOrEmpty(fileName))
                    //{
                    //    StorageFile createdFile = await currentFolder.CreateFileAsync(fileName);
                    //    using (IRandomAccessStream destinationStream = await createdFile.OpenAsync(FileAccessMode.ReadWrite))
                    //    {
                    //        long totalBytes = 0L;
                    //        int currentBlockSize = 0;

                    //        while ((currentBlockSize = await zipStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    //        {
                    //            totalBytes += currentBlockSize;
                    //            await destinationStream.AsStreamForWrite().WriteAsync(buffer, 0, currentBlockSize);
                    //        }
                    //    }
                    //}
                }
            }
        }
    }
}
