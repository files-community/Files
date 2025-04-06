// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT
{
	public unsafe struct IStorageProviderStatusUISource : IComIID
	{
		private void** lpVtbl;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT GetStatusUI(IStorageProviderStatusUI** result)
		{
			return ((delegate* unmanaged[Stdcall]<IStorageProviderStatusUISource*, IStorageProviderStatusUI**, HRESULT>)lpVtbl[6])((IStorageProviderStatusUISource*)Unsafe.AsPointer(ref this), result);
		}

		public static ref readonly Guid Guid
		{
			get
			{
				// A306C249-3D66-5E70-9007-E43DF96051FF
				ReadOnlySpan<byte> data =
				[
					0x49, 0xc2, 0x06, 0xa3,
					0x66, 0x3d,
					0x70, 0x5e,
					0x90, 0x07,
					0xe4, 0x3d, 0xf9, 0x60, 0x51, 0xff
				];

				Debug.Assert(data.Length == sizeof(Guid));
				return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
			}
		}
	}
}
