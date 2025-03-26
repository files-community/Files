// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT
{
	public unsafe struct IStorageProviderStatusUISourceFactory : IComIID
	{
		private void** lpVtbl;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT GetStatusUISource(nint syncRootId, IStorageProviderStatusUISource** result)
		{
			return ((delegate* unmanaged[Stdcall]<IStorageProviderStatusUISourceFactory*, nint, IStorageProviderStatusUISource**, HRESULT>)lpVtbl[6])((IStorageProviderStatusUISourceFactory*)Unsafe.AsPointer(ref this), syncRootId, result);
		}

		public static ref readonly Guid Guid
		{
			get
			{
				// 12E46B74-4E5A-58D1-A62F-0376E8EE7DD8
				ReadOnlySpan<byte> data =
				[
					0x74, 0x6b, 0xe4, 0x12,
					0x5a, 0x4e,
					0xd1, 0x58,
					0xa6, 0x2f,
					0x03, 0x76, 0xe8, 0xee, 0x7d, 0xd8
				];

				Debug.Assert(data.Length == sizeof(Guid));
				return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
			}
		}
	}
}
