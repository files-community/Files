// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
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
		private static readonly Lazy<JumpListManager> _default = new(() => new JumpListManager(), LazyThreadSafetyMode.ExecutionAndPublication);
		public static JumpListManager Default => _default.Value;

		private readonly static string _aumid = $"{Package.Current.Id.FamilyName}!App";

		private FileSystemWatcher? _explorerJumpListWatcher;
		private FileSystemWatcher? _filesJumpListWatcher;

		public bool FetchJumpListFromExplorer(int maxCount = 40)
		{
			if (_filesJumpListWatcher is not null && _filesJumpListWatcher.EnableRaisingEvents)
				_filesJumpListWatcher.EnableRaisingEvents = false;

			ClearAutomaticDestinationsOf(_aumid);

			// Get recent items from the Explorer's jump list
			using ComPtr<IObjectCollection> pRecentOC = default;
			GetRecentItemsOf("Microsoft.Windows.Explorer", maxCount, pRecentOC.GetAddressOf());

			// Get pinned items from the Explorer's jump list
			using ComPtr<IObjectCollection> pPinnedOC = default;
			GetPinnedItemsOf("Microsoft.Windows.Explorer", maxCount, pPinnedOC.GetAddressOf());

			// Copy them to the Files' jump list
			CopyToAutomaticDestinationsOf(_aumid, pRecentOC.Get(), pPinnedOC.Get());

			if (_filesJumpListWatcher is not null && !_filesJumpListWatcher.EnableRaisingEvents)
				_filesJumpListWatcher.EnableRaisingEvents = true;

			return true;
		}

		public bool SyncJumpListWithExplorer(int maxCount = 40)
		{
			if (_explorerJumpListWatcher is not null && _explorerJumpListWatcher.EnableRaisingEvents)
				_explorerJumpListWatcher.EnableRaisingEvents = false;

			ClearAutomaticDestinationsOf("Microsoft.Windows.Explorer");

			// Get recent items from the Explorer's jump list
			using ComPtr<IObjectCollection> pRecentOC = default;
			GetRecentItemsOf(_aumid, maxCount, pRecentOC.GetAddressOf());

			// Get pinned items from the Explorer's jump list
			using ComPtr<IObjectCollection> pPinnedOC = default;
			GetPinnedItemsOf(_aumid, maxCount, pPinnedOC.GetAddressOf());

			// Copy them to the Files' jump list
			CopyToAutomaticDestinationsOf("Microsoft.Windows.Explorer", pRecentOC.Get(), pPinnedOC.Get());

			if (_explorerJumpListWatcher is not null && !_explorerJumpListWatcher.EnableRaisingEvents)
				_explorerJumpListWatcher.EnableRaisingEvents = true;

			return true;
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

		private bool ClearAutomaticDestinationsOf(string aumid)
		{
			HRESULT hr = default;

			using ComPtr<IAutomaticDestinationList> padl = default;
			hr = PInvoke.CoCreateInstance(CLSID.CLSID_AutomaticDestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IAutomaticDestinationList, (void**)padl.GetAddressOf());
			if (FAILED(hr)) return false;

			fixed (char* pwsAppId = aumid)
				hr = padl.Get()->Initialize(pwsAppId, default, default);
			if (FAILED(hr)) return false;

			BOOL hasList = default;
			hr = padl.Get()->HasList(&hasList);
			if (FAILED(hr)) return false;

			hr = padl.Get()->ClearList(true);
			if (FAILED(hr)) return false;

			return true;
		}

		private bool ClearCustomDestinations(string aumid)
		{
			using ComPtr<IInternalCustomDestinationList> picdl = default;
			HRESULT hr = PInvoke.CoCreateInstance(CLSID.CLSID_DestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IInternalCustomDestinationList, (void**)picdl.GetAddressOf());
			if (FAILED(hr)) return false;

			fixed (char* pwszAppId = aumid)
				hr = picdl.Get()->SetApplicationID(pwszAppId);
			if (FAILED(hr)) return false;

			uint count = 0U;
			picdl.Get()->GetCategoryCount(&count);

			for (uint index = 0U; index < count; index++)
			{
				APPDESTCATEGORY category = default;

				try
				{
					hr = picdl.Get()->GetCategory(index, GETCATFLAG.DEFAULT, &category);
					if (FAILED(hr) || category.Type is not APPDESTCATEGORYTYPE.CUSTOM)
						continue;

					picdl.Get()->DeleteCategory(index, true);
					if (FAILED(hr))
						continue;
				}
				finally
				{
					// The memory layout at Name can be either PWSTR or int depending on the category type
					if (category.Anonymous.Name.Value is not null && category.Type is APPDESTCATEGORYTYPE.CUSTOM) PInvoke.CoTaskMemFree(category.Anonymous.Name);
				}
			}

			// Delete the removed destinations too
			picdl.Get()->ClearRemovedDestinations();

			return false;
		}

		private bool GetRecentItemsOf(string aumid, int maxCount, IObjectCollection** ppoc)
		{
			HRESULT hr = default;

			using ComPtr<IAutomaticDestinationList> padl = default;
			hr = PInvoke.CoCreateInstance(CLSID.CLSID_AutomaticDestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IAutomaticDestinationList, (void**)padl.GetAddressOf());
			if (FAILED(hr)) return false;

			fixed (char* pwszAppId = aumid)
				hr = padl.Get()->Initialize(pwszAppId, default, default);
			if (FAILED(hr)) return false;

			BOOL hasList = false;
			hr = padl.Get()->HasList(&hasList);
			if (hr.Failed || hasList == false) return false;

			IObjectCollection* poc = default;
			hr = padl.Get()->GetList(DESTLISTTYPE.RECENT, maxCount, GETDESTLISTFLAGS.NONE, IID.IID_IObjectCollection, (void**)&poc);
			if (FAILED(hr)) return false;

			*ppoc = poc;

			return true;
		}

		private bool GetPinnedItemsOf(string aumid, int maxCount, IObjectCollection** ppoc)
		{
			HRESULT hr = default;

			using ComPtr<IAutomaticDestinationList> padl = default;
			hr = PInvoke.CoCreateInstance(CLSID.CLSID_AutomaticDestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IAutomaticDestinationList, (void**)padl.GetAddressOf());
			if (FAILED(hr)) return false;

			fixed (char* pwszAppId = aumid)
				hr = padl.Get()->Initialize(pwszAppId, default, default);
			if (FAILED(hr)) return false;

			BOOL hasList = false;
			hr = padl.Get()->HasList(&hasList);
			if (hr.Failed || hasList == false) return false;

			IObjectCollection* poc = default;
			hr = padl.Get()->GetList(DESTLISTTYPE.PINNED, maxCount, GETDESTLISTFLAGS.NONE, IID.IID_IObjectCollection, (void**)&poc);
			if (FAILED(hr)) return false;

			*ppoc = poc;

			return true;
		}

		private bool CopyToAutomaticDestinationsOf(string aumid, IObjectCollection* pRecentOC, IObjectCollection* pPinnedOC)
		{
			HRESULT hr = default;

			using ComPtr<IAutomaticDestinationList> padl = default;
			hr = PInvoke.CoCreateInstance(CLSID.CLSID_AutomaticDestinationList, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IAutomaticDestinationList, (void**)padl.GetAddressOf());
			if (FAILED(hr)) return false;

			fixed (char* pwszAppId = aumid)
				hr = padl.Get()->Initialize(pwszAppId, default, default);
			if (FAILED(hr)) return false;

			uint cRecentItems = 0U;
			hr = pRecentOC->GetCount(&cRecentItems);

			IShellItem** ppsi = (IShellItem**)NativeMemory.AllocZeroed((nuint)(sizeof(void*) * cRecentItems + 1));

			for (int index = 0; index < cRecentItems; index++)
			{
				IShellItem* psi = default;
				hr = pRecentOC->GetAt((uint)index, IID.IID_IShellItem, (void**)&psi);
				if (hr.Failed) continue;

				ppsi[index] = psi;
			}

			// Reverse the order to maintain the original order in the jump list
			for (int index = (int)cRecentItems -1; index >= 0U; index--)
			{
				padl.Get()->AddUsagePoint((IUnknown*)ppsi[index]);
				ppsi[index]->Release();
			}

			uint cPinnedItems = 0U;
			hr = pPinnedOC->GetCount(&cPinnedItems);
			for (uint dwIndex = 0U; dwIndex < cRecentItems; dwIndex++)
			{
				using ComPtr<IShellItem> psi = default;
				hr = pPinnedOC->GetAt(dwIndex, IID.IID_IShellItem, (void**)psi.GetAddressOf());
				if (hr.Failed) continue;

				padl.Get()->AddUsagePoint((IUnknown*)psi.Get());
				padl.Get()->PinItem((IUnknown*)psi.Get(), -1);
			}

			NativeMemory.Free(ppsi);

			return true;
		}

		private void ExplorerJumpListWatcher_Changed(object sender, FileSystemEventArgs e)
		{
			FetchJumpListFromExplorer();
		}

		private void FilesJumpListWatcher_Changed(object sender, FileSystemEventArgs e)
		{
			SyncJumpListWithExplorer();
		}

		public void Dispose()
		{
			if (_explorerJumpListWatcher is not null)
			{
				_explorerJumpListWatcher.EnableRaisingEvents = false;
				_explorerJumpListWatcher.Dispose();
			}
		}
	}
}
