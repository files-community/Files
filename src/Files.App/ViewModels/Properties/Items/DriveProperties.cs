using Microsoft.Extensions.Logging;

namespace Files.App.ViewModels.Properties
{
	internal sealed class DriveProperties : BaseProperties
	{
		public DriveItem Drive { get; }

		public DriveProperties(SelectedItemsPropertiesViewModel viewModel, DriveItem driveItem, IShellPage instance)
		{
			ViewModel = viewModel;
			Drive = driveItem;
			AppInstance = instance;
			GetBaseProperties();
		}

		public override void GetBaseProperties()
		{
			if (Drive is null)
				return;

			//Drive.IconSource;
			ViewModel.CustomIconSource = null;

			ViewModel.IconData = Drive.IconData;

			// Drive.IconSource is not null && Drive.IconData is null;
			ViewModel.LoadCustomIcon = false;
			ViewModel.LoadFileIcon = Drive.IconData is not null;

			ViewModel.ItemName = Drive.Text;
			ViewModel.OriginalItemName = Drive.Text;

			// NOTE: If DriveType enum changes, the corresponding resource keys should change too
			ViewModel.ItemType = $"DriveType{Drive.Type}".GetLocalizedResource();
		}

		public async override Task GetSpecialPropertiesAsync()
		{
			ViewModel.ItemAttributesVisibility = false;

			var item = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(Drive.Path));
			BaseStorageFolder diskRoot = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(Drive.Path, item));

			if (ViewModel.LoadFileIcon)
			{
				var result = await FileThumbnailHelper.GetIconAsync(
					Drive.Path,
					Constants.ShellIconSizes.ExtraLarge,
					true,
					IconOptions.ReturnIconOnly | IconOptions.UseCurrentScale);

				if (result is not null)
					ViewModel.IconData = result;
				else
				{
					result = await FileThumbnailHelper.GetIconAsync(
						Drive.DeviceID,
						Constants.ShellIconSizes.ExtraLarge,
						true,
						IconOptions.ReturnIconOnly | IconOptions.UseCurrentScale); // For network shortcuts

					ViewModel.IconData = result;
				}
			}

			if (diskRoot is null || diskRoot.Properties is null)
			{
				ViewModel.LastSeparatorVisibility = false;

				return;
			}

			try
			{
				var syncRootStatus = await SyncRootHelpers.GetSyncRootQuotaAsync(Drive.Path);
				if (syncRootStatus.Success)
				{
					ViewModel.DriveCapacityValue = syncRootStatus.Capacity;
					ViewModel.DriveUsedSpaceValue = syncRootStatus.Used;
					ViewModel.DriveFreeSpaceValue = syncRootStatus.Capacity - syncRootStatus.Used;
					return;
				}
			}
			catch (Exception e)
			{
				App.Logger.LogWarning(e, "Failed to get sync root quota for path: {Path}", Drive.Path);
			}

			try
			{
				string freeSpace = "System.FreeSpace";
				string capacity = "System.Capacity";
				string fileSystem = "System.Volume.FileSystem";

				var properties = await diskRoot.Properties.RetrievePropertiesAsync([freeSpace, capacity, fileSystem]);

				ViewModel.DriveCapacityValue = (ulong)properties[capacity];
				ViewModel.DriveFreeSpaceValue = (ulong)properties[freeSpace];
				ViewModel.DriveUsedSpaceValue = ViewModel.DriveCapacityValue - ViewModel.DriveFreeSpaceValue;
				ViewModel.DriveFileSystem = (string)properties[fileSystem];
			}
			catch (Exception e)
			{
				ViewModel.LastSeparatorVisibility = false;
				App.Logger.LogWarning(e, e.Message);
			}
		}
	}
}
