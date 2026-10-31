// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;
using Windows.Win32.System.WinRT;

namespace Windows.Win32;

[CustomMarshaller(typeof(string), MarshalMode.ManagedToUnmanagedIn, typeof(HStringStringMarshaller.ManagedToUnmanagedIn))]
[CustomMarshaller(typeof(string), MarshalMode.ManagedToUnmanagedOut, typeof(HStringStringMarshaller.ManagedToUnmanagedOut))]
[CustomMarshaller(typeof(string), MarshalMode.UnmanagedToManagedIn, typeof(HStringStringMarshaller.UnmanagedToManagedIn))]
[CustomMarshaller(typeof(string), MarshalMode.UnmanagedToManagedOut, typeof(HStringStringMarshaller.UnmanagedToManagedOut))]
internal static unsafe class HStringStringMarshaller
{
	public ref struct ManagedToUnmanagedIn
	{
		private nint _hstring;

		public void FromManaged(string? managed)
		{
			_hstring = CreateHString(managed);
		}

		public nint ToUnmanaged()
			=> _hstring;

		public void Free()
		{
			DeleteHString(_hstring);
		}
	}

	public ref struct UnmanagedToManagedOut
	{
		private nint _hstring;

		public void FromManaged(string? managed)
		{
			_hstring = CreateHString(managed);
		}

		public nint ToUnmanaged()
			=> _hstring;

		public void Free()
		{
		}
	}

	public ref struct UnmanagedToManagedIn
	{
		private nint _hstring;

		public void FromUnmanaged(nint unmanaged)
		{
			_hstring = unmanaged;
		}

		public string? ToManaged()
		{
			return ToManagedString(_hstring);
		}

		public void Free()
		{
		}
	}

	public ref struct ManagedToUnmanagedOut
	{
		private nint _hstring;

		public void FromUnmanaged(nint unmanaged)
		{
			_hstring = unmanaged;
		}

		public string? ToManaged()
		{
			return ToManagedString(_hstring);
		}

		public void Free()
		{
			DeleteHString(_hstring);
		}
	}

	private static nint CreateHString(string? managed)
	{
		if (managed is null)
			return 0;

		HSTRING hstring;
		fixed (char* sourceString = managed)
		{
			HRESULT hr = PInvoke.WindowsCreateString(new(sourceString), checked((uint)managed.Length), &hstring);
			if (hr.Failed)
				Marshal.ThrowExceptionForHR(hr.Value);
		}

		return hstring;
	}

	private static string? ToManagedString(nint hstring)
	{
		if (hstring == 0)
			return null;

		uint length;
		PCWSTR buffer = PInvoke.WindowsGetStringRawBuffer(new HSTRING(hstring), &length);
		return new string((char*)buffer.Value, 0, checked((int)length));
	}

	private static void DeleteHString(nint hstring)
	{
		if (hstring != 0)
			PInvoke.WindowsDeleteString(new HSTRING(hstring));
	}
}
