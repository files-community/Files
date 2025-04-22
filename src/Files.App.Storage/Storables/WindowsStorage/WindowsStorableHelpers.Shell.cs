// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	public static partial class WindowsStorableHelpers
	{
		public unsafe static HRESULT GetPropertyValue<TValue>(this IWindowsStorable storable, string propKey, out TValue value)
		{
			using ComPtr<IShellItem2> pShellItem2 = default;
			var shellItem2Iid = typeof(IShellItem2).GUID;
			HRESULT hr = storable.ThisPtr.Get()->QueryInterface(&shellItem2Iid, (void**)pShellItem2.GetAddressOf());
			hr = PInvoke.PSGetPropertyKeyFromName(propKey, out var originalPathPropertyKey);
			hr = pShellItem2.Get()->GetString(originalPathPropertyKey, out var szOriginalPath);

			if (typeof(TValue) == typeof(string))
			{
				value = (TValue)(object)szOriginalPath.ToString();
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
			return storable.ThisPtr.Get()->GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var returnedAttributes).Succeeded &&
				returnedAttributes == attributes;
		}

		public unsafe static bool HasShellAttributes(this ComPtr<IShellItem> pShellItem, SFGAO_FLAGS attributes)
		{
			return pShellItem.Get()->GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var returnedAttributes).Succeeded &&
				returnedAttributes == attributes;
		}

		public unsafe static string GetDisplayName(this IWindowsStorable storable, SIGDN options = SIGDN.SIGDN_FILESYSPATH)
		{
			using ComHeapPtr<PWSTR> pszName = default;
			HRESULT hr = storable.ThisPtr.Get()->GetDisplayName(options, (PWSTR*)pszName.GetAddressOf());

			return hr.ThrowIfFailedOnDebug().Succeeded
				? (*pszName.Get()).ToString()
				: string.Empty;
		}
	}
}
