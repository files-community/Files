using Files.Backend.Extensions;
using Files.Shared.Services.DateTimeFormatter;
using Microsoft.UI.Dispatching;
using System.IO;
using Windows.Storage.FileProperties;
using static Files.Backend.Helpers.NativeFindStorageItemHelper;
using FileAttributes = System.IO.FileAttributes;

namespace Files.App.ViewModels.Properties
{
	public abstract class BaseProperties
	{
		protected static readonly IDateTimeFormatter dateTimeFormatter = Ioc.Default.GetService<IDateTimeFormatter>();

		public IShellPage AppInstance { get; set; } = null;

		public SelectedItemsPropertiesViewModel ViewModel { get; set; }

		public CancellationTokenSource TokenSource { get; set; }

		public DispatcherQueue Dispatcher { get; set; }

		public abstract void GetBaseProperties();

		public abstract Task GetSpecialProperties();

		public async Task GetOtherProperties(IStorageItemExtraProperties properties)
		{
			string dateAccessedProperty = "System.DateAccessed";

			List<string> propertiesName = new()
			{
				dateAccessedProperty
			};

			IDictionary<string, object> extraProperties = await properties.RetrievePropertiesAsync(propertiesName);

			// Cannot get date and owner in MTP devices
			ViewModel.ItemAccessedTimestamp = dateTimeFormatter.ToShortLabel((DateTimeOffset)(extraProperties[dateAccessedProperty] ?? DateTimeOffset.Now));
		}

		public async Task<long> CalculateFolderSizeAsync(string path, CancellationToken token)
		{
			if (string.IsNullOrEmpty(path))
			{
				// In MTP devices calculating folder size would be too slow
				// Also should use StorageFolder methods instead of FindFirstFileExFromApp
				return 0;
			}

			long size = 0;
			FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
			int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

			IntPtr hFile = FindFirstFileExFromApp(
				path + "\\*.*",
				findInfoLevel,
				out WIN32_FIND_DATA findData,
				FINDEX_SEARCH_OPS.FindExSearchNameMatch,
				IntPtr.Zero,
				additionalFlags);

			var count = 0;
			if (hFile.ToInt64() != -1)
			{
				do
				{
					if (((FileAttributes)findData.dwFileAttributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
						// Skip symbolic links and junctions
						continue;

					if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
					{
						size += findData.GetSize();
						++count;
						ViewModel.FilesCount++;
					}
					else if (findData.cFileName != "." && findData.cFileName != "..")
					{
						var itemPath = Path.Combine(path, findData.cFileName);

						size += await CalculateFolderSizeAsync(itemPath, token);
						++count;
						ViewModel.FoldersCount++;
					}

					if (size > ViewModel.ItemSizeBytes)
					{
						await Dispatcher.EnqueueOrInvokeAsync(() =>
						{
							ViewModel.ItemSizeBytes = size;
							ViewModel.ItemSize = size.ToSizeString();
							SetItemsCountString();
						},
						DispatcherQueuePriority.Low);
					}

					if (token.IsCancellationRequested)
						break;
				}
				while (FindNextFile(hFile, out findData));

				FindClose(hFile);

				return size;
			}
			else
			{
				return 0;
			}
		}

		public void SetItemsCountString()
		{
			if (ViewModel.LocationsCount > 0)
			{
				ViewModel.FilesAndFoldersCountString = string.Format("PropertiesFilesFoldersAndLocationsCountString".GetLocalizedResource(), ViewModel.FilesCount, ViewModel.FoldersCount, ViewModel.LocationsCount);
			}
			else
			{
				ViewModel.FilesAndFoldersCountString = string.Format("PropertiesFilesAndFoldersCountString".GetLocalizedResource(), ViewModel.FilesCount, ViewModel.FoldersCount);
			}
		}
	}
}
