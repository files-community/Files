// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;
using System.IO;
using Windows.Storage;
using Windows.Win32;

namespace Files.App.Services
{
	internal class StorageArchiveService
	{
		private StatusCenterViewModel StatusCenterViewModel { get; } = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

		public bool CanCompress(IReadOnlyList<ListedItem> items)
		{
			return
				CanDecompress(items) is false ||
				items.Count > 1;
		}

		public bool CanDecompress(IReadOnlyList<ListedItem> items)
		{
			return
				items.Any() &&
				(items.All(x => x.IsArchive) ||
				items.All(x =>
					x.PrimaryItemAttribute == StorageItemTypes.File &&
					FileExtensionHelpers.IsZipFile(x.FileExtension)));
		}

		public async Task CompressAsync(ICompressArchiveModel creator)
		{
			var archivePath = creator.GetArchivePath();

			int index = 1;

			while (File.Exists(archivePath) || Directory.Exists(archivePath))
				archivePath = creator.GetArchivePath($" ({++index})");

			creator.ArchivePath = archivePath;

			var banner = StatusCenterHelper.AddCard_Compress(
				creator.Sources,
				archivePath.CreateEnumerable(),
				ReturnResult.InProgress,
				creator.Sources.Count());

			creator.Progress = banner.ProgressEventSource;
			creator.CancellationToken = banner.CancellationToken;

			bool isSuccess = await creator.RunCreationAsync();

			StatusCenterViewModel.RemoveItem(banner);

			if (isSuccess)
			{
				StatusCenterHelper.AddCard_Compress(
					creator.Sources,
					archivePath.CreateEnumerable(),
					ReturnResult.Success,
					creator.Sources.Count());
			}
			else
			{
				PInvoke.DeleteFileFromApp(archivePath);

				StatusCenterHelper.AddCard_Compress(
					creator.Sources,
					archivePath.CreateEnumerable(),
					creator.CancellationToken.IsCancellationRequested
						? ReturnResult.Cancelled
						: ReturnResult.Failed,
					creator.Sources.Count());
			}
		}
	}
}
