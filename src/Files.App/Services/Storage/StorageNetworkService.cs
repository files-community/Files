// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.WNet;
using Windows.Win32.Security.Credentials;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;

namespace Files.App.Services
{
	public sealed class NetworkService : ObservableObject, INetworkService
	{
		private ICommonDialogService CommonDialogService { get; } = Ioc.Default.GetRequiredService<ICommonDialogService>();

		private ObservableCollection<ILocatableFolder> _Computers = [];
		/// <inheritdoc/>
		public ObservableCollection<ILocatableFolder> Computers
		{
			get => _Computers;
			private set => SetProperty(ref _Computers, value);
		}

		private ObservableCollection<ILocatableFolder> _Shortcuts = [];
		/// <inheritdoc/>
		public ObservableCollection<ILocatableFolder> Shortcuts
		{
			get => _Shortcuts;
			private set => SetProperty(ref _Shortcuts, value);
		}

		/// <summary>
		/// Initializes an instance of <see cref="NetworkService"/>.
		/// </summary>
		public NetworkService()
		{
			var item = new DriveItem()
			{
				DeviceID = "network-folder",
				Text = "Network".GetLocalizedResource(),
				Path = Constants.UserEnvironmentPaths.NetworkFolderPath,
				Type = DriveType.Network,
				ItemType = NavigationControlItemType.Drive,
				MenuOptions = new ContextMenuOptions()
				{
					IsLocationItem = true,
					ShowShellItems = true,
					ShowProperties = true,
				},
			};

			item.MenuOptions.ShowEjectDevice = item.IsRemovable;

			lock (_Computers)
				_Computers.Add(item);
		}

		/// <inheritdoc/>
		public async Task<IEnumerable<ILocatableFolder>> GetComputersAsync()
		{
			return await Task.Run(GetComputers);

			unsafe IEnumerable<ILocatableFolder> GetComputers()
			{
				HRESULT hr = default;

				// Get IShellItem of the shell folder
				var shellItemIid = typeof(IShellItem).GUID;
				using ComPtr<IShellItem> pFolderShellItem = default;
				fixed (char* pszFolderShellPath = "Shell:::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}")
					hr = PInvoke.SHCreateItemFromParsingName(pszFolderShellPath, null, &shellItemIid, (void**)pFolderShellItem.GetAddressOf());

				// Get IEnumShellItems of the shell folder
				var enumItemsBHID = PInvoke.BHID_EnumItems;
				Guid enumShellItemIid = typeof(IEnumShellItems).GUID;
				using ComPtr<IEnumShellItems> pEnumShellItems = default;
				hr = pFolderShellItem.Get()->BindToHandler(null, &enumItemsBHID, &enumShellItemIid, (void**)pEnumShellItems.GetAddressOf());

				// Enumerate items and populate the list
				List<ILocatableFolder> items = [];
				using ComPtr<IShellItem> pShellItem = default;
				while (pEnumShellItems.Get()->Next(1, pShellItem.GetAddressOf()) == HRESULT.S_OK)
				{
					// Get only folders
					if (pShellItem.Get()->GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var attribute) == HRESULT.S_OK &&
						(attribute & SFGAO_FLAGS.SFGAO_FOLDER) is not SFGAO_FLAGS.SFGAO_FOLDER)
						continue;

					// Get the display name
					pShellItem.Get()->GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY, out var szDisplayName);
					var fileName = szDisplayName.ToString();
					PInvoke.CoTaskMemFree(szDisplayName.Value);

					// Get the file system path on disk
					pShellItem.Get()->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out szDisplayName);
					var filePath = szDisplayName.ToString();
					PInvoke.CoTaskMemFree(szDisplayName.Value);

					var item = new DriveItem()
					{
						Text = fileName,
						Path = filePath,
						DeviceID = filePath,
						Type = DriveType.Network,
						ItemType = NavigationControlItemType.Drive,
						MenuOptions = new()
						{
							IsLocationItem = true,
							ShowShellItems = true,
							ShowProperties = true,
						},
					};

