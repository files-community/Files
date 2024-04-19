// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;
using System.Text;
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
		/// <inheritdoc/>
		public ObservableCollection<ILocatableFolder> Drives
		{
			get => _Drives;
			private set => SetProperty(ref _Drives, value);
		}

		/// <summary>
		/// Initializes an instance of <see cref="NetworkDrivesService"/>.
		/// </summary>
		public NetworkDrivesService()
		{
			_Drives = [];

			var networkItem = new DriveItem()
			{
				DeviceID = "network-folder",
				Text = "Network".GetLocalizedResource(),
				Path = Constants.UserEnvironmentPaths.NetworkFolderPath,
				Type = DriveType.Network,
				ItemType = NavigationControlItemType.Drive,
			};

			networkItem.MenuOptions = new ContextMenuOptions()
			{
				IsLocationItem = true,
				ShowEjectDevice = networkItem.IsRemovable,
				ShowShellItems = true,
				ShowProperties = true,
			};
			lock (_Drives)
				_Drives.Add(networkItem);
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<ILocatableFolder> GetDrivesAsync()
		{
			var networkLocations = await Win32Helper.StartSTATask(() =>
			{
				var locations = new List<ShellLinkItem>();
				using (var netHood = new ShellFolder(Shell32.KNOWNFOLDERID.FOLDERID_NetHood))
				{
					foreach (var item in netHood)
					{
						if (item is ShellLink link)
						{
							locations.Add(ShellFolderExtensions.GetShellLinkItem(link));
						}
						else
						{
							var linkPath = (string?)item?.Properties["System.Link.TargetParsingPath"];
							if (linkPath is not null)
							{
								var linkItem = ShellFolderExtensions.GetShellFileItem(item);
								locations.Add(new(linkItem) { TargetPath = linkPath });
							}
						}
					}
				}

				return locations;
			});

			foreach (var item in networkLocations ?? Enumerable.Empty<ShellLinkItem>())
			{
				var networkItem = new DriveItem()
				{
					Text = SystemIO.Path.GetFileNameWithoutExtension(item.FileName),
					Path = item.TargetPath,
					DeviceID = item.FilePath,
					Type = DriveType.Network,
					ItemType = NavigationControlItemType.Drive,
				};

				networkItem.MenuOptions = new ContextMenuOptions()
				{
					IsLocationItem = true,
					ShowEjectDevice = networkItem.IsRemovable,
					ShowShellItems = true,
					ShowProperties = true,
				};
				yield return networkItem;
			}
		}

		/// <inheritdoc/>
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

		/// <inheritdoc/>
		public bool DisconnectNetworkDrive(ILocatableFolder drive)
		{
			return WNetCancelConnection2(drive.Path.TrimEnd('\\'), CONNECT.CONNECT_UPDATE_PROFILE, true).Succeeded;
		}

		/// <inheritdoc/>
		public Task OpenMapNetworkDriveDialogAsync()
		{
			var hWnd = MainWindow.Instance.WindowHandle.ToInt64();

			return Win32Helper.StartSTATask(() =>
			{
				using var ncd = new NetworkConnectionDialog
				{
					UseMostRecentPath = true,
					HideRestoreConnectionCheckBox = false
				};

				return ncd.ShowDialog(Win32Helper.Win32Window.FromLong(hWnd)) == System.Windows.Forms.DialogResult.OK;
			});
		}

		/// <inheritdoc/>
		public async Task<bool> AuthenticateNetworkShare(string path)
		{
			var netRes = new NETRESOURCE()
			{
				dwType = NETRESOURCEType.RESOURCETYPE_DISK,
				lpRemoteName = path
			};

			// If credentials are saved, this will return NO_ERROR
			Win32Error connectionError = WNetAddConnection3(HWND.NULL, netRes, null, null, 0);

			if (connectionError == Win32Error.ERROR_LOGON_FAILURE || connectionError == Win32Error.ERROR_ACCESS_DENIED)
			{
				var dialog = DynamicDialogFactory.GetFor_CredentialEntryDialog(path);
				await dialog.ShowAsync();
				var credentialsReturned = dialog.ViewModel.AdditionalData as string[];

				if (credentialsReturned is not null && credentialsReturned[1] != null)
				{
					connectionError = WNetAddConnection3(HWND.NULL, netRes, credentialsReturned[1], credentialsReturned[0], 0);
					if (credentialsReturned[2] == "y" && connectionError == Win32Error.NO_ERROR)
					{
						var creds = new CREDENTIAL
						{
							TargetName = new StrPtrAuto(path.Substring(2)),
							UserName = new StrPtrAuto(credentialsReturned[0]),
							Type = CRED_TYPE.CRED_TYPE_DOMAIN_PASSWORD,
							AttributeCount = 0,
							Persist = CRED_PERSIST.CRED_PERSIST_ENTERPRISE
						};

						byte[] bPassword = Encoding.Unicode.GetBytes(credentialsReturned[1]);
						creds.CredentialBlobSize = (uint)bPassword.Length;
						creds.CredentialBlob = Marshal.StringToCoTaskMemUni(credentialsReturned[1]);
						CredWrite(creds, 0);
					}
				}
				else
				{
					return false;
				}
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
