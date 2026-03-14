// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Utils;
using System.IO;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.System.WinRT;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using Windows.Win32.UI.Shell.PropertiesSystem;
using static Windows.Win32.ManualMacros;

namespace Files.App.Storage
{
	/// <summary>
	/// Represents a manager for the Files' jump list, allowing synchronization with the Explorer's jump list.
	/// </summary>
	/// <remarks>
	/// See <a href="https://github.com/0x5bfa/JumpListManager/blob/HEAD/JumpListManager/JumpList.cs" />
	/// </remarks>
	public unsafe class JumpListManager : IDisposable
	{
		private string _aumid = null!;
		private string _exeAlias = null!;
		private string _recentCategoryName = null!;

		private FileSystemWatcher? _explorerADLStoreFileWatcher;
		private FileSystemWatcher? _filesADLStoreFileWatcher;

		private readonly Lock _updateJumpListLock = new();

		public static event EventHandler? JumpListChanged;

		private JumpListManager() { }

		public static JumpListManager? Create(string amuid, string exeAlias)
		{
			var categoryName = WindowsStorableHelpers.ResolveIndirectString($"@{{{WindowsStorableHelpers.GetEnvironmentVariable("SystemRoot")}\\SystemResources\\Windows.UI.ShellCommon\\Windows.UI.ShellCommon.pri? ms-resource://Windows.UI.ShellCommon/JumpViewUI/JumpViewCategoryType_Recent}}");
			if (categoryName is null) return null;

			return new JumpListManager()
			{
				_aumid = amuid,
				_exeAlias = exeAlias,
				_recentCategoryName = categoryName,
			};
		}

