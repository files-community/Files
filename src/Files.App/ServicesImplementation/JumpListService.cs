using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.ViewModels;
using Files.Shared.Extensions;
using Files.Shared.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.StartScreen;

namespace Files.App.ServicesImplementation
{
	public class JumpListService : IJumpListService
	{
		private const string JumpListRecentGroupHeader = "ms-resource:///Resources/JumpListRecentGroupHeader";
		private const string JumpListPinnedGroupHeader = "ms-resource:///Resources/JumpListPinnedGroupHeader";

		public async Task AddFolderAsync(string path)
		{
			if (JumpList.IsSupported())
			{
				var instance = await JumpList.LoadCurrentAsync();
				// Disable automatic jumplist. It doesn't work.
				instance.SystemGroupKind = JumpListSystemGroupKind.None;

				// Saving to jumplist may fail randomly with error: ERROR_UNABLE_TO_REMOVE_REPLACED
				// In that case app should just catch the error and proceed as usual
				try
				{
					if (instance is not null)
					{
						AddFolder(path, JumpListRecentGroupHeader, instance);
						await instance.SaveAsync();
					}
				}
				catch { }
			}
		}

		public async Task<IEnumerable<string>> GetFoldersAsync()
		{
			if (JumpList.IsSupported())
			{
				try
				{
					var instance = await JumpList.LoadCurrentAsync();
					// Disable automatic jumplist. It doesn't work.
					instance.SystemGroupKind = JumpListSystemGroupKind.None;

					return instance.Items.Select(item => item.Arguments).ToList();
				}
				catch
				{
					return Enumerable.Empty<string>();
				}
			}
			else
			{
				return Enumerable.Empty<string>();
			}
		}

		public async Task RefreshPinnedFoldersAsync()
		{
			try
			{
				if (JumpList.IsSupported())
				{
					var instance = await JumpList.LoadCurrentAsync();
					// Disable automatic jumplist. It doesn't work with Files UWP.
					instance.SystemGroupKind = JumpListSystemGroupKind.None;

					if (instance is null)
						return;

					var itemsToRemove = instance.Items.Where(x => string.Equals(x.GroupName, JumpListPinnedGroupHeader, StringComparison.OrdinalIgnoreCase)).ToList();
					itemsToRemove.ForEach(x => instance.Items.Remove(x));
					App.QuickAccessManager.Model.FavoriteItems.ForEach(x => AddFolder(x, JumpListPinnedGroupHeader, instance));
					await instance.SaveAsync();
				}
			}
			catch
			{
			}
		}

		public async Task RemoveFolderAsync(string path)
		{
			if (JumpList.IsSupported())
			{
				try
				{
					var instance = await JumpList.LoadCurrentAsync();
					// Disable automatic jumplist. It doesn't work.
					instance.SystemGroupKind = JumpListSystemGroupKind.None;

					var itemToRemove = instance.Items.Where(x => x.Arguments == path).Select(x => x).FirstOrDefault();
					instance.Items.Remove(itemToRemove);
					await instance.SaveAsync();
				}
				catch { }
			}
		}

		private void AddFolder(string path, string group, JumpList instance)
		{
			if (instance is not null)
			{
				string? displayName = null;
				if (path.EndsWith("\\"))
				{
					var drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

					// Jumplist item argument can't end with a slash so append a character that can't exist in a directory name to support listing drives.
					var drive = drivesViewModel.Drives.Where(drive => drive.Path == path).FirstOrDefault();
					if (drive is null)
						return;

					displayName = (drive as DriveItem)?.Text;
					path += '?';
				}

				if (displayName is null)
				{
					var localSettings = ApplicationData.Current.LocalSettings;
					if (path.Equals(CommonPaths.DesktopPath, StringComparison.OrdinalIgnoreCase))
						displayName = "ms-resource:///Resources/Desktop";
					else if (path.Equals(CommonPaths.DownloadsPath, StringComparison.OrdinalIgnoreCase))
						displayName = "ms-resource:///Resources/Downloads";
					else if (path.Equals(CommonPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
						displayName = "RecycleBin".GetLocalizedResource();
					else if (path.Equals(CommonPaths.MyComputerPath, StringComparison.OrdinalIgnoreCase))
						displayName = "ThisPC".GetLocalizedResource();
					else if (path.Equals(CommonPaths.NetworkFolderPath, StringComparison.OrdinalIgnoreCase))
						displayName = "SidebarNetworkDrives".GetLocalizedResource();
					else if (App.LibraryManager.TryGetLibrary(path, out LibraryLocationItem library))
					{
						var libName = Path.GetFileNameWithoutExtension(library.Path);
						switch (libName)
						{
							case "Documents":
							case "Pictures":
							case "Music":
							case "Videos":
								// Use localized name
								displayName = $"ms-resource:///Resources/{libName}";
								break;

							default:
								// Use original name
								displayName = library.Text;
								break;
						}
					}
					else
						displayName = Path.GetFileName(path);
				}

				var jumplistItem = JumpListItem.CreateWithArguments(path, displayName);
				jumplistItem.Description = jumplistItem.Arguments;
				jumplistItem.GroupName = group;
				jumplistItem.Logo = new Uri("ms-appx:///Assets/FolderIcon.png");

				if (string.Equals(group, JumpListRecentGroupHeader, StringComparison.OrdinalIgnoreCase))
				{
					// Keep newer items at the top.
					instance.Items.Remove(instance.Items.FirstOrDefault(x => x.Arguments.Equals(path, StringComparison.OrdinalIgnoreCase)));
					instance.Items.Insert(0, jumplistItem);
				}
				else
				{
					var pinnedItemsCount = instance.Items.Where(x => x.GroupName == JumpListPinnedGroupHeader).Count();
					instance.Items.Insert(pinnedItemsCount, jumplistItem);
				}
			}
		}
	}
}