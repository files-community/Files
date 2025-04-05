// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage.Storables
{
	public static class WindowsStorableHelpers
	{
		public unsafe static HRESULT GetPropertyValue<TValue>(this WindowsStorable file, string propKey, out TValue value)
		{
			using ComPtr<IShellItem2> pShellItem2 = default;
			var shellItem2Iid = typeof(IShellItem2).GUID;
			HRESULT hr = file.ThisPtr.Get()->QueryInterface(&shellItem2Iid, (void**)pShellItem2.GetAddressOf());
			hr = PInvoke.PSGetPropertyKeyFromName(propKey, out var originalPathPropertyKey);
			hr = pShellItem2.Get()->GetString(originalPathPropertyKey, out var szOriginalPath);

			if (typeof(TValue) == typeof(string))
			{
				value = (TValue)(object)szOriginalPath.ToString();
				return hr;
			}
			else if (typeof(TValue) == typeof(DateTimeOffset) && DateTimeOffset.TryParse(szOriginalPath.ToString(), out var dateTimeOffset))
			{
				value = (TValue)(object)dateTimeOffset;
				return hr;
			}
			else
			{
				value = default!;
				return HRESULT.E_FAIL;
			}
		}
	}
}
