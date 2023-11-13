// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using Windows.Storage;

namespace Files.App.Utils.Cloud
{
	public static class CloudDrivesManager
	{
		private static readonly ILogger _logger = Ioc.Default.GetRequiredService<ILogger<App>>();

		private static readonly ICloudDetector _detector = Ioc.Default.GetRequiredService<ICloudDetector>();

		public static EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

		private static readonly List<DriveItem> _Drives = new();
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
			var providers = await _detector.DetectCloudProvidersAsync();
			if (providers is null)
				return;

			foreach (var provider in providers)
			{
				_logger?.LogInformation($"Adding cloud provider \"{provider.Name}\" mapped to {provider.SyncFolder}");

				var cloudProviderItem = new DriveItem()
				{
					Text = provider.Name,
					Path = provider.SyncFolder,
					Type = DriveType.CloudDrive,
				};

				try
				{
					cloudProviderItem.Root = await StorageFolder.GetFolderFromPathAsync(cloudProviderItem.Path);

					_ = MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => cloudProviderItem.UpdatePropertiesAsync());
				}
				catch (Exception ex)
				{
					_logger?.LogWarning(ex, "Cloud provider local folder couldn't be found");
				}

				cloudProviderItem.MenuOptions = new ContextMenuOptions()
				{
					IsLocationItem = true,
					ShowEjectDevice = cloudProviderItem.IsRemovable,
					ShowShellItems = true,
					ShowProperties = true,
				};

				var iconData = provider.IconData ?? await FileThumbnailHelper.LoadIconWithoutOverlayAsync(provider.SyncFolder, 24);
				if (iconData is not null)
				{
					cloudProviderItem.IconData = iconData;

					await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async ()
						=> cloudProviderItem.Icon = await iconData.ToBitmapAsync());
				}

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
			}
		}
	}
}
