// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using Windows.Win32.UI.Shell.PropertiesSystem;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Storage
{
	public unsafe static partial class WindowsStorableHelpers
	{
		public static HRESULT GetPropertyValue<TValue>(this IWindowsStorable storable, string propKey, out TValue value)
		{
			HRESULT hr = default;
			var shellItem2 = (IShellItem2)storable.ThisPtr;

			PROPERTYKEY propertyKey = default;
			fixed (char* pszPropertyKey = propKey)
				hr = PInvoke.PSGetPropertyKeyFromName(pszPropertyKey, &propertyKey);

			if (typeof(TValue) == typeof(string))
			{
				ComHeapPtr<PWSTR> szPropertyValue = default;
				hr = shellItem2.GetString(&propertyKey, szPropertyValue.Get());
				value = (TValue)(object)szPropertyValue.Get()->ToString();

				return hr;
			}
			if (typeof(TValue) == typeof(bool))
			{
				bool fPropertyValue = false;
				hr = shellItem2.GetBool(&propertyKey, (BOOL*)&fPropertyValue);
				value = Unsafe.As<bool, TValue>(ref fPropertyValue);

				return hr;
			}
			else
			{
				value = default!;
				return HRESULT.E_FAIL;
			}
		}

		public static bool HasShellAttributes(this IWindowsStorable storable, SFGAO_FLAGS attributes)
		{
			return storable.ThisPtr.GetAttributes(attributes, out var dwRetAttributes).Succeeded && dwRetAttributes == attributes;
		}

		public static string GetDisplayName(this IWindowsStorable storable, SIGDN options = SIGDN.SIGDN_FILESYSPATH)
		{
			using ComHeapPtr<PWSTR> pszName = default;
			HRESULT hr = storable.ThisPtr.GetDisplayName(options, (PWSTR*)pszName.GetAddressOf());

			return hr.ThrowIfFailedOnDebug().Succeeded
				? new string((char*)pszName.Get()) // this is safe as it gets memcpy'd internally
				: string.Empty;
		}

		public static HRESULT TryInvokeContextMenuVerb(this IWindowsStorable storable, string verbName)
		{
			Debug.Assert(Thread.CurrentThread.GetApartmentState() is ApartmentState.STA);

			HRESULT hr = storable.ThisPtr.BindToHandler(null, BHID.BHID_SFUIObject, IID.IID_IContextMenu, out var contextMenuObj);
			var contextMenu = (IContextMenu)contextMenuObj;
			HMENU hMenu = PInvoke.CreatePopupMenu();
			hr = contextMenu.QueryContextMenu(hMenu, 0, 1, 0x7FFF, PInvoke.CMF_OPTIMIZEFORINVOKE);

			CMINVOKECOMMANDINFO cmici = default;
			cmici.cbSize = (uint)sizeof(CMINVOKECOMMANDINFO);
			cmici.nShow = (int)SHOW_WINDOW_CMD.SW_HIDE;

			fixed (byte* pszVerbName = Encoding.ASCII.GetBytes(verbName))
			{
				cmici.lpVerb = new(pszVerbName);
				hr = contextMenu.InvokeCommand(cmici);

				if (!PInvoke.DestroyMenu(hMenu))
					return HRESULT.E_FAIL;

				return hr;
			}
		}

		public static HRESULT TryInvokeContextMenuVerbs(this IWindowsStorable storable, string[] verbNames, bool earlyReturnOnSuccess)
		{
			Debug.Assert(Thread.CurrentThread.GetApartmentState() is ApartmentState.STA);

			HRESULT hr = storable.ThisPtr.BindToHandler(null, BHID.BHID_SFUIObject, IID.IID_IContextMenu, out var contextMenuObj);
			var contextMenu = (IContextMenu)contextMenuObj;
			HMENU hMenu = PInvoke.CreatePopupMenu();
			hr = contextMenu.QueryContextMenu(hMenu, 0, 1, 0x7FFF, PInvoke.CMF_OPTIMIZEFORINVOKE);

			CMINVOKECOMMANDINFO cmici = default;
			cmici.cbSize = (uint)sizeof(CMINVOKECOMMANDINFO);
			cmici.nShow = (int)SHOW_WINDOW_CMD.SW_HIDE;

			foreach (var verbName in verbNames)
			{
				fixed (byte* pszVerbName = Encoding.ASCII.GetBytes(verbName))
				{
					cmici.lpVerb = new(pszVerbName);
					hr = contextMenu.InvokeCommand(cmici);

					if (!PInvoke.DestroyMenu(hMenu))
						return HRESULT.E_FAIL;

					if (hr.Succeeded && earlyReturnOnSuccess)
						return hr;
				}
			}

			return hr;
		}

		public static HRESULT TryGetShellTooltip(this IWindowsStorable storable, out string? tooltip)
		{
			tooltip = null;

			HRESULT hr = storable.ThisPtr.BindToHandler(null, BHID.BHID_SFUIObject, IID.IID_IQueryInfo, out var queryInfoObj);
			var queryInfo = (IQueryInfo)queryInfoObj;
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			queryInfo.GetInfoTip((uint)QITIPF_FLAGS.QITIPF_DEFAULT, out var pszTip);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			tooltip = pszTip.ToString();
			PInvoke.CoTaskMemFree(pszTip);

			return HRESULT.S_OK;
		}

		public static HRESULT TryPinFolderToQuickAccess(this IWindowsFolder @this)
		{
			HRESULT hr = default;

			hr = PInvoke.CoCreateInstance(*CLSID.CLSID_PinToFrequentExecute, null, CLSCTX.CLSCTX_INPROC_SERVER, out IExecuteCommand executeCommand);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			hr = PInvoke.SHCreateShellItemArrayFromShellItem(@this.ThisPtr, *IID.IID_IShellItemArray, out var shellItemArrayObj);
			var shellItemArray = (IShellItemArray)shellItemArrayObj;
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			var objectWithSelection = (IObjectWithSelection)executeCommand;

			hr = objectWithSelection.SetSelection(shellItemArray);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			hr = executeCommand.Execute();
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			return HRESULT.S_OK;
		}

		public static HRESULT TryUnpinFolderFromQuickAccess(this IWindowsFolder @this)
		{
			HRESULT hr = default;

			hr = PInvoke.CoCreateInstance(*CLSID.CLSID_UnPinFromFrequentExecute, null, CLSCTX.CLSCTX_INPROC_SERVER, out IExecuteCommand executeCommand);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			hr = PInvoke.SHCreateShellItemArrayFromShellItem(@this.ThisPtr, *IID.IID_IShellItemArray, out var shellItemArrayObj);
			var shellItemArray = (IShellItemArray)shellItemArrayObj;
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			var objectWithSelection = (IObjectWithSelection)executeCommand;

			hr = objectWithSelection.SetSelection(shellItemArray);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			hr = executeCommand.Execute();
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			return HRESULT.S_OK;
		}

		public static IEnumerable<WindowsContextMenuItem> GetShellNewItems(this IWindowsFolder @this)
		{
			HRESULT hr = default;

			hr = PInvoke.CoCreateInstance(*CLSID.CLSID_NewMenu, null, CLSCTX.CLSCTX_INPROC_SERVER, out IContextMenu newMenu);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return [];

			var contextMenu2 = (IContextMenu2)newMenu;
			var shellExtInit = (IShellExtInit)newMenu;

			@this.ShellNewMenu = newMenu;

			ITEMIDLIST* folderPidl = default;
			hr = PInvoke.SHGetIDListFromObject(@this.ThisPtr, &folderPidl);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return [];

			hr = shellExtInit.Initialize(folderPidl, null, default);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return [];

			// Inserts "New (&W)"
			HMENU hMenu = PInvoke.CreatePopupMenu();
			hr = newMenu.QueryContextMenu(hMenu, 0, 1, 256, 0);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return [];

			// Invokes CNewMenu::_InitMenuPopup(), which populates the hSubMenu
			HMENU hSubMenu = PInvoke.GetSubMenu(hMenu, 0);
			hr = contextMenu2.HandleMenuMsg(PInvoke.WM_INITMENUPOPUP, (WPARAM)(nuint)hSubMenu.Value, 0);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return [];

			uint count = unchecked((uint)PInvoke.GetMenuItemCount(hSubMenu));
			if (count is unchecked((uint)-1))
				return [];

			// Enumerates and populates the list
			List<WindowsContextMenuItem> shellNewItems = [];
			for (uint index = 0; index < count; index++)
			{
				MENUITEMINFOW mii = default;
				mii.cbSize = (uint)sizeof(MENUITEMINFOW);
				mii.fMask = MENU_ITEM_MASK.MIIM_STRING | MENU_ITEM_MASK.MIIM_ID | MENU_ITEM_MASK.MIIM_STATE;
				mii.dwTypeData = (char*)NativeMemory.Alloc(256U);
				mii.cch = 256;

				if (PInvoke.GetMenuItemInfo(hSubMenu, index, true, &mii))
				{
					shellNewItems.Add(new()
					{
						Id = mii.wID,
						Name = mii.dwTypeData.ToString(),
						Type = (WindowsContextMenuType)mii.fState,
					});
				}

				NativeMemory.Free(mii.dwTypeData);
			}

			return shellNewItems;
		}

		public static bool InvokeShellNewItem(this IWindowsFolder @this, WindowsContextMenuItem item)
		{
			HRESULT hr = default;

			if (@this.ShellNewMenu is null)
			{
				hr = PInvoke.CoCreateInstance(*CLSID.CLSID_NewMenu, null, CLSCTX.CLSCTX_INPROC_SERVER, out IContextMenu newMenu);
				if (hr.ThrowIfFailedOnDebug().Failed)
					return false;

				@this.ShellNewMenu = newMenu;
			}

			CMINVOKECOMMANDINFO cmici = default;
			cmici.cbSize = (uint)sizeof(CMINVOKECOMMANDINFO);
			cmici.lpVerb = (PCSTR)(byte*)item.Id;
			cmici.nShow = (int)SHOW_WINDOW_CMD.SW_SHOWNORMAL;

			hr = @this.ShellNewMenu.InvokeCommand(&cmici);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return false;

			return false;
		}
	}
}
