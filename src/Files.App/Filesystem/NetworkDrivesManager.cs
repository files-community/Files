using Files.App.DataModels.NavigationControlItems;
using Files.App.Shell;
using System.Collections.Specialized;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace Files.App.Filesystem
{
	public class NetworkDrivesManager
	{
		public EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

		private readonly List<DriveItem> drives = new();
		public IReadOnlyList<DriveItem> Drives
		{
			get
			{
				lock (drives)
				{
					return drives.ToList().AsReadOnly();
				}
			}
		}

		public NetworkDrivesManager()
		{
			var networkItem = new DriveItem
			{
				DeviceID = "network-folder",
				Text = "Network".GetLocalizedResource(),
				Path = CommonPaths.NetworkFolderPath,
				Type = DriveType.Network,
				ItemType = NavigationControlItemType.Drive,
			};
			networkItem.MenuOptions = new ContextMenuOptions
			{
				IsLocationItem = true,
				ShowShellItems = true,
				ShowEjectDevice = networkItem.IsRemovable,
				ShowProperties = true
			};

			lock (drives)
			{
				drives.Add(networkItem);
			}
			DataChanged?.Invoke(SectionType.Network, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, networkItem));
		}

		public static Task<bool> OpenMapNetworkDriveDialogAsync(long hwnd)
			=> NetworkDrivesAPI.OpenMapNetworkDriveDialog(hwnd);

		public static bool DisconnectNetworkDrive(string drivePath)
			=> NetworkDrivesAPI.DisconnectNetworkDrive(drivePath);

		public async Task UpdateDrivesAsync()
		{
			var networkLocations = await Win32API.StartSTATask(() =>
			{
				var locations = new List<ShellLinkItem>();
				using (var nethood = new ShellFolder(Shell32.KNOWNFOLDERID.FOLDERID_NetHood))
				{
					foreach (var item in nethood)
					{
						if (item is ShellLink link)
						{
							locations.Add(ShellFolderExtensions.GetShellLinkItem(link));
						}
						else
						{
							var linkPath = (string)item.Properties["System.Link.TargetParsingPath"];
							if (linkPath is not null)
							{
								var linkItem = ShellFolderExtensions.GetShellFileItem(item);
								locations.Add(new ShellLinkItem(linkItem) { TargetPath = linkPath });
							}
						}
					}
				}
				return locations;
			});

			foreach (var item in networkLocations)
			{
				var networkItem = new DriveItem
				{
					Text = System.IO.Path.GetFileNameWithoutExtension(item.FileName),
					Path = item.TargetPath,
					DeviceID = item.FilePath,
					Type = DriveType.Network,
					ItemType = NavigationControlItemType.Drive,
				};
				networkItem.MenuOptions = new ContextMenuOptions
				{
					IsLocationItem = true,
					ShowEjectDevice = networkItem.IsRemovable,
					ShowShellItems = true,
					ShowProperties = true,
				};

				lock (drives)
				{
					if (drives.Any(x => x.Path == networkItem.Path))
					{
						continue;
					}
					drives.Add(networkItem);
				}
			}

			var orderedDrives = Drives
				.OrderByDescending(o => string.Equals(o.Text, "Network".GetLocalizedResource(), StringComparison.OrdinalIgnoreCase))
				.ThenBy(o => o.Text);
			foreach (var drive in orderedDrives)
			{
				DataChanged?.Invoke(SectionType.Network, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, drive));
			}
		}
	}
}