		public HRESULT PullJumpListFromExplorer(int maxCount = 200)
		{
			try
			{
				// Disable all watchers that could be triggered by this operation
				if (_explorerADLStoreFileWatcher is not null && _explorerADLStoreFileWatcher.EnableRaisingEvents)
					_explorerADLStoreFileWatcher.EnableRaisingEvents = false;
				if (_filesADLStoreFileWatcher is not null && _filesADLStoreFileWatcher.EnableRaisingEvents)
					_filesADLStoreFileWatcher.EnableRaisingEvents = false;

				HRESULT hr;

				using ComPtr<IAutomaticDestinationList2> pExplorerADL = default;
				hr = PInvoke.CoCreateInstance(CLSID.CLSID_AutomaticDestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IAutomaticDestinationList, (void**)pExplorerADL.GetAddressOf());
				fixed (char* pAumid = "Microsoft.Windows.Explorer", pExePath = "C:\\Windows\\explorer.exe") pExplorerADL.Get()->Initialize(pAumid, pExePath, default);

				using ComPtr<IAutomaticDestinationList2> pFilesADL = default;
				hr = PInvoke.CoCreateInstance(CLSID.CLSID_AutomaticDestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IAutomaticDestinationList, (void**)pFilesADL.GetAddressOf());
				fixed (char* pAumid = _aumid) pFilesADL.Get()->Initialize(pAumid, default, default);

				using ComPtr<IInspectable> storageObj = default;
				hr = PInvoke.RoActivateInstance("Windows.Internal.AutomaticDestinationListStorage", storageObj.GetAddressOf());

				// Get whether the Files's Automatic Destination has items
				BOOL hasList = default;
				hr = pFilesADL.Get()->HasList(&hasList);
				if (FAILED(hr)) return hr;

				// Clear the Files' Automatic Destination if any
				if (hasList)
				{
					hr = pFilesADL.Get()->ClearList(true);
					if (FAILED(hr)) return hr;
				}

				using ComPtr<IObjectCollection> poc = default;
				hr = pExplorerADL.Get()->GetList(DESTLISTTYPE.RECENT, maxCount, GETDESTLISTFLAGS.NONE, IID.IID_IObjectCollection, (void**)poc.GetAddressOf());
				if (FAILED(hr)) return hr;

				// Get the count of the Explorer's Recent items
				uint dwItemsCount = 0U;
				hr = poc.Get()->GetCount(&dwItemsCount);
				if (FAILED(hr)) return hr;

				for (uint dwIndex = 0; dwIndex < dwItemsCount; dwIndex++)
				{
					// Get an instance of IShellItem
					using ComPtr<IShellItem> psi = default;
					hr = poc.Get()->GetAt(dwIndex, IID.IID_IShellItem, (void**)psi.GetAddressOf());
					if (FAILED(hr)) continue;

					// Get an instance of IShellLinkW from the IShellItem instance
					using ComPtr<IShellLinkW> psl = default;
					hr = CreateLinkFromItem(psi.Get(), psl.GetAddressOf());
					if (FAILED(hr)) continue;

					hr = pFilesADL.Get()->AddUsagePointsEx((IUnknown*)psl.Get(), true, 0);
					if (FAILED(hr)) continue;

					float accessCount; long lastAccessedTimeUtc;
					hr = pExplorerADL.Get()->GetUsageData((IUnknown*)psi.Get(), &accessCount, &lastAccessedTimeUtc);

					hr = pFilesADL.Get()->SetUsageData((IUnknown*)psl.Get(), &accessCount, &lastAccessedTimeUtc);
					if (FAILED(hr)) continue;

					var IID_IStorageItem = new Guid("4207A996-CA2F-42F7-BDE8-8B10457A7F30");
					void* pStorageItem;
					hr = psi.Get()->BindToHandler(null, BHID.BHID_StorageItem, (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID_IStorageItem)), &pStorageItem);
					if (FAILED(hr)) continue;

					using ComPtr<IAutomaticDestinationListStorage> storage = default;
					hr = storageObj.As(storage.GetAddressOf());
					if (FAILED(hr)) continue;

					var explorerAumid = "Microsoft.Windows.Explorer";
					hr = PInvoke.WindowsCreateString(explorerAumid, (uint)explorerAumid.Length, out var explorerAumidAsHSTRING);
					if (FAILED(hr)) continue;

					hr = storage.Get()->Load(0, HSTRING.Null, (HSTRING)explorerAumidAsHSTRING.DangerousGetHandle(), HSTRING.Null);
					if (FAILED(hr)) continue;

					using ComPtr<IAutomaticDestinationListItemInfo> pInfo = default;
					hr = storage.Get()->GetInfoForItem(pStorageItem, pInfo.GetAddressOf());
					if (FAILED(hr)) continue;

					uint actionCount;
					hr = pInfo.Get()->get_ActionCount(&actionCount);
					if (FAILED(hr)) continue;

					hr = storage.Get()->Close();
					if (FAILED(hr)) continue;

					explorerAumidAsHSTRING.Close();

					using ComPtr<IAutomaticDestinationListPropertyStore> pPropertyStore = default;
					hr = pFilesADL.As(pPropertyStore.GetAddressOf());
					if (FAILED(hr)) continue;

					JumpListItemAccessInfo info = default;
					info.ActionCount = actionCount * 3;

					PROPVARIANT pv;
					hr = PInvoke.InitVariantFromBuffer(&info, (uint)sizeof(JumpListItemAccessInfo), &pv);
					if (FAILED(hr)) continue;

					using ComPtr<IPropertyStore> itemPropertyStore = default;
					//hr = pPropertyStore.Get()->GetPropertyStorageForItem((IUnknown*)psl.Get(), itemPropertyStore.GetAddressOf());
					hr = psl.As(itemPropertyStore.GetAddressOf());
					if (FAILED(hr)) continue;

					PROPERTYKEY PKEY_JumpList_ActionCount = default;
					PKEY_JumpList_ActionCount.fmtid = new Guid("D8E6A5C2-6F47-4D7B-A7D1-5D4F9E3C2101");
					PKEY_JumpList_ActionCount.pid = 2;

					hr = itemPropertyStore.Get()->SetValue(&PKEY_JumpList_ActionCount, &pv);
					if (FAILED(hr)) continue;

					hr = itemPropertyStore.Get()->Commit();
					if (FAILED(hr)) continue;

					int pinIndex = 0;
					hr = pExplorerADL.Get()->GetPinIndex((IUnknown*)psi.Get(), &pinIndex);
					if (FAILED(hr)) continue;

					// Pin it to the Files' Automatic Destinations
					hr = pFilesADL.Get()->PinItem((IUnknown*)psl.Get(), pinIndex);
					if (FAILED(hr)) continue;
				}

				return hr;
			}
			finally
			{
				// Re-enable all watchers
				if (_explorerADLStoreFileWatcher is not null && !_explorerADLStoreFileWatcher.EnableRaisingEvents)
					_explorerADLStoreFileWatcher.EnableRaisingEvents = true;
				if (_filesADLStoreFileWatcher is not null && !_filesADLStoreFileWatcher.EnableRaisingEvents)
					_filesADLStoreFileWatcher.EnableRaisingEvents = true;
			}
		}

