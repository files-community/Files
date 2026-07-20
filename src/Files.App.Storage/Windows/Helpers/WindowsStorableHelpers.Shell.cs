// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

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
			if (storable.ThisPtr is not IShellItem2 pShellItem2)
			{
				value = default!;
				return HRESULT.E_NOINTERFACE;
			}

			HRESULT hr = PInvoke.PSGetPropertyKeyFromName(propKey, out PROPERTYKEY propertyKey);
			if (hr.ThrowIfFailedOnDebug().Failed)
			{
				value = default!;
				return hr;
			}

			if (typeof(TValue) == typeof(string))
			{
				hr = pShellItem2.GetString(propertyKey, out PWSTR szPropertyValue);
				value = (TValue)(object)szPropertyValue.ToString();
				PInvoke.CoTaskMemFree(szPropertyValue);

				return hr;
			}
			if (typeof(TValue) == typeof(bool))
			{
				hr = pShellItem2.GetBool(propertyKey, out BOOL fPropertyValue);
				bool propertyValue = fPropertyValue;
				value = Unsafe.As<bool, TValue>(ref propertyValue);

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
			HRESULT hr = storable.ThisPtr.GetDisplayName(options, out PWSTR pszName);
			string name = hr.ThrowIfFailedOnDebug().Succeeded
				? pszName.ToString()
				: string.Empty;
			PInvoke.CoTaskMemFree(pszName);

			return name;
		}

		public static HRESULT TryInvokeContextMenuVerb(this IWindowsStorable storable, string verbName)
		{
			Debug.Assert(Thread.CurrentThread.GetApartmentState() is ApartmentState.STA);

			HRESULT hr = storable.ThisPtr.BindToHandler(null, PInvoke.BHID_SFUIObject, out IContextMenu? pContextMenu);
			if (hr.ThrowIfFailedOnDebug().Failed || pContextMenu is null)
				return hr;

			HMENU hMenu = PInvoke.CreatePopupMenu();
			hr = pContextMenu.QueryContextMenu(hMenu, 0, 1, 0x7FFF, PInvoke.CMF_OPTIMIZEFORINVOKE);

			CMINVOKECOMMANDINFO cmici = default;
			cmici.cbSize = (uint)sizeof(CMINVOKECOMMANDINFO);
			cmici.nShow = (int)SHOW_WINDOW_CMD.SW_HIDE;

			fixed (byte* pszVerbName = Encoding.ASCII.GetBytes(verbName))
			{
				cmici.lpVerb = new(pszVerbName);
				hr = pContextMenu.InvokeCommand(cmici);

				if (!PInvoke.DestroyMenu(hMenu))
					return HRESULT.E_FAIL;

				return hr;
			}
		}

		public static HRESULT TryInvokeContextMenuVerbs(this IWindowsStorable storable, string[] verbNames, bool earlyReturnOnSuccess)
		{
			Debug.Assert(Thread.CurrentThread.GetApartmentState() is ApartmentState.STA);

			HRESULT hr = storable.ThisPtr.BindToHandler(null, PInvoke.BHID_SFUIObject, out IContextMenu? pContextMenu);
			if (hr.ThrowIfFailedOnDebug().Failed || pContextMenu is null)
				return hr;

			HMENU hMenu = PInvoke.CreatePopupMenu();
			HRESULT result = HRESULT.S_OK;
			try
			{
				hr = pContextMenu.QueryContextMenu(hMenu, 0, 1, 0x7FFF, PInvoke.CMF_OPTIMIZEFORINVOKE);
				result = hr;

				CMINVOKECOMMANDINFO cmici = default;
				cmici.cbSize = (uint)sizeof(CMINVOKECOMMANDINFO);
				cmici.nShow = (int)SHOW_WINDOW_CMD.SW_HIDE;

				foreach (var verbName in verbNames)
				{
					fixed (byte* pszVerbName = Encoding.ASCII.GetBytes(verbName))
					{
						cmici.lpVerb = new(pszVerbName);
						hr = pContextMenu.InvokeCommand(cmici);
						result = hr;

						if (hr.Succeeded && earlyReturnOnSuccess)
							break;
					}
				}
			}
			finally
			{
				if (!PInvoke.DestroyMenu(hMenu))
					result = HRESULT.E_FAIL;
			}

			return result;
		}

		public static HRESULT TryGetShellTooltip(this IWindowsStorable storable, out string? tooltip)
		{
			tooltip = null;

			HRESULT hr = storable.ThisPtr.BindToHandler(null, PInvoke.BHID_SFUIObject, out IQueryInfo? pQueryInfo);
			if (hr.ThrowIfFailedOnDebug().Failed || pQueryInfo is null)
				return hr;

			hr = pQueryInfo.GetInfoTip(QITIPF_FLAGS.QITIPF_DEFAULT, out var pszTip);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			tooltip = pszTip.ToString();
			PInvoke.CoTaskMemFree(pszTip);

			return HRESULT.S_OK;
		}

		public static HRESULT TryPinFolderToQuickAccess(this IWindowsFolder @this)
		{
			HRESULT hr = default;

			hr = PInvoke.CoCreateInstance(CLSID.CLSID_PinToFrequentExecute, null, CLSCTX.CLSCTX_INPROC_SERVER, out IExecuteCommand? pExecuteCommand);
			if (hr.ThrowIfFailedOnDebug().Failed || pExecuteCommand is null)
				return hr;

			hr = PInvoke.SHCreateShellItemArrayFromShellItem(@this.ThisPtr, out IShellItemArray pShellItemArray);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			if (pExecuteCommand is not IObjectWithSelection pObjectWithSelection)
				return HRESULT.E_NOINTERFACE;

			hr = pObjectWithSelection.SetSelection(pShellItemArray);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			hr = pExecuteCommand.Execute();
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			return HRESULT.S_OK;
		}

		public static HRESULT TryUnpinFolderFromQuickAccess(this IWindowsFolder @this)
		{
			HRESULT hr = default;

			hr = PInvoke.CoCreateInstance(CLSID.CLSID_UnPinFromFrequentExecute, null, CLSCTX.CLSCTX_INPROC_SERVER, out IExecuteCommand? pExecuteCommand);
			if (hr.ThrowIfFailedOnDebug().Failed || pExecuteCommand is null)
				return hr;

			hr = PInvoke.SHCreateShellItemArrayFromShellItem(@this.ThisPtr, out IShellItemArray pShellItemArray);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			if (pExecuteCommand is not IObjectWithSelection pObjectWithSelection)
				return HRESULT.E_NOINTERFACE;

			hr = pObjectWithSelection.SetSelection(pShellItemArray);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			hr = pExecuteCommand.Execute();
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			return HRESULT.S_OK;
		}

		public static IEnumerable<WindowsContextMenuItem> GetShellNewItems(this IWindowsFolder @this)
		{
			HRESULT hr = default;

			hr = PInvoke.CoCreateInstance(CLSID.CLSID_NewMenu, null, CLSCTX.CLSCTX_INPROC_SERVER, out IContextMenu? pNewMenu);
			if (hr.ThrowIfFailedOnDebug().Failed || pNewMenu is null)
				return [];

			if (pNewMenu is not IContextMenu2 pContextMenu2 ||
				pNewMenu is not IShellExtInit pShellExtInit)
				return [];

			@this.ShellNewMenu = pNewMenu;

			hr = PInvoke.SHGetIDListFromObject(@this.ThisPtr, out ITEMIDLIST* pFolderPidl);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return [];

			hr = pShellExtInit.Initialize(pFolderPidl, null!, default);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return [];

			// Inserts "New (&W)"
			HMENU hMenu = PInvoke.CreatePopupMenu();
			hr = pNewMenu.QueryContextMenu(hMenu, 0, 1, 256, 0);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return [];

			// Invokes CNewMenu::_InitMenuPopup(), which populates the hSubMenu
			HMENU hSubMenu = PInvoke.GetSubMenu(hMenu, 0);
			hr = pContextMenu2.HandleMenuMsg(PInvoke.WM_INITMENUPOPUP, (WPARAM)(nuint)hSubMenu.Value, 0);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return [];

			uint dwCount = unchecked((uint)PInvoke.GetMenuItemCount(hSubMenu));
			if (dwCount is unchecked((uint)-1))
				return [];

			// Enumerates and populates the list
			List<WindowsContextMenuItem> shellNewItems = [];
			for (uint dwIndex = 0; dwIndex < dwCount; dwIndex++)
			{
				MENUITEMINFOW mii = default;
				mii.cbSize = (uint)sizeof(MENUITEMINFOW);
				mii.fMask = MENU_ITEM_MASK.MIIM_STRING | MENU_ITEM_MASK.MIIM_ID | MENU_ITEM_MASK.MIIM_STATE;
				mii.dwTypeData = (char*)NativeMemory.Alloc(256U);
				mii.cch = 256;

				if (PInvoke.GetMenuItemInfo(hSubMenu, dwIndex, true, &mii))
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
				hr = PInvoke.CoCreateInstance(CLSID.CLSID_NewMenu, null, CLSCTX.CLSCTX_INPROC_SERVER, out IContextMenu? pNewMenu);
				if (hr.ThrowIfFailedOnDebug().Failed || pNewMenu is null)
					return false;

				@this.ShellNewMenu = pNewMenu;
			}

			CMINVOKECOMMANDINFO cmici = default;
			cmici.cbSize = (uint)sizeof(CMINVOKECOMMANDINFO);
			cmici.lpVerb = (PCSTR)(byte*)item.Id;
			cmici.nShow = (int)SHOW_WINDOW_CMD.SW_SHOWNORMAL;

			hr = @this.ShellNewMenu.InvokeCommand(cmici);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return false;

			return false;
		}
	}
}
