using Microsoft.Extensions.Logging;

namespace Files.App.ViewModels.Properties
{
	internal sealed class DriveProperties : BaseProperties
	{
		public DriveItem Drive { get; }

		public DriveProperties(
			SelectedItemsPropertiesViewModel viewModel,
			CancellationTokenSource tokenSource,
			Microsoft.UI.Dispatching.DispatcherQueue dispatcher,
			DriveItem driveItem,
			IShellPage instance)
			: base(viewModel, tokenSource, dispatcher, instance)
		{
			Drive = driveItem;
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
			var drivePath = Drive.GetRequiredPath();

			var rootResult = await FilesystemTasks.WrapNullable(() => DriveHelpers.GetRootFromPathAsync(drivePath));
			var diskRootResult = await FilesystemTasks.WrapNullable(
				() => StorageFileExtensions.DangerousGetFolderFromPathAsync(drivePath, rootResult.Result));
			var diskRoot = diskRootResult.Result;

			if (ViewModel.LoadFileIcon)
			{
				var result = await FileThumbnailHelper.GetIconAsync(
					drivePath,
					Constants.ShellIconSizes.ExtraLarge,
					true,
					IconOptions.ReturnIconOnly);

				if (result is not null)
					ViewModel.IconData = result;
				else
				{
					result = await FileThumbnailHelper.GetIconAsync(
						Drive.DeviceID,
						Constants.ShellIconSizes.ExtraLarge,
						true,
						IconOptions.ReturnIconOnly); // For network shortcuts

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
				var syncRootStatus = await SyncRootHelpers.GetSyncRootQuotaAsync(drivePath);
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
				App.Logger.LogWarning(e, "Failed to get sync root quota for path: {Path}", LogPathHelper.RedactPath(Drive.Path));
			}

			try
			{
				string freeSpace = "System.FreeSpace";
				string capacity = "System.Capacity";
				string fileSystem = "System.Volume.FileSystem";

				var properties = await diskRoot.Properties.RetrievePropertiesAsync((string[])[freeSpace, capacity, fileSystem]);

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
