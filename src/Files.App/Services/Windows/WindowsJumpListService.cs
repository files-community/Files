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

		// JumpList is a shared app resource and can throw sharing violations if
		// multiple navigation-triggered updates save it at the same time.
		private readonly SemaphoreSlim jumpListSemaphore = new(1, 1);

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
			await jumpListSemaphore.WaitAsync();

			try
			{
				var instance = await LoadCurrentJumpListAsync();

				// Saving to jumplist may fail randomly with error: ERROR_UNABLE_TO_REMOVE_REPLACED
				// In that case app should just catch the error and proceed as usual
				if (instance is not null)
				{
					AddFolder(path, JumpListRecentGroupHeader, instance);
					await instance.SaveAsync();
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}
			finally
			{
				jumpListSemaphore.Release();
			}
		}

		public async Task<IEnumerable<string>> GetFoldersAsync()
		{
			await jumpListSemaphore.WaitAsync();

			try
			{
				var instance = await LoadCurrentJumpListAsync();

				return instance?.Items.Select(item => item.Arguments).ToList() ?? [];
			}
			catch
			{
				return [];
			}
			finally
			{
				jumpListSemaphore.Release();
			}
		}

		public async Task RefreshPinnedFoldersAsync()
		{
			await jumpListSemaphore.WaitAsync();

			try
			{
				if (App.QuickAccessManager.PinnedItemsWatcher is not null)
					App.QuickAccessManager.PinnedItemsWatcher.EnableRaisingEvents = false;

				var instance = await LoadCurrentJumpListAsync();

				if (instance is null)
					return;

				var itemsToRemove = instance.Items.Where(x => string.Equals(x.GroupName, JumpListPinnedGroupHeader, StringComparison.OrdinalIgnoreCase)).ToList();
				itemsToRemove.ForEach(x => instance.Items.Remove(x));
				App.QuickAccessManager.Model.PinnedFolders.ForEach(x => AddFolder(x, JumpListPinnedGroupHeader, instance));
				await instance.SaveAsync();
			}
			catch
			{
			}
			finally
			{
				if (App.QuickAccessManager.PinnedItemsWatcher is not null)
					SafetyExtensions.IgnoreExceptions(() => App.QuickAccessManager.PinnedItemsWatcher.EnableRaisingEvents = true);

				jumpListSemaphore.Release();
			}
		}

		public async Task RemoveFolderAsync(string path)
		{
			await jumpListSemaphore.WaitAsync();

			try
			{
				var instance = await LoadCurrentJumpListAsync();

				if (instance is null)
					return;

				var itemToRemove = instance.Items.Where(x => x.Arguments == path).Select(x => x).FirstOrDefault();
				if (itemToRemove is not null)
					instance.Items.Remove(itemToRemove);

				await instance.SaveAsync();
			}
			catch { }
			finally
			{
				jumpListSemaphore.Release();
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
					var drive = drivesViewModel.Drives.FirstOrDefault(drive => drive.Id == path);
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
					else if (App.LibraryManager.TryGetLibrary(path, out LibraryLocationItem library))
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

		private static async Task<JumpList?> LoadCurrentJumpListAsync()
		{
			if (!JumpList.IsSupported())
				return null;

			var instance = await JumpList.LoadCurrentAsync();

			// Disable automatic jumplist. It doesn't work.
			instance.SystemGroupKind = JumpListSystemGroupKind.None;

			return instance;
		}
	}
}
