// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;
using SevenZip;
using System.IO;

namespace Files.App.ViewModels.Previews
{
	public sealed partial class ArchivePreviewViewModel : BasePreviewModel
	{
		public ArchivePreviewViewModel(ListedItem item)
			: base(item)
		{
		}

		public override async Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			var details = new List<FileProperty>();

			var zipResult = await FilesystemTasks.WrapNullable<SevenZipExtractor>(async () =>
			{
				var arch = new SevenZipExtractor(await PreviewFile.OpenStreamForReadAsync());

				// Force load archive (1665013614u)
				if (arch.ArchiveFileData is null)
				{
					arch.Dispose();
					return null;
				}

				return arch;
			});
			using var zipFile = zipResult.Result;

			if (zipFile is null)
			{
				// Loads the thumbnail preview
				_ = await base.LoadPreviewAndDetailsAsync();

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

			string propertyItemCount = Strings.DetailsArchiveItems.GetLocalizedFormatResource(zipFile.FilesCount, fileCount, folderCount);
			details.Add(GetFileProperty("PropertyItemCount", propertyItemCount));
			details.Add(GetFileProperty("PropertyUncompressedSize", totalSize.ToLongSizeString()));

			// Loads the thumbnail preview
			_ = await base.LoadPreviewAndDetailsAsync();
			return details;
		}
	}
}
