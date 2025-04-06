// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT
{
	public unsafe struct IStorageProviderQuotaUI : IComIID
	{
		private void** lpVtbl;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT GetQuotaTotalInBytes(ulong* value)
		{
			return ((delegate* unmanaged[Stdcall]<IStorageProviderQuotaUI*, ulong*, HRESULT>)(lpVtbl[6]))((IStorageProviderQuotaUI*)Unsafe.AsPointer(ref this), value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT GetQuotaUsedInBytes(ulong* value)
		{
			return ((delegate* unmanaged[Stdcall]<IStorageProviderQuotaUI*, ulong*, HRESULT>)(lpVtbl[8]))((IStorageProviderQuotaUI*)Unsafe.AsPointer(ref this), value);
		}

		public static ref readonly Guid Guid
		{
			get
			{
				// BA6295C3-312E-544F-9FD5-1F81B21F3649
				ReadOnlySpan<byte> data =
				[
					0xC3, 0x95, 0x62, 0xBA,
					0x2E, 0x31,
					0x4F, 0x54,
					0x9F, 0xD5,
					0x1F, 0x81, 0xB2, 0x1F, 0x36, 0x49
				];

				Debug.Assert(data.Length == sizeof(Guid));
				return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
			}
		}
	}
}