		public HRESULT PushJumpListToExplorer(int maxCount = 200)
		{
			try
			{
				// Disable all watchers that could be triggered by this operation
				if (_explorerADLStoreFileWatcher is not null && _explorerADLStoreFileWatcher.EnableRaisingEvents)
					_explorerADLStoreFileWatcher.EnableRaisingEvents = false;
				if (_filesADLStoreFileWatcher is not null && _filesADLStoreFileWatcher.EnableRaisingEvents)
					_filesADLStoreFileWatcher.EnableRaisingEvents = false;

				HRESULT hr;

				using ComPtr<IAutomaticDestinationList2> pExplorerADL = default;
				hr = PInvoke.CoCreateInstance(CLSID.CLSID_AutomaticDestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IAutomaticDestinationList, (void**)pExplorerADL.GetAddressOf());
				fixed (char* pAumid = "Microsoft.Windows.Explorer") pExplorerADL.Get()->Initialize(pAumid, default, default);

				using ComPtr<IAutomaticDestinationList2> pFilesADL = default;
				hr = PInvoke.CoCreateInstance(CLSID.CLSID_AutomaticDestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IAutomaticDestinationList, (void**)pFilesADL.GetAddressOf());
				fixed (char* pAumid = _aumid) pFilesADL.Get()->Initialize(pAumid, default, default);

				using ComPtr<IInspectable> storageObj = default;
				hr = PInvoke.RoActivateInstance("Windows.Internal.AutomaticDestinationListStorage", storageObj.GetAddressOf());

				// Get whether the Explorer's Automatic Destination has items
				BOOL hasList = default;
				hr = pExplorerADL.Get()->HasList(&hasList);
				if (FAILED(hr)) return hr;

				// Clear the Explorer' Automatic Destination if any
				if (hasList)
				{
					hr = pExplorerADL.Get()->ClearList(true);
					if (FAILED(hr)) return hr;
				}

				using ComPtr<IObjectCollection> poc = default;
				hr = pFilesADL.Get()->GetList(DESTLISTTYPE.RECENT, maxCount, GETDESTLISTFLAGS.NONE, IID.IID_IObjectCollection, (void**)poc.GetAddressOf());
				if (FAILED(hr)) return hr;

				// Get the count of the Explorer's Recent items
				uint dwItemsCount = 0U;
				hr = poc.Get()->GetCount(&dwItemsCount);
				if (FAILED(hr)) return hr;

				for (uint dwIndex = 0; dwIndex < dwItemsCount; dwIndex++)
				{
					// Get an instance of IShellItem
					using ComPtr<IShellLinkW> psl = default;
					hr = poc.Get()->GetAt(dwIndex, IID.IID_IShellLinkW, (void**)psl.GetAddressOf());
					if (FAILED(hr)) continue;

					using ComHeapPtr<char> pszParseablePath = default;
					pszParseablePath.Allocate(PInvoke.MAX_PATH);
					hr = psl.Get()->GetArguments(pszParseablePath.Get(), (int)PInvoke.MAX_PATH);
					if (FAILED(hr)) continue;

					using ComHeapPtr<IShellItem> psi = default;
					hr = PInvoke.SHCreateItemFromParsingName(pszParseablePath.Get(), null, IID.IID_IShellItem, (void**)psi.GetAddressOf());
					if (FAILED(hr)) continue;

					hr = pExplorerADL.Get()->AddUsagePointsEx((IUnknown*)psi.Get(), true, 0);
					if (FAILED(hr)) continue;

					float accessCount; long lastAccessedTimeUtc;
					hr = pFilesADL.Get()->GetUsageData((IUnknown*)psl.Get(), &accessCount, &lastAccessedTimeUtc);
					if (FAILED(hr)) continue;

					hr = pExplorerADL.Get()->SetUsageData((IUnknown*)psi.Get(), &accessCount, &lastAccessedTimeUtc);
					if (FAILED(hr)) continue;

					using ComPtr<IAutomaticDestinationListPropertyStore> pPropertyStore = default;
					pFilesADL.As(pPropertyStore.GetAddressOf());

					using ComPtr<IPropertyStore> itemPropertyStore = default;
					//hr = pPropertyStore.Get()->GetPropertyStorageForItem((IUnknown*)psl.Get(), itemPropertyStore.GetAddressOf());
					hr = psl.As(itemPropertyStore.GetAddressOf());
					if (FAILED(hr)) continue;

					PROPERTYKEY PKEY_JumpList_ActionCount = default;
					PKEY_JumpList_ActionCount.fmtid = new Guid("D8E6A5C2-6F47-4D7B-A7D1-5D4F9E3C2101");
					PKEY_JumpList_ActionCount.pid = 2;

					PROPVARIANT pv;
					hr = itemPropertyStore.Get()->GetValue(&PKEY_JumpList_ActionCount, &pv);
					if (FAILED(hr)) continue;

					JumpListItemAccessInfo info;
					hr = PInvoke.PropVariantToBuffer(&pv, &info, (uint)sizeof(JumpListItemAccessInfo));
					if (FAILED(hr)) continue;

					hr = PInvoke.PropVariantClear(&pv);
					if (FAILED(hr)) continue;

					var IID_IStorageItem = new Guid("4207A996-CA2F-42F7-BDE8-8B10457A7F30");
					void* pStorageItem;
					hr = psi.Get()->BindToHandler(null, BHID.BHID_StorageItem, (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID_IStorageItem)), &pStorageItem);
					if (FAILED(hr)) continue;

					using ComPtr<IAutomaticDestinationListStorage> storage = default;
					hr = storageObj.As(storage.GetAddressOf());
					if (FAILED(hr)) continue;

					var explorerAumid = "Microsoft.Windows.Explorer";
					hr = PInvoke.WindowsCreateString(explorerAumid, (uint)explorerAumid.Length, out var explorerAumidAsHSTRING);
					if (FAILED(hr)) continue;

					hr = storage.Get()->Load(1, HSTRING.Null, (HSTRING)explorerAumidAsHSTRING.DangerousGetHandle(), HSTRING.Null);
					if (FAILED(hr)) continue;

					using ComPtr<IAutomaticDestinationListItemInfo> pInfo = default;
					hr = storage.Get()->GetInfoForItem(pStorageItem, pInfo.GetAddressOf());
					if (FAILED(hr)) continue;

					hr = pInfo.Get()->put_ActionCount(info.ActionCount);
					if (FAILED(hr)) continue;

					explorerAumidAsHSTRING.Close();

					hr = storage.Get()->Save();
					if (FAILED(hr)) continue;

					hr = storage.Get()->Close();
					if (FAILED(hr)) continue;

					int pinIndex = 0;
					hr = pFilesADL.Get()->GetPinIndex((IUnknown*)psl.Get(), &pinIndex);
					if (FAILED(hr)) continue;

					// Pin it to the Files' Automatic Destinations
					hr = pExplorerADL.Get()->PinItem((IUnknown*)psi.Get(), pinIndex);
					if (FAILED(hr)) continue;
				}

				return hr;
			}
			finally
			{
				// Re-enable all watchers
				if (_explorerADLStoreFileWatcher is not null && !_explorerADLStoreFileWatcher.EnableRaisingEvents)
					_explorerADLStoreFileWatcher.EnableRaisingEvents = true;
				if (_filesADLStoreFileWatcher is not null && !_filesADLStoreFileWatcher.EnableRaisingEvents)
					_filesADLStoreFileWatcher.EnableRaisingEvents = true;
			}
		}

		public HRESULT AddFolderToRecentCategory(string path) // TODO: This will be WindowsFolder in the future
		{
			HRESULT hr;

			using ComHeapPtr<IShellItem> psi = default;
			fixed (char* pszPath = path)
				hr = PInvoke.SHCreateItemFromParsingName(pszPath, null, IID.IID_IShellItem, (void**)psi.GetAddressOf());
			if (FAILED(hr)) return hr;

			fixed (char* pAumid = "Microsoft.Windows.Explorer")
			{
				SHARDAPPIDINFO info = default;
				info.psi = psi.Get();
				info.pszAppID = pAumid;

				// This will update Files' jump list as well because of the file watcher
				PInvoke.SHAddToRecentDocs((uint)SHARD.SHARD_APPIDINFO, &info);
			}

			return HRESULT.S_OK;
		}

		public bool WatchJumpListChanges(string aumidCrcHash)
		{
			_explorerADLStoreFileWatcher?.Dispose();
			_explorerADLStoreFileWatcher = new()
			{
				Path = $"{WindowsStorableHelpers.GetRecentFolderPath()}\\AutomaticDestinations",
				Filter = "f01b4d95cf55d32a.automaticDestinations-ms", // Microsoft.Windows.Explorer
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
			};

			_filesADLStoreFileWatcher?.Dispose();
			_filesADLStoreFileWatcher = new()
			{
				Path = $"{WindowsStorableHelpers.GetRecentFolderPath()}\\AutomaticDestinations",
				Filter = $"{aumidCrcHash}.automaticDestinations-ms",
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
			};

			_explorerADLStoreFileWatcher.Changed += ExplorerJumpListWatcher_Changed;
			_filesADLStoreFileWatcher.Changed += FilesJumpListWatcher_Changed;

			try
			{
				// NOTE: This may throw various exceptions (e.g., when the file doesn't exist or cannot be accessed)
				_explorerADLStoreFileWatcher.EnableRaisingEvents = true;
				_filesADLStoreFileWatcher.EnableRaisingEvents = true;
			}
			catch
			{
				if (_explorerADLStoreFileWatcher is not null)
				{
					_explorerADLStoreFileWatcher.EnableRaisingEvents = false;
					_explorerADLStoreFileWatcher.Changed -= ExplorerJumpListWatcher_Changed;
					_explorerADLStoreFileWatcher.Dispose();
				}

				if (_filesADLStoreFileWatcher is not null)
				{
					_filesADLStoreFileWatcher.EnableRaisingEvents = false;
					_filesADLStoreFileWatcher.Changed -= FilesJumpListWatcher_Changed;
					_filesADLStoreFileWatcher.Dispose();
				}

				return false;
			}

			return true;
		}

		private HRESULT CreateLinkFromItem(IShellItem* psi, IShellLinkW** ppsl)
		{
			// Instantiate a default instance of IShellLinkW
			using ComPtr<IShellLinkW> psl = default;
			HRESULT hr = PInvoke.CoCreateInstance(CLSID.CLSID_ShellLink, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IShellLinkW, (void**)psl.GetAddressOf());
			if (FAILED(hr)) return hr;

			// Set the Files package path in the shell namespace
			fixed (char* pszFilesEntryPointPath = _exeAlias)
				hr = psl.Get()->SetPath(pszFilesEntryPointPath);
			if (FAILED(hr)) return hr;

			// Get the full path of the folder
			using ComHeapPtr<char> pszParseablePath = default;
			hr = psi->GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING, (PWSTR*)pszParseablePath.GetAddressOf());
			if (FAILED(hr)) return hr;

			// Set it as the argument of the link
			hr = psl.Get()->SetArguments(pszParseablePath.Get());
			if (FAILED(hr)) return hr;

			// Get the icon location of the folder
			using ComHeapPtr<char> pszIconLocation = default;
			pszIconLocation.Allocate(PInvoke.MAX_PATH); int index = 0;
			hr = GetFolderIconLocation(psi, (PWSTR*)pszIconLocation.GetAddressOf(), PInvoke.MAX_PATH, &index);
			if (FAILED(hr)) return hr;

			// Set it as the icon location of the link
			hr = psl.Get()->SetIconLocation(pszIconLocation.Get(), index);
			if (FAILED(hr)) return hr;

			// Get the display name of the folder
			using ComHeapPtr<char> pszDisplayName = default;
			hr = psi->GetDisplayName(SIGDN.SIGDN_PARENTRELATIVEFORUI, (PWSTR*)pszDisplayName.GetAddressOf());
			if (FAILED(hr)) return hr;

			// Set it as the tooltip of the link
			fixed (char* pszTooltip = $"{new(pszDisplayName.Get())} ({new(pszParseablePath.Get())})")
				hr = psl.Get()->SetDescription(pszTooltip);
			if (FAILED(hr)) return hr;

			// Query an instance of IPropertyStore
			using ComPtr<IPropertyStore> pps = default;
			hr = psl.Get()->QueryInterface(IID.IID_IPropertyStore, (void**)pps.GetAddressOf());
			if (FAILED(hr)) return hr;

			PROPVARIANT PVAR_Title;
			PROPERTYKEY PKEY_Title = PInvoke.PKEY_Title;

			hr = PInvoke.InitPropVariantFromString(pszDisplayName.Get(), &PVAR_Title);
			if (FAILED(hr)) return hr;

			hr = pps.Get()->SetValue(&PKEY_Title, &PVAR_Title);
			if (FAILED(hr)) return hr;

			hr = pps.Get()->Commit();
		 if (FAILED(hr)) return hr;

			hr = PInvoke.PropVariantClear(&PVAR_Title);
			if (FAILED(hr)) return hr;

			psl.Get()->AddRef();
			*ppsl = psl.Get();

			return hr;
		}

		private HRESULT GetFolderIconLocation(IShellItem* psi, PWSTR* pIconFilePath, uint cch, int* pIndex)
		{
			HRESULT hr = default;

			using ComPtr<IExtractIconW> pei = default;
			hr = psi->BindToHandler(null, BHID.BHID_SFUIObject, IID.IID_IExtractIconW, (void**)pei.GetAddressOf());
			if (FAILED(hr)) return hr;

			uint flags;
			hr = pei.Get()->GetIconLocation(PInvoke.GIL_FORSHELL, *pIconFilePath, cch, pIndex, &flags);
			if (FAILED(hr)) return hr;

			return hr;
		}

		private void ExplorerJumpListWatcher_Changed(object sender, FileSystemEventArgs e)
		{
			_ = STATask.Run(() =>
			{
				if (_updateJumpListLock.TryEnter())
				{
					try
					{
						Debug.WriteLine("in: ExplorerJumpListWatcher_Changed");
						PullJumpListFromExplorer();
						JumpListChanged?.Invoke(this, EventArgs.Empty);
						Debug.WriteLine("out: ExplorerJumpListWatcher_Changed");
					}
					finally
					{
						_updateJumpListLock.Exit();
					}
				}
			},
			null);
		}

		private void FilesJumpListWatcher_Changed(object sender, FileSystemEventArgs e)
		{
			_ = STATask.Run(() =>
			{
				if (_updateJumpListLock.TryEnter())
				{
					try
					{
						Debug.WriteLine("in: FilesJumpListWatcher_Changed");
						PushJumpListToExplorer();
						JumpListChanged?.Invoke(this, EventArgs.Empty);
						Debug.WriteLine("out: FilesJumpListWatcher_Changed");
					}
					finally
					{
						_updateJumpListLock.Exit();
					}
				}
			},
			null);
		}

		public void Dispose()
		{
			if (_explorerADLStoreFileWatcher is not null)
			{
				_explorerADLStoreFileWatcher.EnableRaisingEvents = false;
				_explorerADLStoreFileWatcher.Changed -= ExplorerJumpListWatcher_Changed;
				_explorerADLStoreFileWatcher.Dispose();
			}

			if (_filesADLStoreFileWatcher is not null)
			{
				_filesADLStoreFileWatcher.EnableRaisingEvents = false;
				_filesADLStoreFileWatcher.Changed -= FilesJumpListWatcher_Changed;
				_filesADLStoreFileWatcher.Dispose();
			}
		}
	}

	internal struct JumpListItemAccessInfo
	{
		internal uint ActionCount;
	}
}
