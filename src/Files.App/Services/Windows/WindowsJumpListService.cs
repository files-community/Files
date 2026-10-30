// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.IO;
using Windows.UI.StartScreen;

namespace Files.App.Services
{
	public sealed class WindowsJumpListService : IWindowsJumpListService
	{
		private const string JumpListRecentGroupHeader = "ms-resource:///Resources/JumpListRecentGroupHeader";
		private const string JumpListPinnedGroupHeader = "ms-resource:///Resources/JumpListPinnedGroupHeader";

		// SaveAsync throws ArgumentException ("Maximum number of items exceeded") once the list grows past
		// the shell's cap, so the recent group must stay bounded. The taskbar displays at most ~13 entries.
		private const int MaxRecentItemsCount = 20;

		// The Windows Jump List is a single shared resource. Load -> modify -> Save sequences must never overlap,
		// otherwise concurrent navigations (fire-and-forget calls from ShellViewModel) race on the native list and
		// fail with COMException (ERROR_SHARING_VIOLATION / ERROR_ALREADY_EXISTS) or crash the app.
		private static readonly SemaphoreSlim jumpListLock = new(1, 1);

		public async Task InitializeAsync()
		{
			try
			{
				App.QuickAccessManager.UpdateQuickAccessWidget -= UpdateQuickAccessWidget_Invoked;
				App.QuickAccessManager.UpdateQuickAccessWidget += UpdateQuickAccessWidget_Invoked;

				await RefreshPinnedFoldersAsync();
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}
		}

