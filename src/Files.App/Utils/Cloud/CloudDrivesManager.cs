// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.IO;
using Windows.Storage;

namespace Files.App.Utils.Cloud
{
	public static class CloudDrivesManager
	{
		private static readonly ILogger _logger = Ioc.Default.GetRequiredService<ILogger<App>>();
		private static readonly ICloudDetector _detector = Ioc.Default.GetRequiredService<ICloudDetector>();
		public static EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;
		private static readonly List<DriveItem> _Drives = [];

		public static IReadOnlyList<DriveItem> Drives
		{
			get
			{
				lock (_Drives)
				{
					return _Drives.ToList().AsReadOnly();
				}
			}
		}

		public static async Task UpdateDrivesAsync()
		{
			// Add each provider as its detector finishes, so a slow one (e.g. Google Drive's virtual
			// drive) never holds up the rest of the cloud drives or the sidebar.
			await foreach (var provider in _detector.DetectCloudProvidersProgressiveAsync())
			{
				_logger?.LogInformation($"Adding cloud provider {provider.ID} mapped to {LogPathHelper.RedactUserName(provider.SyncFolder)}");

				var cloudProviderItem = new DriveItem()
				{
					Text = provider.Name,
					Path = provider.SyncFolder,
					Type = Data.Items.DriveType.CloudDrive,
				};

				cloudProviderItem.MenuOptions = new ContextMenuOptions()
				{
					IsLocationItem = true,
					ShowEjectDevice = cloudProviderItem.IsRemovable,
					ShowShellItems = true,
					ShowProperties = true,
				};

				_ = LoadIconAsync(cloudProviderItem, provider);
				lock (_Drives)
				{
					if (_Drives.Any(x => x.Path == cloudProviderItem.Path))
						continue;

					_Drives.Add(cloudProviderItem);
				}
				DataChanged?.Invoke(
					SectionType.CloudDrives,
					new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, cloudProviderItem)
				);

				// Resolve the folder + properties off the critical path so a slow provider (e.g. Google Drive's virtual drive) doesn't delay the sidebar
				_ = SetRootAndUpdatePropertiesAsync(cloudProviderItem);
			}
		}

		private static async Task SetRootAndUpdatePropertiesAsync(DriveItem cloudProviderItem)
		{
			try
			{
				cloudProviderItem.Root = await StorageFolder.GetFolderFromPathAsync(cloudProviderItem.Path);

				await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => cloudProviderItem.UpdatePropertiesAsync());
			}
			catch (FileNotFoundException ex)
			{
				_logger?.LogInformation(ex, "Failed to find the cloud folder");
			}
			catch (UnauthorizedAccessException ex)
			{
				_logger?.LogInformation(ex, " Failed to access the cloud folder");
			}
			catch (Exception ex)
			{
				_logger?.LogWarning(ex, "Cloud provider local folder couldn't be found");
			}
		}

		private static async Task LoadIconAsync(DriveItem cloudProviderItem, ICloudProvider provider)
		{
			try
			{
				var iconData = provider.IconData;

				if (iconData is null)
				{
					var result = await FileThumbnailHelper.GetIconAsync(
						provider.SyncFolder,
						Constants.ShellIconSizes.Small,
						false,
						IconOptions.ReturnIconOnly);

					iconData = result;
				}

				if (iconData is not null)
				{
					cloudProviderItem.IconData = iconData;

					await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async ()
						=> cloudProviderItem.Icon = await iconData.ToBitmapAsync());
				}
			}
			catch (Exception ex)
			{
				_logger?.LogWarning(ex, "Failed to load icon for cloud provider \"{ProviderName}\"", provider.Name);
			}
		}
	}
}
