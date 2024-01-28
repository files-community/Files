// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.InteropServices;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace Files.App.Services
{
	/// <inheritdoc cref="INetworkDrivesService"/>
	public class NetworkDrivesService : INetworkDrivesService
	{
		private ObservableCollection<ILocatableFolder> _NetworkDrives = [];
		/// <inheritdoc/>
		public ObservableCollection<ILocatableFolder> NetworkDrives
		{
			get => _NetworkDrives;
			private set => NotifyPropertyChanged(nameof(_NetworkDrives));
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		public NetworkDrivesService()
		{
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
				ShowShellItems = true,
				ShowEjectDevice = networkItem.IsRemovable,
				ShowProperties = true
			};

			lock (_NetworkDrives)
				_NetworkDrives.Add(networkItem);
		}
		// Methods

		/// <inheritdoc/>
		public bool DisconnectNetworkDrive(ILocatableFolder drive)
		{
			return Mpr.WNetCancelConnection2(drive.Path.TrimEnd('\\'), Mpr.CONNECT.CONNECT_UPDATE_PROFILE, true) == Win32Error.NO_ERROR;
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<ILocatableFolder> GetDrivesAsync()
		{
			var networkLocations = await Win32API.StartSTATask(() =>
			{
				var locations = new List<ShellLinkItem>();

				using var folders = new ShellFolder(Shell32.KNOWNFOLDERID.FOLDERID_NetHood);

				foreach (var item in folders)
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

		/// <inheritdoc/>
		public Task OpenMapNetworkDriveDialogAsync()
		{
			var hWnd = MainWindow.Instance.WindowHandle.ToInt64();

			return Win32API.StartSTATask(() =>
			{
				using var dialog = new NetworkConnectionCommonDialog()
				{
					UseMostRecentPath = true,
					HideRestoreConnectionCheckBox = false,
				};

				return dialog.ShowDialog(Win32API.Win32Window.FromLong(hWnd)) == System.Windows.Forms.DialogResult.OK;
			});
		}

		/// <inheritdoc/>
		public async Task<bool> AuthenticateNetworkShare(string path)
		{
			Mpr.NETRESOURCE nr = new()
			{
				dwType = Mpr.NETRESOURCEType.RESOURCETYPE_DISK,
				lpRemoteName = path
			};

			// If credentials are saved, this will return NO_ERROR
			Win32Error connectionError = Mpr.WNetAddConnection3(HWND.NULL, nr, null, null, 0);

			if (connectionError == Win32Error.ERROR_LOGON_FAILURE)
			{
				var dialog = DynamicDialogFactory.GetFor_CredentialEntryDialog(path);
				await dialog.ShowAsync();

				if (dialog.ViewModel.AdditionalData is not string[] credentialsReturned || credentialsReturned[1] is null)
					return false;

				connectionError = Mpr.WNetAddConnection3(HWND.NULL, nr, credentialsReturned[1], credentialsReturned[0], 0);
				if (credentialsReturned[2] == "y" && connectionError == Win32Error.NO_ERROR)
				{
					AdvApi32.CREDENTIAL creds = new();
					creds.TargetName = new StrPtrAuto(path.Substring(2));
					creds.UserName = new StrPtrAuto(credentialsReturned[0]);
					creds.Type = AdvApi32.CRED_TYPE.CRED_TYPE_DOMAIN_PASSWORD;
					creds.AttributeCount = 0;
					creds.Persist = AdvApi32.CRED_PERSIST.CRED_PERSIST_ENTERPRISE;
					byte[] aPassword = Encoding.Unicode.GetBytes(credentialsReturned[1]);
					creds.CredentialBlobSize = (uint)aPassword.Length;
					creds.CredentialBlob = Marshal.StringToCoTaskMemUni(credentialsReturned[1]);
					AdvApi32.CredWrite(creds, 0);
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

		/// <inheritdoc/>
		public async Task RefreshNetworkDrivesAsync()
		{
			if (_NetworkDrives is null || NetworkDrives is null)
				return;

			var unsortedDrives = new List<ILocatableFolder>()
			{
				_NetworkDrives.Single(x => x is DriveItem o && o.DeviceID == "network-folder")
			};

			await foreach (ILocatableFolder item in GetDrivesAsync())
				unsortedDrives.Add(item);

			var orderedDrives = unsortedDrives.Cast<DriveItem>()
				.OrderByDescending(o => o.DeviceID == "network-folder")
				.ThenBy(o => o.Text);

			NetworkDrives.Clear();

			foreach (ILocatableFolder item in orderedDrives)
				NetworkDrives.AddIfNotPresent(item);
		}

		// Event Methods

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