					item.MenuOptions.ShowEjectDevice = item.IsRemovable;

					items.Add(item);
				}

				return items;
			}
		}

		/// <inheritdoc/>
		public async Task<IEnumerable<ILocatableFolder>> GetShortcutsAsync()
		{
			return await Task.Run(GetShortcuts);

			unsafe IEnumerable<ILocatableFolder> GetShortcuts()
			{
				// Get IShellItem of the known folder
				using ComPtr<IShellItem> pShellFolder = default;
				var folderId = PInvoke.FOLDERID_NetHood;
				var shellItemIid = typeof(IShellItem).GUID;
				HRESULT hr = PInvoke.SHGetKnownFolderItem(&folderId, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, HANDLE.Null, &shellItemIid, (void**)pShellFolder.GetAddressOf());

				// Get IEnumShellItems for Recycle Bin folder
				using ComPtr<IEnumShellItems> pEnumShellItems = default;
				Guid enumShellItemGuid = typeof(IEnumShellItems).GUID;
				var enumItemsBHID = BHID.BHID_EnumItems;
				hr = pShellFolder.Get()->BindToHandler(null, &enumItemsBHID, &enumShellItemGuid, (void**)pEnumShellItems.GetAddressOf());

				List<ILocatableFolder> items = [];
				using ComPtr<IShellItem> pShellItem = default;
				while (pEnumShellItems.Get()->Next(1, pShellItem.GetAddressOf()) == HRESULT.S_OK)
				{
					// Get the target path
					using ComPtr<IShellLinkW> pShellLink = default;
					var shellLinkIid = typeof(IShellLinkW).GUID;
					pShellItem.Get()->QueryInterface(&shellLinkIid, (void**)pShellLink.GetAddressOf());
					string targetPath = string.Empty;
					if (pShellLink.IsNull)
					{
						using ComPtr<IShellItem2> pShellItem2 = default;
						var shellItem2Iid = typeof(IShellItem2).GUID;
						pShellItem.Get()->QueryInterface(&shellItem2Iid, (void**)pShellItem2.GetAddressOf());
						PInvoke.PSGetPropertyKeyFromName("System.Link.TargetParsingPath", out var propertyKey);
						pShellItem2.Get()->GetString(propertyKey, out var pszTargetPath);
						targetPath = Environment.ExpandEnvironmentVariables(pszTargetPath.ToString());
					}
					else
					{
						fixed (char* pszTargetPath = new char[1024])
						{
							hr = pShellLink.Get()->GetPath(pszTargetPath, 1024, null, (uint)SLGP_FLAGS.SLGP_RAWPATH);
							targetPath = Environment.ExpandEnvironmentVariables(new PWSTR(pszTargetPath).ToString());
						}
					}

					// Get the display name
					pShellItem.Get()->GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY, out var szDisplayName);
					var fileName = szDisplayName.ToString();
					PInvoke.CoTaskMemFree(szDisplayName.Value);

					// Get the file system path on disk
					pShellItem.Get()->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out szDisplayName);
					var filePath = szDisplayName.ToString();
					PInvoke.CoTaskMemFree(szDisplayName.Value);

					var item = new DriveItem()
					{
						Text = fileName,
						Path = targetPath,
						DeviceID = filePath,
						Type = DriveType.Network,
						ItemType = NavigationControlItemType.Drive,
						MenuOptions = new()
						{
							IsLocationItem = true,
							ShowShellItems = true,
							ShowProperties = true,
						},
					};

					item.MenuOptions.ShowEjectDevice = item.IsRemovable;

					items.Add(item);
				}

				return items;
			}
		}

		/// <inheritdoc/>
		public async Task UpdateComputersAsync()
		{
			var unsortedDrives = new List<ILocatableFolder>()
			{
				_Computers.Single(x => x is DriveItem o && o.DeviceID == "network-folder")
			};

			foreach (ILocatableFolder item in await GetComputersAsync())
				unsortedDrives.Add(item);

			var orderedDrives =
				unsortedDrives.Cast<DriveItem>()
					.OrderByDescending(o => o.DeviceID == "network-folder")
					.ThenBy(o => o.Text);

			Computers.Clear();

			foreach (ILocatableFolder item in orderedDrives)
				Computers.AddIfNotPresent(item);
		}

		/// <inheritdoc/>
		public async Task UpdateShortcutsAsync()
		{
			var unsortedDrives = new List<ILocatableFolder>();

			foreach (ILocatableFolder item in await GetShortcutsAsync())
				unsortedDrives.Add(item);

			var orderedDrives =
				unsortedDrives.Cast<DriveItem>()
					.OrderBy(o => o.Text);

			Shortcuts.Clear();

			foreach (ILocatableFolder item in orderedDrives)
				Shortcuts.AddIfNotPresent(item);
		}

		/// <inheritdoc/>
		public bool DisconnectNetworkDrive(ILocatableFolder drive)
		{
			return
				PInvoke.WNetCancelConnection2W(
					drive.Path.TrimEnd('\\'),
					NET_CONNECT_FLAGS.CONNECT_UPDATE_PROFILE,
					true)
				is WIN32_ERROR.NO_ERROR;
		}

		/// <inheritdoc/>
		public Task OpenMapNetworkDriveDialogAsync()
		{
			return Win32Helper.StartSTATask(() =>
			{
				return CommonDialogService.Open_NetworkConnectionDialog(
					MainWindow.Instance.WindowHandle,
					useMostRecentPath: true,
					hideRestoreConnectionCheckBox: false);
			});
		}

		/// <inheritdoc/>
		public async Task<bool> AuthenticateNetworkShare(string path)
		{
			var netRes = new NETRESOURCEW() { dwType = NET_RESOURCE_TYPE.RESOURCETYPE_DISK };

			unsafe
			{
				fixed (char* lpcPath = path)
					netRes.lpRemoteName = new PWSTR(lpcPath);
			}

			// If credentials are saved, this will return NO_ERROR
			var res = (WIN32_ERROR)PInvoke.WNetAddConnection3W(new(nint.Zero), netRes, null, null, 0);

			if (res == WIN32_ERROR.ERROR_LOGON_FAILURE || res == WIN32_ERROR.ERROR_ACCESS_DENIED)
			{
				var dialog = DynamicDialogFactory.GetFor_CredentialEntryDialog(path);
				await dialog.ShowAsync();
				var credentialsReturned = dialog.ViewModel.AdditionalData as string[];

				if (credentialsReturned is not null && credentialsReturned[1] != null)
				{
					res = (WIN32_ERROR)PInvoke.WNetAddConnection3W(new(nint.Zero), netRes, credentialsReturned[1], credentialsReturned[0], 0);
					if (credentialsReturned[2] == "y" && res == WIN32_ERROR.NO_ERROR)
					{
						var creds = new CREDENTIALW()
						{
							Type = CRED_TYPE.CRED_TYPE_DOMAIN_PASSWORD,
							AttributeCount = 0,
							Persist = CRED_PERSIST.CRED_PERSIST_ENTERPRISE
						};

						unsafe
						{
							fixed (char* lpcTargetName = path.Substring(2))
								creds.TargetName = new(lpcTargetName);

							fixed (char* lpcUserName = credentialsReturned[0])
								creds.UserName = new(lpcUserName);

							byte[] bPassword = Encoding.Unicode.GetBytes(credentialsReturned[1]);
							fixed (byte* lpCredentialBlob = bPassword)
								creds.CredentialBlob = lpCredentialBlob;

							creds.CredentialBlobSize = (uint)bPassword.Length;
						}

						PInvoke.CredWrite(creds, 0);
					}
				}
				else
				{
					return false;
				}
			}

			if (res == WIN32_ERROR.NO_ERROR)
			{
				return true;
			}
			else
			{
				await DialogDisplayHelper.ShowDialogAsync("NetworkFolderErrorDialogTitle".GetLocalizedResource(), res.ToString());

				return false;
			}
		}
	}
}
