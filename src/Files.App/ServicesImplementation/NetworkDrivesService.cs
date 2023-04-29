// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Data.Items;
using Files.App.Shell;
using Files.Backend.Services;
using Files.Sdk.Storage.LocatableStorage;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace Files.App.ServicesImplementation
{
	public class NetworkDrivesService : INetworkDrivesService
	{
		public bool DisconnectNetworkDrive(ILocatableFolder drive)
		{
			return NetworkDrivesAPI.DisconnectNetworkDrive(drive.Path);    
		}

		public async IAsyncEnumerable<ILocatableFolder> GetDrivesAsync()
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

			foreach (var item in networkLocations ?? Enumerable.Empty<ShellLinkItem>())
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

				yield return networkItem;
			}
		}

		public Task OpenMapNetworkDriveDialogAsync()
		{
			var handle = NativeWinApiHelper.CoreWindowHandle.ToInt64();
			return NetworkDrivesAPI.OpenMapNetworkDriveDialog(handle);
		}
	}
}
