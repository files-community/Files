using Files.Uwp.Extensions;
using Files.Uwp.Filesystem;
using Files.Uwp.ViewModels.Properties;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Toolkit.Uwp;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Files.Uwp.ViewModels.Previews
{
    public class ArchivePreviewViewModel : BasePreviewModel
    {
        public ArchivePreviewViewModel(ListedItem item) : base(item) {}

        public override async Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
        {
            var details = new List<FileProperty>();
            using ZipFile zipFile = new(await Item.ItemFile.OpenStreamForReadAsync());
            zipFile.IsStreamOwner = true;

            long fileCount = 0;
            long folderCount;
            long totalSize = 0;

            foreach (ZipEntry entry in zipFile)
            {
                if (entry.IsFile)
                {
                    ++fileCount;
                    totalSize += entry.Size;
                }
            }
            folderCount = zipFile.Count - fileCount;

            string propertyItemCount = string.Format("DetailsArchiveItemCount".GetLocalized(), zipFile.Count, fileCount, folderCount);
            details.Add(GetFileProperty("PropertyItemCount", propertyItemCount));
            details.Add(GetFileProperty("PropertyUncompressedSize", totalSize.ToLongSizeString()));

            _ = await base.LoadPreviewAndDetailsAsync(); // Loads the thumbnail preview
            return details;
        }
    }
}