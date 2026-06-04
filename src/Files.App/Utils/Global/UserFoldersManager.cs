// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.IO;

namespace Files.App.Utils
{
	/// <summary>
	/// Resolves the current user's standard Windows known folders (Desktop, Documents, Downloads,
	/// Pictures, Music and Videos) and exposes them as sidebar locations.
	/// </summary>
	/// <remarks>
	/// Paths are resolved through the Windows Known Folder / Shell APIs (see <see cref="WindowsFolder"/>),
	/// so redirected folders (e.g. OneDrive-backed, domain-redirected or moved to another drive) point at
	/// their real location instead of a hard-coded <c>C:\Users\&lt;user&gt;\…</c> path.
	/// </remarks>
	public static class UserFoldersManager
	{
		private static readonly ILogger? logger = Ioc.Default.GetService<ILogger<App>>();

		public static EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;

		// The standard user known folders shown in the "User Folders" section, in display order.
		// The GUIDs are the canonical Windows KNOWNFOLDERID values.
		private static readonly Guid[] _knownFolderIds =
		[
			new("B4BFCC3A-DB2C-424C-B029-7FE99A87C641"), // FOLDERID_Desktop
			new("FDD39AD0-238F-46AF-ADB4-6C85480369C7"), // FOLDERID_Documents
			new("374DE290-123F-4565-9164-39C4925E467B"), // FOLDERID_Downloads
			new("33E28130-4E1E-4676-835A-98395C3BC3BB"), // FOLDERID_Pictures
			new("4BD8D571-6D19-48D3-BE97-422220080E43"), // FOLDERID_Music
			new("18989B1D-99B5-455B-841C-AB7C74E4DDFC"), // FOLDERID_Videos
		];

		private static readonly List<INavigationControlItem> _userFolders = [];
		public static IReadOnlyList<INavigationControlItem> UserFolderItems
		{
			get
			{
				lock (_userFolders)
					return _userFolders.ToList().AsReadOnly();
			}
		}

		/// <summary>
		/// Resolves the known folders and adds them to the sidebar section.
		/// </summary>
		public static async Task UpdateUserFoldersAsync()
		{
			// The section is hidden by default; only do the work when it is enabled.
			if (!Ioc.Default.GetRequiredService<IUserSettingsService>().GeneralSettingsService.ShowUserFoldersSection)
				return;

			// Resolving known folders through the Shell touches disk, so keep it off the UI thread.
			await Task.Run(() =>
			{
				foreach (var folderId in _knownFolderIds)
				{
					string path;
					string name;

					try
					{
						// Resolve through the Shell so the real (possibly redirected) path is used.
						using var shellFolder = new WindowsFolder(folderId);
						path = shellFolder.Id;
						name = shellFolder.Name;
					}
					catch (Exception ex)
					{
						logger?.LogInformation(ex, "Failed to resolve known folder {FolderId}.", folderId);
						continue;
					}

					// Skip folders that can't be resolved or no longer exist on disk.
					if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
						continue;

					var locationItem = new LocationItem()
					{
						Text = string.IsNullOrEmpty(name) ? Path.GetFileName(path.TrimEnd('\\')) : name,
						Path = path,
						Section = SectionType.UserFolders,
						IsDefaultLocation = false,
						MenuOptions = new ContextMenuOptions()
						{
							IsLocationItem = true,
							ShowProperties = true,
							ShowShellItems = true,
							// These are standard user folders, not pinned items, so no "Unpin from sidebar".
							ShowUnpinItem = false,
						},
					};

					lock (_userFolders)
					{
						// Avoid duplicates when two known folders resolve to the same path.
						if (_userFolders.Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase)))
							continue;

						_userFolders.Add(locationItem);
					}

					_ = LoadIconAsync(locationItem);

					DataChanged?.Invoke(
						SectionType.UserFolders,
						new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, locationItem));
				}
			});
		}

		private static async Task LoadIconAsync(LocationItem locationItem)
		{
			try
			{
				var iconData = await FileThumbnailHelper.GetIconAsync(
					locationItem.Path,
					Constants.ShellIconSizes.Small,
					true,
					IconOptions.ReturnIconOnly | IconOptions.UseCurrentScale);

				if (iconData is null)
					return;

				locationItem.IconData = iconData;

				await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async ()
					=> locationItem.Icon = await iconData.ToBitmapAsync());
			}
			catch (Exception ex)
			{
				logger?.LogWarning(ex, "Failed to load icon for user folder {Path}.", locationItem.Path);
			}
		}
	}
}
