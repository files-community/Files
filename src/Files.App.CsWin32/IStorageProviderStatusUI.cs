// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT
{
	public unsafe struct IStorageProviderStatusUI : IComIID
	{
		private void** lpVtbl;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT GetQuotaUI(IStorageProviderQuotaUI** result)
		{
			return ((delegate* unmanaged[Stdcall]<IStorageProviderStatusUI*, IStorageProviderQuotaUI**, HRESULT>)lpVtbl[14])((IStorageProviderStatusUI*)Unsafe.AsPointer(ref this), result);
		}

		public static ref readonly Guid Guid
		{
			get
			{
				// d6b6a758-198d-5b80-977f-5ff73da33118
				ReadOnlySpan<byte> data =
				[
					0x58, 0xa7, 0xb6, 0xd6,
					0x8d, 0x19,
					0x80, 0x5b,
					0x97, 0x7f,
					0x5f, 0xf7, 0x3d, 0xa3, 0x31, 0x18
				];

				Debug.Assert(data.Length == sizeof(Guid));
				return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
			}
		}
	}
}
