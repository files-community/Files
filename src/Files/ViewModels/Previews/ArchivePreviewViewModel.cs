using ByteSizeLib;
using Files.Extensions;
using Files.Filesystem;
using Files.ViewModels.Properties;
using SevenZipExtractor;
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
            using ArchiveFile zipFile = new ArchiveFile(await Item.ItemFile.OpenStreamForReadAsync());
            zipFile.IsStreamOwner = true;

            var folderCount = 0;
            var fileCount = 0;
            ulong totalSize = 0;

            foreach (ZipEntry entry in zipFile.Entries)
            {
                if (!entry.IsFolder)
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
                Value = string.Format("DetailsArchiveItemCount".GetLocalized(), zipFile.Entries.Count, fileCount, folderCount),
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