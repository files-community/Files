// Copyright (c) Files Community
// Licensed under the MIT License.

using OwlCore.Storage;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;
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
		private static readonly Lazy<JumpListManager?> _default = new(Create, LazyThreadSafetyMode.ExecutionAndPublication);
		public static JumpListManager? Default => _default.Value;

		private string _filesAUMID = null!;

		private string _localizedRecentCategoryName = null!;

		private FileSystemWatcher? _explorerJumpListWatcher;
		private FileSystemWatcher? _filesJumpListWatcher;

		private IAutomaticDestinationList* _explorerADL;
		private IAutomaticDestinationList* _filesADL;
		private ICustomDestinationList* _filesCDL;
		private IInternalCustomDestinationList* _filesICDL;

		private JumpListManager() { }

		public HRESULT PullJumpListFromExplorer(int maxCount = 40)
		{
			if (_filesJumpListWatcher is not null && _filesJumpListWatcher.EnableRaisingEvents)
				_filesJumpListWatcher.EnableRaisingEvents = false;

			HRESULT hr;

			try
			{
				hr = SyncFilesJumpListWithExplorer(maxCount);
				if (FAILED(hr)) return hr;
			}
			finally
			{
				if (_filesJumpListWatcher is not null && !_filesJumpListWatcher.EnableRaisingEvents)
					_filesJumpListWatcher.EnableRaisingEvents = true;
			}

			return hr;
		}

		public HRESULT PushJumpListToExplorer(int maxCount = 40)
		{
			if (_explorerJumpListWatcher is not null && _explorerJumpListWatcher.EnableRaisingEvents)
				_explorerJumpListWatcher.EnableRaisingEvents = false;

			HRESULT hr;

			try
			{
				hr = SyncExplorerJumpListWithFiles(maxCount);
				if (FAILED(hr)) return hr;
			}
			finally
			{
				if (_explorerJumpListWatcher is not null && !_explorerJumpListWatcher.EnableRaisingEvents)
					_explorerJumpListWatcher.EnableRaisingEvents = true;
			}

			return hr;
		}

		public bool WatchJumpListChanges(string aumidCrcHash)
		{
			_explorerJumpListWatcher?.Dispose();
			_explorerJumpListWatcher = new()
			{
				Path = $"{GetRecentFolderPath()}\\AutomaticDestinations",
				Filter = "f01b4d95cf55d32a.automaticDestinations-ms", // Microsoft.Windows.Explorer
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
			};

			_filesJumpListWatcher?.Dispose();
			_filesJumpListWatcher = new()
			{
				Path = $"{GetRecentFolderPath()}\\AutomaticDestinations",
				Filter = $"{aumidCrcHash}.automaticDestinations-ms",
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
			};

			_explorerJumpListWatcher.Changed += ExplorerJumpListWatcher_Changed;
			_filesJumpListWatcher.Changed += FilesJumpListWatcher_Changed;

			try
			{
				// NOTE: This may throw various exceptions (e.g., when the file doesn't exist or cannot be accessed)
				_explorerJumpListWatcher.EnableRaisingEvents = true;
				_filesJumpListWatcher.EnableRaisingEvents = true;
			}
			catch
			{
				// Gracefully exit if we can't monitor the file
				return false;
			}

			return true;
		}

		public string GetRecentFolderPath()
		{
			using ComHeapPtr<char> pwszRecentFolderPath = default;
			PInvoke.SHGetKnownFolderPath(FOLDERID.FOLDERID_Recent, KNOWN_FOLDER_FLAG.KF_FLAG_DONT_VERIFY | KNOWN_FOLDER_FLAG.KF_FLAG_NO_ALIAS, HANDLE.Null, (PWSTR*)pwszRecentFolderPath.GetAddressOf());
			return new(pwszRecentFolderPath.Get());
		}

		private static JumpListManager? Create()
		{
			HRESULT hr = default;

			void* pv = default;

			var instance = new JumpListManager()
			{
				_filesAUMID = $"{Package.Current.Id.FamilyName}!App",
				_localizedRecentCategoryName = $"@{{{Environment.GetEnvironmentVariable("SystemRoot")}\\SystemResources\\Windows.UI.ShellCommon\\Windows.UI.ShellCommon.pri? ms-resource://Windows.UI.ShellCommon/JumpViewUI/JumpViewCategoryType_Recent}}",
			};

			hr = PInvoke.CoCreateInstance(CLSID.CLSID_AutomaticDestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IAutomaticDestinationList, &pv);
			if (FAILED(hr)) return null;
			instance._explorerADL = (IAutomaticDestinationList*)pv;
			instance._explorerADL->Initialize((PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in "Microsoft.Windows.Explorer".GetPinnableReference())), default, default);

			hr = PInvoke.CoCreateInstance(CLSID.CLSID_AutomaticDestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IAutomaticDestinationList, &pv);
			if (FAILED(hr)) return null;
			instance._filesADL = (IAutomaticDestinationList*)pv;
			instance._filesADL->Initialize((PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in instance._filesAUMID.GetPinnableReference())), default, default);

			hr = PInvoke.CoCreateInstance(CLSID.CLSID_DestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_ICustomDestinationList, &pv);
			if (FAILED(hr)) return null;
			instance._filesCDL = (ICustomDestinationList*)pv;
			instance._filesCDL->SetAppID((PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in instance._filesAUMID.GetPinnableReference())));

			hr = PInvoke.CoCreateInstance(CLSID.CLSID_DestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IInternalCustomDestinationList, &pv);
			if (FAILED(hr)) return null;
			instance._filesICDL = (IInternalCustomDestinationList*)pv;
			instance._filesICDL->SetApplicationID((PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in instance._filesAUMID.GetPinnableReference())));

			return instance;
		}

		private HRESULT SyncFilesJumpListWithExplorer(int maxItemsToSync)
		{
			HRESULT hr = default;

			BOOL hasList = default;
			hr = _filesADL->HasList(&hasList);
			if (FAILED(hr)) return hr;

			if (hasList)
			{
				hr = _filesADL->ClearList(true);
				if (FAILED(hr)) return hr;
			}

			hr = _filesCDL->DeleteList((PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in _filesAUMID.GetPinnableReference())));

			using ComPtr<IObjectCollection> poc = default;
			hr = _explorerADL->GetList(DESTLISTTYPE.RECENT, maxItemsToSync, GETDESTLISTFLAGS.NONE, IID.IID_IObjectCollection, (void**)poc.GetAddressOf());
			if (FAILED(hr)) return hr;

			uint dwItemsCount = 0U;
			hr = poc.Get()->GetCount(&dwItemsCount);
			if (FAILED(hr)) return hr;

			using ComPtr<IObjectCollection> pNewObjectCollection = default;
			hr = PInvoke.CoCreateInstance(CLSID.CLSID_EnumerableObjectCollection, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IObjectCollection, (void**)pNewObjectCollection.GetAddressOf());

			for (uint dwIndex = 0; dwIndex < dwItemsCount; dwIndex++)
			{
				using ComPtr<IShellItem> psi = default;
				hr = poc.Get()->GetAt(dwIndex, IID.IID_IShellItem, (void**)psi.GetAddressOf());
				if (FAILED(hr)) continue;

				IShellLinkW* psl = default;
				hr = CreateLinkFromItem(psi.Get(), &psl);
				if (FAILED(hr)) continue;

				hr = pNewObjectCollection.Get()->AddObject((IUnknown*)psl);
				if (FAILED(hr)) continue;

				int pinIndex = 0;
				hr = _explorerADL->GetPinIndex((IUnknown*)psi.Get(), &pinIndex);
				if (FAILED(hr)) continue; // If not pinned, HRESULT is E_NOT_SET

				hr = _filesADL->PinItem((IUnknown*)psl, pinIndex);
				if (FAILED(hr)) continue;
			}

			using ComPtr<IObjectArray> pNewObjectArray = default;
			hr = pNewObjectCollection.Get()->QueryInterface(IID.IID_IObjectArray, (void**)pNewObjectArray.GetAddressOf());
			if (FAILED(hr)) return hr;

			PWSTR pOutBuffer = (PWSTR)NativeMemory.Alloc(256 * sizeof(char));
			hr = PInvoke.SHLoadIndirectString(
				(PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in _localizedRecentCategoryName.GetPinnableReference())),
				pOutBuffer, 256U);
			if (FAILED(hr)) return hr;

			uint cMinSlots;
			using ComPtr<IObjectArray> pRemovedObjectArray = default;
			hr = _filesCDL->BeginList(&cMinSlots, IID.IID_IObjectArray, (void**)pRemovedObjectArray.GetAddressOf());
			if (FAILED(hr)) return hr;

			hr = _filesCDL->AppendCategory(pOutBuffer, pNewObjectArray.Get());
			if (FAILED(hr)) return hr;

			hr = _filesCDL->CommitList();
			if (FAILED(hr)) return hr;

			NativeMemory.Free(pOutBuffer);

			return hr;
		}

		private HRESULT SyncExplorerJumpListWithFiles(int maxItemsToSync)
		{
			HRESULT hr = default;

			BOOL hasList = default;
			hr = _explorerADL->HasList(&hasList);
			if (FAILED(hr)) return hr;

			if (hasList)
			{
				hr = _explorerADL->ClearList(true);
				if (FAILED(hr)) return hr;
			}

			uint count = 0U;
			_filesICDL->GetCategoryCount(&count);

			uint dwDestinationsIndex = 0U;
			for (uint dwIndex = 0U; dwIndex < count; dwIndex++)
			{
				APPDESTCATEGORY category = default;

				try
				{
					hr = _filesICDL->GetCategory(dwIndex, GETCATFLAG.DEFAULT, &category);
					if (FAILED(hr) ||
						category.Type is not APPDESTCATEGORYTYPE.CUSTOM ||
						!_localizedRecentCategoryName.Equals(new(category.Anonymous.Name), StringComparison.OrdinalIgnoreCase))
						continue;

					dwDestinationsIndex = dwIndex;
				}
				finally

				{
					// The memory layout at Name can be either PWSTR or int depending on the category type
					if (category.Anonymous.Name.Value is not null && category.Type is APPDESTCATEGORYTYPE.CUSTOM)
						PInvoke.CoTaskMemFree(category.Anonymous.Name);
				}
			}

			using ComPtr<IObjectCollection> pDestinationsObjectCollection = default;
			hr = _filesICDL->EnumerateCategoryDestinations(dwDestinationsIndex, IID.IID_IObjectCollection, (void**)pDestinationsObjectCollection.GetAddressOf());

			uint dwItems = 0U;
			hr = pDestinationsObjectCollection.Get()->GetCount(&dwItems);
			if (FAILED(hr)) return hr;

			for (uint dwIndex = 0U; dwIndex < dwItems && dwIndex < maxItemsToSync; dwIndex++)
			{
				using ComPtr<IShellLinkW> psl = default;
				hr = pDestinationsObjectCollection.Get()->GetAt(dwIndex, IID.IID_IShellLinkW, (void**)psl.GetAddressOf());
				if (FAILED(hr)) continue;

				using ComHeapPtr<ITEMIDLIST> pidl = default;
				hr = psl.Get()->GetIDList(pidl.GetAddressOf());

				using ComHeapPtr<IShellItem> psi = default;
				hr = PInvoke.SHCreateItemFromIDList(pidl.Get(), IID.IID_IShellItem, (void**)psi.GetAddressOf());
				if (FAILED(hr)) continue;

				hr = _explorerADL->AddUsagePoint((IUnknown*)psi.Get());
				if (FAILED(hr)) continue;

				int pinIndex = 0;
				hr = _explorerADL->GetPinIndex((IUnknown*)psl.Get(), &pinIndex);
				if (FAILED(hr)) continue; // If not pinned, HRESULT is E_NOT_SET

				hr = _filesADL->PinItem((IUnknown*)psi.Get(), -1);
				if (FAILED(hr)) continue;
			}

			return hr;
		}

		private HRESULT CreateLinkFromItem(IShellItem* psi, IShellLinkW** ppsl)
		{
			using ComPtr<IShellLinkW> psl = default;
			HRESULT hr = PInvoke.CoCreateInstance(CLSID.CLSID_ShellLink, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IShellLinkW, (void**)psl.GetAddressOf());
			if (FAILED(hr)) return hr;

			using ComHeapPtr<ITEMIDLIST> pidl = default;
			hr = PInvoke.SHGetIDListFromObject((IUnknown*)psi, pidl.GetAddressOf());
			if (FAILED(hr)) return hr;

			using ComHeapPtr<char> pParseablePath = default;
			hr = psi->GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING, (PWSTR*)pParseablePath.GetAddressOf());
			if (FAILED(hr)) return hr;

			hr = psl.Get()->SetArguments(new(pParseablePath.Get()));
			if (FAILED(hr)) return hr;

			using ComHeapPtr<char> pDisplayName = default;
			hr = psi->GetDisplayName(SIGDN.SIGDN_PARENTRELATIVEFORUI, (PWSTR*)pDisplayName.GetAddressOf());
			if (FAILED(hr)) return hr;

			PWSTR pIconLocation = (PWSTR)PInvoke.CoTaskMemAlloc(PInvoke.MAX_PATH);
			int index = 0;
			hr = GetFolderIconLocation(psi, &pIconLocation, PInvoke.MAX_PATH, &index);
			if (FAILED(hr)) return hr;

			var iconLocation = new string(pIconLocation);
			hr = psl.Get()->SetIconLocation(pIconLocation, index);
			if (FAILED(hr)) return hr;

			PInvoke.CoTaskMemFree(pIconLocation);

			using ComPtr<IPropertyStore> pps = default;
			hr = psl.Get()->QueryInterface(IID.IID_IPropertyStore, (void**)pps.GetAddressOf());
			if (FAILED(hr)) return hr;

			PROPVARIANT propvar;
			PROPERTYKEY PKEY_Title = PInvoke.PKEY_Title;

			hr = PInvoke.InitPropVariantFromString(pDisplayName.Get(), &propvar);
			if (FAILED(hr)) return hr;

			hr = pps.Get()->SetValue(&PKEY_Title, &propvar);
			if (FAILED(hr)) return hr;

			hr = pps.Get()->Commit();
			if (FAILED(hr)) return hr;

			hr = PInvoke.PropVariantClear(&propvar);
			if (FAILED(hr)) return hr;

			hr = psl.Get()->QueryInterface(IID.IID_IShellLinkW, (void**)ppsl);
			if (FAILED(hr)) return hr;

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
			PullJumpListFromExplorer();
		}

		private void FilesJumpListWatcher_Changed(object sender, FileSystemEventArgs e)
		{
			PushJumpListToExplorer();
		}

		public void Dispose()
		{
			if (_filesJumpListWatcher is not null)
			{
				_filesJumpListWatcher.EnableRaisingEvents = false;
				_filesJumpListWatcher.Dispose();
			}

			if (_explorerJumpListWatcher is not null)
			{
				_explorerJumpListWatcher.EnableRaisingEvents = false;
				_explorerJumpListWatcher.Dispose();
			}

			if (_explorerADL is not null) ((IUnknown*)_explorerADL)->Release();
			if (_filesADL is not null) ((IUnknown*)_filesADL)->Release();
			if (_filesCDL is not null) ((IUnknown*)_filesCDL)->Release();
		}
	}
}
