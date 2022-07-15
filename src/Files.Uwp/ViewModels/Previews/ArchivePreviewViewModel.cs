using Files.Uwp.Extensions;
using Files.Uwp.Filesystem;
using Files.Uwp.ViewModels.Properties;
using SevenZip;
using Microsoft.Toolkit.Uwp;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Files.Uwp.ViewModels.Previews
{
    public class ArchivePreviewViewModel : BasePreviewModel
    {
        public static List<string> Extensions => new List<string>()
        {
            ".zip", ".7z", ".rar"
        };

        public ArchivePreviewViewModel(ListedItem item) : base(item) {}

        public override async Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
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
                    ++fileCount;
                    totalSize += entry.Size;
                }
            }

            details.Add(new FileProperty()
            {
                NameResource = "PropertyItemCount",
                Value = string.Format("DetailsArchiveItemCount".GetLocalized(), zipFile.Count, fileCount, folderCount),
            });

            string propertyItemCount = string.Format("DetailsArchiveItemCount".GetLocalized(), zipFile.Count, fileCount, folderCount);
            details.Add(GetFileProperty("PropertyItemCount", propertyItemCount));
            details.Add(GetFileProperty("PropertyUncompressedSize", totalSize.ToLongSizeString()));

            _ = await base.LoadPreviewAndDetailsAsync(); // Loads the thumbnail preview
            return details;
        }
    }
}