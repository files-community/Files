using ByteSizeLib;
using Files.Extensions;
using Files.Filesystem;
using Files.ViewModels.Properties;
using SevenZip;
using Microsoft.Toolkit.Uwp;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Files.ViewModels.Previews
{
    public class ArchivePreviewViewModel : BasePreviewModel
    {
        public static List<string> Extensions => new List<string>()
        {
            ".zip", ".7z", ".rar"
        };

        public ArchivePreviewViewModel(ListedItem item) : base(item)
        {
        }

        public override async Task<List<FileProperty>> LoadPreviewAndDetails()
        {
            var details = new List<FileProperty>();
            using SevenZipExtractor zipFile = await FilesystemTasks.Wrap(async () => new SevenZipExtractor(await Item.ItemFile.OpenStreamForReadAsync()));
            if (zipFile == null || zipFile.ArchiveFileData == null)
            {
                _ = await base.LoadPreviewAndDetails(); // Loads the thumbnail preview
                return details;
            }
            //zipFile.IsStreamOwner = true;

            var folderCount = 0;
            var fileCount = 0;
            ulong totalSize = 0;

            foreach (ArchiveFileInfo entry in zipFile.ArchiveFileData)
            {
                if (!entry.IsDirectory)
                {
                    fileCount++;
                    totalSize += entry.Size;
                }
                else
                {
                    folderCount++;
                }
            }

            details.Add(new FileProperty()
            {
                NameResource = "PropertyItemCount",
                Value = string.Format("DetailsArchiveItemCount".GetLocalized(), zipFile.ArchiveFileData.Count, fileCount, folderCount),
            });

            details.Add(new FileProperty()
            {
                NameResource = "PropertyUncompressedSize",
                Value = $"{ByteSize.FromBytes(totalSize).ToBinaryString().ConvertSizeAbbreviation()} ({ByteSize.FromBytes(totalSize).Bytes:#,##0} {"ItemSizeBytes".GetLocalized()})",
            });

            _ = await base.LoadPreviewAndDetails(); // Loads the thumbnail preview
            return details;
        }
    }
}