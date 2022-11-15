using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.ViewModels.Properties;
using SevenZip;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Files.App.ViewModels.Previews
{
	public class ArchivePreviewViewModel : BasePreviewModel
	{
		public ArchivePreviewViewModel(ListedItem item) : base(item) { }

		public override async Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			var details = new List<FileProperty>();
			using SevenZipExtractor zipFile = await FilesystemTasks.Wrap(async () =>
			{
				var arch = new SevenZipExtractor(await Item.ItemFile.OpenStreamForReadAsync());
				return arch?.ArchiveFileData is null ? null : arch; // Force load archive (1665013614u)
			});
			if (zipFile is null)
			{
				_ = await base.LoadPreviewAndDetailsAsync(); // Loads the thumbnail preview
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
			folderCount = (int)zipFile.FilesCount - fileCount;

			string propertyItemCount = string.Format("DetailsArchiveItemCount".GetLocalizedResource(), zipFile.FilesCount, fileCount, folderCount);
			details.Add(GetFileProperty("PropertyItemCount", propertyItemCount));
			details.Add(GetFileProperty("PropertyUncompressedSize", totalSize.ToLongSizeString()));

			_ = await base.LoadPreviewAndDetailsAsync(); // Loads the thumbnail preview
			return details;
		}
	}
}