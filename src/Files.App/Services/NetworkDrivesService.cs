// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Shell;
using Files.Core.Storage.LocatableStorage;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Mpr;

namespace Files.App.Services
{
	public sealed class NetworkDrivesService : ObservableObject, INetworkDrivesService
	{
		private ObservableCollection<ILocatableFolder> _Drives;
		public ObservableCollection<ILocatableFolder> Drives
		{
			get => _Drives;
			private set => SetProperty(ref _Drives, value);
		}

		public NetworkDrivesService()
		{
			_Drives = [];

			var networkItem = new DriveItem
			{
				DeviceID = "network-folder",
				Text = "Network".GetLocalizedResource(),
				Path = Constants.UserEnvironmentPaths.NetworkFolderPath,
				Type = DriveType.Network,
				ItemType = NavigationControlItemType.Drive,
				MenuOptions = new()
				{
					IsLocationItem = true,
					ShowShellItems = true,
					ShowProperties = true,
				},
			};

			networkItem.MenuOptions.ShowEjectDevice = networkItem.IsRemovable;

			lock (_Drives)
				_Drives.Add(networkItem);
		}

		public async IAsyncEnumerable<ILocatableFolder> GetDrivesAsync()
		{
			var networkLocations = await Win32Helper.StartSTATask(() =>
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

		public async Task UpdateDrivesAsync()
		{
			var unsortedDrives = new List<ILocatableFolder>()
			{
				_Drives.Single(x => x is DriveItem o && o.DeviceID == "network-folder")
			};

			await foreach (ILocatableFolder item in GetDrivesAsync())
				unsortedDrives.Add(item);

			var orderedDrives =
				unsortedDrives.Cast<DriveItem>()
					.OrderByDescending(o => o.DeviceID == "network-folder")
					.ThenBy(o => o.Text);

			Drives.Clear();

			foreach (ILocatableFolder item in orderedDrives)
				Drives.AddIfNotPresent(item);
		}

		public bool DisconnectNetworkDrive(ILocatableFolder drive)
		{
			return WNetCancelConnection2(drive.Path.TrimEnd('\\'), CONNECT.CONNECT_UPDATE_PROFILE, true).Succeeded;
		}

		public Task OpenMapNetworkDriveDialogAsync()
		{
			var hWnd = MainWindow.Instance.WindowHandle.ToInt64();

			return Win32Helper.StartSTATask(() =>
			{
				using var ncd = new NetworkConnectionDialog { UseMostRecentPath = true };
				ncd.HideRestoreConnectionCheckBox = false;
				return ncd.ShowDialog(Win32Helper.Win32Window.FromLong(hWnd)) == System.Windows.Forms.DialogResult.OK;
			});
		}

		public async Task<bool> AuthenticateNetworkShare(string path)
		{
			var nr = new NETRESOURCE()
			{
				dwType = NETRESOURCEType.RESOURCETYPE_DISK,
				lpRemoteName = path
			};

			// If credentials are saved, this will return NO_ERROR
			Win32Error connectionError = WNetAddConnection3(HWND.NULL, nr, null, null, 0);

			if (connectionError == Win32Error.ERROR_LOGON_FAILURE || connectionError == Win32Error.ERROR_ACCESS_DENIED)
			{
				var dialog = DynamicDialogFactory.GetFor_CredentialEntryDialog(path);
				await dialog.ShowAsync();
				var credentialsReturned = dialog.ViewModel.AdditionalData as string[];

				if (credentialsReturned is string[] && credentialsReturned[1] != null)
				{
					connectionError = WNetAddConnection3(HWND.NULL, nr, credentialsReturned[1], credentialsReturned[0], 0);
					if (credentialsReturned[2] == "y" && connectionError == Win32Error.NO_ERROR)
					{
						CREDENTIAL creds = new CREDENTIAL();
						creds.TargetName = new StrPtrAuto(path.Substring(2));
						creds.UserName = new StrPtrAuto(credentialsReturned[0]);
						creds.Type = CRED_TYPE.CRED_TYPE_DOMAIN_PASSWORD;
						creds.AttributeCount = 0;
						creds.Persist = CRED_PERSIST.CRED_PERSIST_ENTERPRISE;
						byte[] bpassword = Encoding.Unicode.GetBytes(credentialsReturned[1]);
						creds.CredentialBlobSize = (UInt32)bpassword.Length;
						creds.CredentialBlob = Marshal.StringToCoTaskMemUni(credentialsReturned[1]);
						CredWrite(creds, 0);
					}
				}
				else
					return false;
			}

			if (connectionError == Win32Error.NO_ERROR)
			{
				return true;
			}
			else
			{
				await DialogDisplayHelper.ShowDialogAsync("NetworkFolderErrorDialogTitle".GetLocalizedResource(), connectionError.ToString().Split(":")[1].Trim());

				return false;
			}
		}
	}
}
