// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.ApplicationModel.Activation;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.System.Variant;
using Windows.Win32.System.WinRT;
using Windows.Win32.UI.WindowsAndMessaging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Windows.Win32
{
	public unsafe static partial class PInvoke
	{
		public static HRESULT InitPropVariantFromString(char* psz, PROPVARIANT* ppropvar)
		{
			HRESULT hr = psz != null ? HRESULT.S_OK : HRESULT.E_INVALIDARG;

			if (SUCCEEDED(hr))
			{
				nuint byteCount = (nuint)((MemoryMarshal.CreateReadOnlySpanFromNullTerminated(psz).Length + 1) * 2);

				((ppropvar)->Anonymous.Anonymous.Anonymous.pwszVal) = (char*)(PInvoke.CoTaskMemAlloc(byteCount));
				hr = ((ppropvar)->Anonymous.Anonymous.Anonymous.pwszVal) != null ? HRESULT.S_OK : HRESULT.E_OUTOFMEMORY;
				if (SUCCEEDED(hr))
				{
					NativeMemory.Copy(psz, ((ppropvar)->Anonymous.Anonymous.Anonymous.pwszVal), unchecked(byteCount));
					((ppropvar)->Anonymous.Anonymous.vt) = VARENUM.VT_LPWSTR;
				}
			}

			if (FAILED(hr))
			{
				PInvoke.PropVariantInit(ppropvar);
			}

			return hr;
		}

		public static HRESULT InitPropVariantFromBoolean(BOOL fVal, PROPVARIANT* ppropvar)
		{
			ppropvar->Anonymous.Anonymous.vt = VARENUM.VT_BOOL;
			ppropvar->Anonymous.Anonymous.Anonymous.boolVal = (VARIANT_BOOL)(bool)fVal;
			return HRESULT.S_OK;
		}

		public static HRESULT InitVariantFromBuffer(void* pv, uint cb, PROPVARIANT* ppropvar)
		{
			HRESULT hr;
			SAFEARRAY* arr;
			void* data;

			arr = SafeArrayCreateVector(VARENUM.VT_UI1, 0, cb);
			if (arr is null)
				return HRESULT.E_OUTOFMEMORY;

			hr = SafeArrayAccessData(arr, &data);
			if (FAILED(hr))
			{
				SafeArrayDestroy(arr);
				return hr;
			}

			Buffer.MemoryCopy(data, pv, cb, cb);

			hr = SafeArrayUnaccessData(arr);
			if (FAILED(hr))
			{
				SafeArrayDestroy(arr);
				return hr;
			}

			ppropvar->Anonymous.Anonymous.vt = VARENUM.VT_ARRAY | VARENUM.VT_UI1;
			ppropvar->Anonymous.Anonymous.Anonymous.parray = arr;

			return HRESULT.S_OK;
		}

		public static void PropVariantInit(PROPVARIANT* pvar)
		{
			NativeMemory.Fill(pvar, (uint)(sizeof(PROPVARIANT)), 0);
		}

		/// <inheritdoc cref="RoActivateInstance(HSTRING, IInspectable**)"/>
		[SupportedOSPlatform("windows8.0")]
		[OverloadResolutionPriority(1)]
		public static HRESULT RoActivateInstance(string activatableClassId, IInspectable** instance)
		{
			WindowsDeleteStringSafeHandle activatableClassIdAsHSTRING = null!;

			try
			{
				HRESULT hr = WindowsCreateString(activatableClassId, (uint)activatableClassId.Length, out activatableClassIdAsHSTRING);
				if (hr.Failed) return hr;

				return RoActivateInstance(activatableClassIdAsHSTRING, instance);
			}
			finally
			{
				if (!activatableClassIdAsHSTRING.IsInvalid)
					activatableClassIdAsHSTRING.Close();
			}
		}

		public static unsafe nint SetWindowLongPtr(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, nint dwNewLong)
		{
			// NOTE:
			//  Since CsWin32 generates SetWindowLong only on x86 and SetWindowLongPtr only on x64,
			//  we need to manually define both functions here.
			//  For more info, visit https://github.com/microsoft/CsWin32/issues/882
			return sizeof(nint) is 4
				? _SetWindowLong(hWnd, (int)nIndex, (int)dwNewLong)
				: _SetWindowLongPtr(hWnd, (int)nIndex, dwNewLong);

			[DllImport("User32", EntryPoint = "SetWindowLongW", ExactSpelling = true)]
			static extern int _SetWindowLong(HWND hWnd, int nIndex, int dwNewLong);

			[DllImport("User32", EntryPoint = "SetWindowLongPtrW", ExactSpelling = true)]
			static extern nint _SetWindowLongPtr(HWND hWnd, int nIndex, nint dwNewLong);
		}

		[LibraryImport("Shell32.dll", EntryPoint = "SHUpdateRecycleBinIcon")]
		public static partial void SHUpdateRecycleBinIcon();
	}
}
