// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Dispatching;
using System.IO;
using Windows.Storage.FileProperties;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;
using FileAttributes = System.IO.FileAttributes;

namespace Files.App.ViewModels.Properties
{
	public abstract class BaseProperties
	{
		public IShellPage AppInstance { get; }

		public SelectedItemsPropertiesViewModel ViewModel { get; }

		public CancellationTokenSource TokenSource { get; }

		public DispatcherQueue Dispatcher { get; }

		protected BaseProperties(
			SelectedItemsPropertiesViewModel viewModel,
			CancellationTokenSource tokenSource,
			DispatcherQueue dispatcher,
			IShellPage appInstance)
		{
			ViewModel = viewModel;
			TokenSource = tokenSource;
			Dispatcher = dispatcher;
			AppInstance = appInstance;
		}

		public abstract void GetBaseProperties();

		public abstract Task GetSpecialPropertiesAsync();

		public async Task GetOtherPropertiesAsync(IStorageItemExtraProperties properties)
		{
			string dateAccessedProperty = "System.DateAccessed";
			string dateModifiedProperty = "System.DateModified";

			List<string> propertiesName =
			[
				dateAccessedProperty,
				dateModifiedProperty
			];

			IDictionary<string, object> extraProperties = await properties.RetrievePropertiesAsync(propertiesName);

			// Cannot get date and owner in MTP devices
			ViewModel.ItemAccessedTimestampReal = (DateTimeOffset)(extraProperties[dateAccessedProperty] ?? DateTimeOffset.Now);
			ViewModel.ItemModifiedTimestampReal = (DateTimeOffset)(extraProperties[dateModifiedProperty] ?? DateTimeOffset.Now);
		}

		public async Task<(long size, long sizeOnDisk)> CalculateFolderSizeAsync(string path, CancellationToken token)
		{
			if (string.IsNullOrEmpty(path))
			{
				// In MTP devices calculating folder size would be too slow
				// Also should use StorageFolder methods instead of FindFirstFileEx
				return (0, 0);
			}

			long size = 0;
			long sizeOnDisk = 0;
			WIN32_FIND_DATAW findData = default;
			FindCloseSafeHandle hFile;
			unsafe
			{
				hFile = PInvoke.FindFirstFileEx(
					path + "\\*.*",
					FINDEX_INFO_LEVELS.FindExInfoBasic,
					&findData,
					FINDEX_SEARCH_OPS.FindExSearchNameMatch,
					FIND_FIRST_EX_FLAGS.FIND_FIRST_EX_LARGE_FETCH);
			}
			using FindCloseSafeHandle findHandleScope = hFile;

			var count = 0;
			if (!hFile.IsInvalid)
			{
				do
				{
					string fileName = findData.cFileName.ToString();
					if (((FileAttributes)findData.dwFileAttributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
						// Skip symbolic links and junctions
						continue;

					if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
					{
						size += findData.GetSize();
						var fileSizeOnDisk = Win32Helper.GetFileSizeOnDisk(Path.Combine(path, fileName));
						sizeOnDisk += fileSizeOnDisk ?? 0;
						++count;
						ViewModel.FilesCount++;
					}
					else if (fileName != "." && fileName != "..")
					{
						var itemPath = Path.Combine(path, fileName);

						var folderSize = await CalculateFolderSizeAsync(itemPath, token);
						size += folderSize.size;
						sizeOnDisk += folderSize.sizeOnDisk;
						++count;
						ViewModel.FoldersCount++;
					}

					if (size > ViewModel.ItemSizeBytes || sizeOnDisk > ViewModel.ItemSizeOnDiskBytes)
					{
						await Dispatcher.EnqueueOrInvokeAsync(() =>
						{
							ViewModel.ItemSizeBytes = size;
							ViewModel.ItemSize = size.ToSizeString();
							ViewModel.ItemSizeOnDiskBytes = sizeOnDisk;
							ViewModel.ItemSizeOnDisk = sizeOnDisk.ToSizeString();
							SetItemsCountString();
						},
						DispatcherQueuePriority.Low);
					}

					if (token.IsCancellationRequested)
						break;
				}
				while (PInvoke.FindNextFile(hFile, out findData));

				return (size, sizeOnDisk);
			}
			else
			{
				return (0, 0);
			}
		}

		public void SetItemsCountString()
		{
			ViewModel.FilesAndFoldersCountString = ViewModel.LocationsCount > 0
				? Strings.PropertiesFilesAndFoldersAndLocationsCount.GetLocalizedFormatResource(ViewModel.FilesCount, ViewModel.FoldersCount, ViewModel.LocationsCount)
				: Strings.PropertiesFilesAndFoldersCountString.GetLocalizedFormatResource(ViewModel.FilesCount, ViewModel.FoldersCount);
		}
	}
}
