// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.PropertiesSystem;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Storage
{
	public static partial class WindowsStorableHelpers
	{
		public unsafe static HRESULT GetPropertyValue<TValue>(this IWindowsStorable storable, string propKey, out TValue value)
		{
			using ComPtr<IShellItem2> pShellItem2 = default;
			HRESULT hr = storable.ThisPtr->QueryInterface(IID.IID_IShellItem2, (void**)pShellItem2.GetAddressOf());

			PROPERTYKEY propertyKey = default;
			fixed (char* pszPropertyKey = propKey)
				hr = PInvoke.PSGetPropertyKeyFromName(pszPropertyKey, &propertyKey);

			if (typeof(TValue) == typeof(string))
			{
				ComHeapPtr<PWSTR> szPropertyValue = default;
				hr = pShellItem2.Get()->GetString(&propertyKey, szPropertyValue.Get());
				value = (TValue)(object)szPropertyValue.Get()->ToString();

				return hr;
			}
			if (typeof(TValue) == typeof(bool))
			{
				bool fPropertyValue = false;
				hr = pShellItem2.Get()->GetBool(&propertyKey, (BOOL*)&fPropertyValue);
				value = Unsafe.As<bool, TValue>(ref fPropertyValue);

				return hr;
			}
			else
			{
				value = default!;
				return HRESULT.E_FAIL;
			}
		}

		public unsafe static bool HasShellAttributes(this IWindowsStorable storable, SFGAO_FLAGS attributes)
		{
			return storable.ThisPtr->GetAttributes(attributes, out var dwRetAttributes).Succeeded && dwRetAttributes == attributes;
		}

		public unsafe static string GetDisplayName(this IWindowsStorable storable, SIGDN options = SIGDN.SIGDN_FILESYSPATH)
		{
			using ComHeapPtr<PWSTR> pszName = default;
			HRESULT hr = storable.ThisPtr->GetDisplayName(options, (PWSTR*)pszName.GetAddressOf());

			return hr.ThrowIfFailedOnDebug().Succeeded
				? new string((char*)pszName.Get()) // this is safe as it gets memcpy'd internally
				: string.Empty;
		}

		public unsafe static HRESULT TryInvokeContextMenuVerb(this IWindowsStorable storable, string verbName)
		{
			Debug.Assert(Thread.CurrentThread.GetApartmentState() is ApartmentState.STA);

			using ComPtr<IContextMenu> pContextMenu = default;
			HRESULT hr = storable.ThisPtr->BindToHandler(null, BHID.BHID_SFUIObject, IID.IID_IContextMenu, (void**)pContextMenu.GetAddressOf());
			HMENU hMenu = PInvoke.CreatePopupMenu();
			hr = pContextMenu.Get()->QueryContextMenu(hMenu, 0, 1, 0x7FFF, PInvoke.CMF_OPTIMIZEFORINVOKE);

			CMINVOKECOMMANDINFO cmici = default;
			cmici.cbSize = (uint)sizeof(CMINVOKECOMMANDINFO);
			cmici.nShow = (int)SHOW_WINDOW_CMD.SW_HIDE;

			fixed (byte* pszVerbName = Encoding.ASCII.GetBytes(verbName))
			{
				cmici.lpVerb = new(pszVerbName);
				hr = pContextMenu.Get()->InvokeCommand(cmici);

				if (!PInvoke.DestroyMenu(hMenu))
					return HRESULT.E_FAIL;

				return hr;
			}
		}

		public unsafe static HRESULT TryInvokeContextMenuVerbs(this IWindowsStorable storable, string[] verbNames, bool earlyReturnOnSuccess)
		{
			Debug.Assert(Thread.CurrentThread.GetApartmentState() is ApartmentState.STA);

			using ComPtr<IContextMenu> pContextMenu = default;
			HRESULT hr = storable.ThisPtr->BindToHandler(null, BHID.BHID_SFUIObject, IID.IID_IContextMenu, (void**)pContextMenu.GetAddressOf());
			HMENU hMenu = PInvoke.CreatePopupMenu();
			hr = pContextMenu.Get()->QueryContextMenu(hMenu, 0, 1, 0x7FFF, PInvoke.CMF_OPTIMIZEFORINVOKE);

			CMINVOKECOMMANDINFO cmici = default;
			cmici.cbSize = (uint)sizeof(CMINVOKECOMMANDINFO);
			cmici.nShow = (int)SHOW_WINDOW_CMD.SW_HIDE;

			foreach (var verbName in verbNames)
			{
				fixed (byte* pszVerbName = Encoding.ASCII.GetBytes(verbName))
				{
					cmici.lpVerb = new(pszVerbName);
					hr = pContextMenu.Get()->InvokeCommand(cmici);

					if (!PInvoke.DestroyMenu(hMenu))
						return HRESULT.E_FAIL;

					if (hr.Succeeded && earlyReturnOnSuccess)
						return hr;
				}
			}

			return hr;
		}

		public unsafe static HRESULT TryGetShellTooltip(this IWindowsStorable storable, out string? tooltip)
		{
			tooltip = null;

			using ComPtr<IQueryInfo> pQueryInfo = default;
			HRESULT hr = storable.ThisPtr->BindToHandler(null, BHID.BHID_SFUIObject, IID.IID_IQueryInfo, (void**)pQueryInfo.GetAddressOf());
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			pQueryInfo.Get()->GetInfoTip((uint)QITIPF_FLAGS.QITIPF_DEFAULT, out var pszTip);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			tooltip = pszTip.ToString();
			PInvoke.CoTaskMemFree(pszTip);

			return HRESULT.S_OK;
		}
	}
}