		public async Task AddFolderAsync(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return;

			await jumpListLock.WaitAsync();
			try
			{
				if (JumpList.IsSupported())
				{
					var instance = await JumpList.LoadCurrentAsync();
					// Disable automatic jumplist. It doesn't work.
					instance.SystemGroupKind = JumpListSystemGroupKind.None;

					// Saving to jumplist may fail randomly with error: ERROR_UNABLE_TO_REMOVE_REPLACED
					// In that case app should just catch the error and proceed as usual
					if (instance is not null)
					{
						AddFolder(path, JumpListRecentGroupHeader, instance);
						await instance.SaveAsync();
					}
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}
			finally
			{
				jumpListLock.Release();
			}
		}

		public async Task<IEnumerable<string>> GetFoldersAsync()
		{
			if (JumpList.IsSupported())
			{
				await jumpListLock.WaitAsync();
				try
				{
					var instance = await JumpList.LoadCurrentAsync();
					// Disable automatic jumplist. It doesn't work.
					instance.SystemGroupKind = JumpListSystemGroupKind.None;

					return instance.Items.Select(item => item.Arguments).ToList();
				}
				catch
				{
					return [];
				}
				finally
				{
					jumpListLock.Release();
				}
			}
			else
			{
				return [];
			}
		}

		public async Task RefreshPinnedFoldersAsync()
		{
			await jumpListLock.WaitAsync();
			try
			{
				if (App.QuickAccessManager.PinnedItemsWatcher is not null)
					App.QuickAccessManager.PinnedItemsWatcher.EnableRaisingEvents = false;

				if (JumpList.IsSupported())
				{
					var instance = await JumpList.LoadCurrentAsync();
					// Disable automatic jumplist. It doesn't work with Files UWP.
					instance.SystemGroupKind = JumpListSystemGroupKind.None;

					if (instance is null)
						return;

					var itemsToRemove = instance.Items.Where(x => string.Equals(x.GroupName, JumpListPinnedGroupHeader, StringComparison.OrdinalIgnoreCase)).ToList();
					itemsToRemove.ForEach(x => instance.Items.Remove(x));
					App.QuickAccessManager.Model.PinnedFolders.ForEach(x => AddFolder(x, JumpListPinnedGroupHeader, instance));
					await instance.SaveAsync();
				}
			}
			catch
			{
			}
			finally
			{
				if (App.QuickAccessManager.PinnedItemsWatcher is not null)
					SafetyExtensions.IgnoreExceptions(() => App.QuickAccessManager.PinnedItemsWatcher.EnableRaisingEvents = true);

				jumpListLock.Release();
			}
		}

		public async Task RemoveFolderAsync(string path)
		{
			if (JumpList.IsSupported())
			{
				await jumpListLock.WaitAsync();
				try
				{
					var instance = await JumpList.LoadCurrentAsync();
					// Disable automatic jumplist. It doesn't work.
					instance.SystemGroupKind = JumpListSystemGroupKind.None;

					var itemToRemove = instance.Items.FirstOrDefault(x => x.Arguments == path);
					if (itemToRemove is null)
						return;

					instance.Items.Remove(itemToRemove);
					await instance.SaveAsync();
				}
				catch { }
				finally
				{
					jumpListLock.Release();
				}
			}
		}

		private void AddFolder(string path, string group, JumpList instance)
		{
			if (instance is not null)
			{
				string? displayName = null;

				if (path.StartsWith("\\\\SHELL", StringComparison.OrdinalIgnoreCase))
					displayName = Strings.ThisPC.GetLocalizedResource();

				if (path.EndsWith('\\'))
				{
					var drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

					// Jumplist item argument can't end with a slash so append a character that can't exist in a directory name to support listing drives.
					// Drives is a UI-bound collection that can change while we run on a background thread, so snapshot it.
					var drive = drivesViewModel.Drives.ToArray().FirstOrDefault(drive => drive.Id == path);
					if (drive is null)
						return;

					displayName = (drive as DriveItem)?.Text;
					path += '?';
				}

				if (displayName is null)
				{
					if (path.Equals(Constants.UserEnvironmentPaths.DesktopPath, StringComparison.OrdinalIgnoreCase))
						displayName = "ms-resource:///Resources/Desktop";
					else if (path.Equals(Constants.UserEnvironmentPaths.DownloadsPath, StringComparison.OrdinalIgnoreCase))
						displayName = "ms-resource:///Resources/Downloads";
					else if (path.Equals(Constants.UserEnvironmentPaths.NetworkFolderPath, StringComparison.OrdinalIgnoreCase))
						displayName = Strings.Network.GetLocalizedResource();
					else if (path.Equals(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
						displayName = Strings.RecycleBin.GetLocalizedResource();
					else if (path.Equals(Constants.UserEnvironmentPaths.MyComputerPath, StringComparison.OrdinalIgnoreCase))
						displayName = Strings.ThisPC.GetLocalizedResource();
					else if (App.LibraryManager.TryGetLibrary(path, out var library))
					{
						var libName = Path.GetFileNameWithoutExtension(library.Path);
						displayName = libName switch
						{
							"Documents" or "Pictures" or "Music" or "Videos" => $"ms-resource:///Resources/{libName}",// Use localized name
							_ => library.Text,// Use original name
						};
					}
					else
						displayName = Path.GetFileName(path);
				}

				var jumplistItem = Windows.UI.StartScreen.JumpListItem.CreateWithArguments(path, displayName);
				jumplistItem.Description = jumplistItem.Arguments ?? string.Empty;
				jumplistItem.GroupName = group;
				jumplistItem.Logo = new Uri("ms-appx:///Assets/FolderIcon.png");

				if (string.Equals(group, JumpListRecentGroupHeader, StringComparison.OrdinalIgnoreCase))
				{
					// Keep newer items at the top.
					var existingItem = instance.Items.FirstOrDefault(x => string.Equals(x.Arguments, path, StringComparison.OrdinalIgnoreCase));
					if (existingItem is not null)
						instance.Items.Remove(existingItem);

					instance.Items.Insert(0, jumplistItem);

					var excessRecentItems = instance.Items
						.Where(x => string.Equals(x.GroupName, JumpListRecentGroupHeader, StringComparison.OrdinalIgnoreCase))
						.Skip(MaxRecentItemsCount)
						.ToList();

					foreach (var item in excessRecentItems)
						instance.Items.Remove(item);
				}
				else
				{
					var pinnedItemsCount = instance.Items.Count(x => x.GroupName == JumpListPinnedGroupHeader);
					instance.Items.Insert(pinnedItemsCount, jumplistItem);
				}
			}
		}

		private async void UpdateQuickAccessWidget_Invoked(object? sender, ModifyQuickAccessEventArgs e)
		{
			await RefreshPinnedFoldersAsync();
		}
	}
}
