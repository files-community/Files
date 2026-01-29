// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
		private string _aumid = null!;
		private string _exeAlias = null!;
		private string _recentCategoryName = null!;

		private FileSystemWatcher? _explorerADLStoreFileWatcher;
		private FileSystemWatcher? _filesADLStoreFileWatcher;
		private FileSystemWatcher? _filesCDLStoreFileWatcher;

		public event EventHandler? ExplorerJumpListChanged;
		public event EventHandler? FilesJumpListChanged;

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
			// This method changes the jump list of Files, so we disable the watcher temporarily
			if (_filesADLStoreFileWatcher is not null && _filesADLStoreFileWatcher.EnableRaisingEvents)
				_filesADLStoreFileWatcher.EnableRaisingEvents = false;
			if (_filesCDLStoreFileWatcher is not null && _filesCDLStoreFileWatcher.EnableRaisingEvents)
				_filesCDLStoreFileWatcher.EnableRaisingEvents = false;

			HRESULT hr;

			using ComPtr<IAutomaticDestinationList> pExplorerADL = default;
			hr = PInvoke.CoCreateInstance(CLSID.CLSID_AutomaticDestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IAutomaticDestinationList, (void**)pExplorerADL.GetAddressOf());
			fixed (char* pAumid = "Microsoft.Windows.Explorer") pExplorerADL.Get()->Initialize(pAumid, default, default);

			using ComPtr<IAutomaticDestinationList> pFilesADL = default;
			hr = PInvoke.CoCreateInstance(CLSID.CLSID_AutomaticDestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IAutomaticDestinationList, (void**)pFilesADL.GetAddressOf());
			fixed (char* pAumid = _aumid) pFilesADL.Get()->Initialize(pAumid, default, default);

			using ComPtr<ICustomDestinationList> pFilesCDL = default;
			hr = PInvoke.CoCreateInstance(CLSID.CLSID_DestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_ICustomDestinationList, (void**)pFilesCDL.GetAddressOf());
			fixed (char* pAumid = _aumid) pFilesCDL.Get()->SetAppID(pAumid);

			try
			{
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

				// Clear the Files' Custom Destination
				hr = pFilesCDL.Get()->DeleteList((PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in _aumid.GetPinnableReference())));

				// Get the Explorer's Pinned items from its Automatic Destination
				using ComPtr<IObjectCollection> poc = default;
				hr = pExplorerADL.Get()->GetList(DESTLISTTYPE.PINNED, maxCount, GETDESTLISTFLAGS.NONE, IID.IID_IObjectCollection, (void**)poc.GetAddressOf());
				if (FAILED(hr)) return hr;

				// Get the count of the Explorer's Pinned items
				uint dwItemsCount = 0U;
				hr = poc.Get()->GetCount(&dwItemsCount);
				if (FAILED(hr)) return hr;

				for (uint dwIndex = 0; dwIndex < dwItemsCount; dwIndex++)
				{
					// Get an instance of IShellItem
					using ComPtr<IShellItem> psi = default;
					hr = poc.Get()->GetAt(dwIndex, IID.IID_IShellItem, (void**)psi.GetAddressOf());
					if (FAILED(hr)) continue;

					// Get its pin index
					int pinIndex = 0;
					hr = pExplorerADL.Get()->GetPinIndex((IUnknown*)psi.Get(), &pinIndex);
					if (FAILED(hr)) continue;

					// Get an instance of IShellLinkW from the IShellItem instance
					IShellLinkW* psl = default;
					hr = CreateLinkFromItem(psi.Get(), &psl);
					if (FAILED(hr)) continue;

					// Pin it to the Files' Automatic Destinations
					hr = pFilesADL.Get()->PinItem((IUnknown*)psl, pinIndex);
					if (FAILED(hr)) continue;
				}

				// Get the Explorer's Recent items from its Automatic Destination
				poc.Dispose();
				hr = pExplorerADL.Get()->GetList(DESTLISTTYPE.RECENT, maxCount, GETDESTLISTFLAGS.NONE, IID.IID_IObjectCollection, (void**)poc.GetAddressOf());
				if (FAILED(hr)) return hr;

				// Get the count of the Explorer's Recent items
				hr = poc.Get()->GetCount(&dwItemsCount);
				if (FAILED(hr)) return hr;

				// Instantiate an instance of IObjectCollection
				using ComPtr<IObjectCollection> pNewObjectCollection = default;
				hr = PInvoke.CoCreateInstance(CLSID.CLSID_EnumerableObjectCollection, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IObjectCollection, (void**)pNewObjectCollection.GetAddressOf());
				if (FAILED(hr)) return hr;

				for (uint dwIndex = 0; dwIndex < dwItemsCount; dwIndex++)
				{
					// Get an instance of IShellItem
					using ComPtr<IShellItem> psi = default;
					hr = poc.Get()->GetAt(dwIndex, IID.IID_IShellItem, (void**)psi.GetAddressOf());
					if (FAILED(hr)) continue;

					// Try to get the pin index of the item. If it is not pinned, keep going
					int pinIndex = 0;
					hr = pExplorerADL.Get()->GetPinIndex((IUnknown*)psi.Get(), &pinIndex);
					if (SUCCEEDED(hr)) continue; // If not pinned, HRESULT is E_NOT_SET

					// Get an instance of IShellLinkW from the IShellItem instance
					IShellLinkW* psl = default;
					hr = CreateLinkFromItem(psi.Get(), &psl);
					if (FAILED(hr)) continue;

					// Add it to the Files' Custom Destinations
					hr = pNewObjectCollection.Get()->AddObject((IUnknown*)psl);
					if (FAILED(hr)) continue;
				}

				// Get IObjectArray from IObjectCollection
				using ComPtr<IObjectArray> pNewObjectArray = default;
				hr = pNewObjectCollection.Get()->QueryInterface(IID.IID_IObjectArray, (void**)pNewObjectArray.GetAddressOf());
				if (FAILED(hr)) return hr;

				// Set the collection
				uint cMinSlots;
				using ComPtr<IObjectArray> pRemovedObjectArray = default;
				hr = pFilesCDL.Get()->BeginList(&cMinSlots, IID.IID_IObjectArray, (void**)pRemovedObjectArray.GetAddressOf());
				if (FAILED(hr)) return hr;

				hr = pRemovedObjectArray.Get()->GetCount(out var count);
				if (FAILED(hr)) return hr;

				// Append "Recent" category
				hr = pFilesCDL.Get()->AppendCategory((PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in "Recent".GetPinnableReference())), pNewObjectArray.Get());
				if (FAILED(hr)) return hr;

				// Commit the collection updates
				hr = pFilesCDL.Get()->CommitList();
				if (FAILED(hr)) return hr;

				return hr;
			}
			finally
			{
				if (_filesADLStoreFileWatcher is not null && !_filesADLStoreFileWatcher.EnableRaisingEvents)
					_filesADLStoreFileWatcher.EnableRaisingEvents = true;
				if (_filesCDLStoreFileWatcher is not null && !_filesCDLStoreFileWatcher.EnableRaisingEvents)
					_filesCDLStoreFileWatcher.EnableRaisingEvents = true;
			}
		}

		public HRESULT PushJumpListToExplorer(int maxCount = 200)
		{
			if (_explorerADLStoreFileWatcher is not null && _explorerADLStoreFileWatcher.EnableRaisingEvents)
				_explorerADLStoreFileWatcher.EnableRaisingEvents = false;

			HRESULT hr;

			using ComPtr<IAutomaticDestinationList> pExplorerADL = default;
			hr = PInvoke.CoCreateInstance(CLSID.CLSID_AutomaticDestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IAutomaticDestinationList, (void**)pExplorerADL.GetAddressOf());
			fixed (char* pAumid = "Microsoft.Windows.Explorer") pExplorerADL.Get()->Initialize(pAumid, default, default);

			using ComPtr<IAutomaticDestinationList> pFilesADL = default;
			hr = PInvoke.CoCreateInstance(CLSID.CLSID_AutomaticDestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IAutomaticDestinationList, (void**)pFilesADL.GetAddressOf());
			fixed (char* pAumid = _aumid) pFilesADL.Get()->Initialize(pAumid, default, default);

			using ComPtr<IInternalCustomDestinationList> pFilesICDL = default;
			hr = PInvoke.CoCreateInstance(CLSID.CLSID_DestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IInternalCustomDestinationList, (void**)pFilesICDL.GetAddressOf());
			fixed (char* pAumid = _aumid) pFilesICDL.Get()->SetApplicationID(pAumid);

			try
			{
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

				// Get the count of categories in the Files' Custom Destinations
				uint count = 0U;
				pFilesICDL.Get()->GetCategoryCount(&count);

				// Find the "Recent" category index
				uint indexOfRecentCategory = 0U;
				for (uint index = 0U; index < count; index++)
				{
					APPDESTCATEGORY category = default;

					try
					{
						hr = pFilesICDL.Get()->GetCategory(index, GETCATFLAG.DEFAULT, &category);
						if (FAILED(hr) ||
							category.Type is not APPDESTCATEGORYTYPE.CUSTOM ||
							!_recentCategoryName.Equals(new(category.Anonymous.Name), StringComparison.OrdinalIgnoreCase))
							continue;

						indexOfRecentCategory = index;
						break;
					}
					finally
					{
						// The memory layout at Name can be either PWSTR or int depending on the category type
						if (category.Anonymous.Name.Value is not null && category.Type is APPDESTCATEGORYTYPE.CUSTOM)
							PInvoke.CoTaskMemFree(category.Anonymous.Name);
					}
				}

				// Get the items in the "Recent" category
				using ComPtr<IObjectCollection> poc = default;
				hr = pFilesICDL.Get()->EnumerateCategoryDestinations(indexOfRecentCategory, IID.IID_IObjectCollection, (void**)poc.GetAddressOf());

				// Get the count of items in the "Recent" category
				uint countOfItems = 0U;
				hr = poc.Get()->GetCount(&countOfItems);
				if (FAILED(hr)) return hr;

				// Copy them to the Explorer's Automatic Destination
				for (int index = (int)countOfItems - 1; index >= 0; index--)
				{
					using ComPtr<IShellLinkW> psl = default;
					hr = poc.Get()->GetAt((uint)index, IID.IID_IShellLinkW, (void**)psl.GetAddressOf());
					if (FAILED(hr)) continue;

					int pinIndex;
					hr = pFilesADL.Get()->GetPinIndex((IUnknown*)psl.Get(), &pinIndex);
					if (SUCCEEDED(hr)) continue; // If pinned, HRESULT is S_OK

					using ComHeapPtr<char> pszParseablePath = default;
					pszParseablePath.Allocate(PInvoke.MAX_PATH);
					hr = psl.Get()->GetArguments(pszParseablePath.Get(), (int)PInvoke.MAX_PATH);

					using ComHeapPtr<IShellItem> psi = default;
					hr = PInvoke.SHCreateItemFromParsingName(pszParseablePath.Get(), null, IID.IID_IShellItem, (void**)psi.GetAddressOf());
					if (FAILED(hr)) continue;

					fixed (char* pAumid = "Microsoft.Windows.Explorer")
					{
						SHARDAPPIDINFO info = default;
						info.psi = psi.Get();
						info.pszAppID = pAumid;

						PInvoke.SHAddToRecentDocs((uint)SHARD.SHARD_APPIDINFO, &info);
					}
				}

				// Get the Explorer's Pinned items from its Automatic Destination
				poc.Dispose();
				hr = pFilesADL.Get()->GetList(DESTLISTTYPE.PINNED, maxCount, GETDESTLISTFLAGS.NONE, IID.IID_IObjectCollection, (void**)poc.GetAddressOf());
				if (FAILED(hr)) return hr;

				// Get the count of the Explorer's Pinned items
				hr = poc.Get()->GetCount(&countOfItems);
				if (FAILED(hr)) return hr;

				// Copy them to the Explorer's Automatic Destination
				for (uint index = 0U; index < countOfItems; index++)
				{
					using ComPtr<IShellLinkW> psl = default;
					hr = poc.Get()->GetAt(index, IID.IID_IShellLinkW, (void**)psl.GetAddressOf());
					if (FAILED(hr)) continue;

					using ComHeapPtr<char> pszParseablePath = default;
					pszParseablePath.Allocate(PInvoke.MAX_PATH);
					hr = psl.Get()->GetArguments(pszParseablePath.Get(), (int)PInvoke.MAX_PATH);

					using ComHeapPtr<IShellItem> psi = default;
					hr = PInvoke.SHCreateItemFromParsingName(pszParseablePath.Get(), null, IID.IID_IShellItem, (void**)psi.GetAddressOf());
					if (FAILED(hr)) continue;

					fixed (char* pAumid = "Microsoft.Windows.Explorer")
					{
						SHARDAPPIDINFO info = default;
						info.psi = psi.Get();
						info.pszAppID = pAumid;

						PInvoke.SHAddToRecentDocs((uint)SHARD.SHARD_APPIDINFO, &info);
					}

					int pinIndex;
					hr = pFilesADL.Get()->GetPinIndex((IUnknown*)psl.Get(), &pinIndex);
					if (FAILED(hr)) continue;

					hr = pExplorerADL.Get()->PinItem((IUnknown*)psi.Get(), pinIndex);
					if (FAILED(hr)) continue;
				}

				return hr;
			}
			finally
			{
				if (_explorerADLStoreFileWatcher is not null && !_explorerADLStoreFileWatcher.EnableRaisingEvents)
					_explorerADLStoreFileWatcher.EnableRaisingEvents = true;
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

			_filesCDLStoreFileWatcher?.Dispose();
			_filesCDLStoreFileWatcher = new()
			{
				Path = $"{WindowsStorableHelpers.GetRecentFolderPath()}\\CustomDestinations",
				Filter = $"{aumidCrcHash}.customDestinations-ms",
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
			};

			_explorerADLStoreFileWatcher.Changed += ExplorerJumpListWatcher_Changed;
			_filesADLStoreFileWatcher.Changed += FilesJumpListWatcher_Changed;
			_filesCDLStoreFileWatcher.Changed += FilesJumpListWatcher_Changed;

			try
			{
				// NOTE: This may throw various exceptions (e.g., when the file doesn't exist or cannot be accessed)
				_explorerADLStoreFileWatcher.EnableRaisingEvents = true;
				_filesADLStoreFileWatcher.EnableRaisingEvents = true;
				_filesCDLStoreFileWatcher.EnableRaisingEvents = true;
			}
			catch
			{
				// Gracefully exit if we can't monitor the file
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
			ExplorerJumpListChanged?.Invoke(this, EventArgs.Empty);
			PullJumpListFromExplorer();
		}

		private void FilesJumpListWatcher_Changed(object sender, FileSystemEventArgs e)
		{
			FilesJumpListChanged?.Invoke(this, EventArgs.Empty);
			PushJumpListToExplorer();
		}

		public void Dispose()
		{
			if (_explorerADLStoreFileWatcher is not null)
			{
				_explorerADLStoreFileWatcher.EnableRaisingEvents = false;
				_explorerADLStoreFileWatcher.Dispose();
			}

			if (_filesADLStoreFileWatcher is not null)
			{
				_filesADLStoreFileWatcher.EnableRaisingEvents = false;
				_filesADLStoreFileWatcher.Dispose();
			}

			if (_filesCDLStoreFileWatcher is not null)
			{
				_filesCDLStoreFileWatcher.EnableRaisingEvents = false;
				_filesCDLStoreFileWatcher.Dispose();
			}
		}
	}
}
