using Files.App.Filesystem;
using Files.App.UserControls.Widgets;
using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.StartScreen;

namespace Files.App.Helpers
{
	public sealed class JumpListManager
	{
		private JumpList instance = null;
		private List<string> JumpListItemPaths { get; set; }

		public JumpListManager()
		{
			JumpListItemPaths = new List<string>();
		}

		public async Task InitializeAsync()
		{
			try
			{
				if (JumpList.IsSupported())
				{
					instance = await JumpList.LoadCurrentAsync();
					App.QuickAccessManager.UpdateQuickAccessWidget += QuickAccessManager_DataChanged;

					QuickAccessManager_DataChanged(null, null);

					// Disable automatic jumplist. It doesn't work with Files UWP.
					instance.SystemGroupKind = JumpListSystemGroupKind.None;
					JumpListItemPaths = instance.Items.Select(item => item.Arguments).ToList();
				}
			}
			catch (Exception ex)
			{
				App.Logger.Warn(ex, ex.Message);
				instance = null;
			}
		}

		public async void AddFolderToJumpList(string path)
		{
			// Saving to jumplist may fail randomly with error: ERROR_UNABLE_TO_REMOVE_REPLACED
			// In that case app should just catch the error and proceed as usual
			try
			{
				if (instance is not null)
				{
					AddFolder(path, "ms-resource:///Resources/JumpListRecentGroupHeader");
					await instance.SaveAsync();
				}
			}
			catch { }
		}

		private void AddFolder(string path, string group)
		{
			if (instance is not null)
			{
				string displayName = null;
				if (path.EndsWith("\\"))
				{
					// Jumplist item argument can't end with a slash so append a character that can't exist in a directory name to support listing drives.
					var drive = App.DrivesManager.Drives.Where(drive => drive.Path == path).FirstOrDefault();
					if (drive is null)
						return;

					displayName = drive.Text;
					path += '?';
				}

				if (displayName is null)
				{
					if (path.Equals(CommonPaths.DesktopPath, StringComparison.OrdinalIgnoreCase))
						displayName = "ms-resource:///Resources/Desktop";
					else if (path.Equals(CommonPaths.DownloadsPath, StringComparison.OrdinalIgnoreCase))
						displayName = "ms-resource:///Resources/Downloads";
					else if (path.Equals(CommonPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
					{
						var localSettings = ApplicationData.Current.LocalSettings;
						displayName = localSettings.Values.Get("RecycleBin_Title", "Recycle Bin");
					}
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

				if (string.Equals(group, "ms-resource:///Resources/JumpListRecentGroupHeader", StringComparison.OrdinalIgnoreCase))
				{
					// Keep newer items at the top.
					instance.Items.Remove(instance.Items.FirstOrDefault(x => x.Arguments.Equals(path, StringComparison.OrdinalIgnoreCase)));
					instance.Items.Insert(0, jumplistItem);

					JumpListItemPaths.Remove(JumpListItemPaths.FirstOrDefault(x => x.Equals(path, StringComparison.OrdinalIgnoreCase)));
					JumpListItemPaths.Add(path);
				}
				else
				{
					var pinnedItemsCount = instance.Items.Where(x => x.GroupName == "ms-resource:///Resources/JumpListPinnedGroupHeader").Count();
					instance.Items.Insert(pinnedItemsCount, jumplistItem);
				}
			}
		}

		public async void RemoveFolder(string path)
		{
			// Updating the jumplist may fail randomly with error: FileLoadException: File in use
			// In that case app should just catch the error and proceed as usual
			try
			{
				if (instance is null)
					return;

				if (JumpListItemPaths.Remove(path))
				{
					var itemToRemove = instance.Items.Where(x => x.Arguments == path).Select(x => x).FirstOrDefault();
					instance.Items.Remove(itemToRemove);
					await instance.SaveAsync();
				}
			}
			catch { }
		}

		private async void QuickAccessManager_DataChanged(object sender, ModifyQuickAccessEventArgs e)
		{
			if (instance is null)
				return;

			var itemsToRemove = instance.Items.Where(x => string.Equals(x.GroupName, "ms-resource:///Resources/JumpListPinnedGroupHeader", StringComparison.OrdinalIgnoreCase)).ToList();
			itemsToRemove.ForEach(x => instance.Items.Remove(x));
			App.QuickAccessManager.Model.FavoriteItems.ForEach(x => AddFolder(x, "ms-resource:///Resources/JumpListPinnedGroupHeader"));
			await instance.SaveAsync();
		}
	}
}
