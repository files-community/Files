// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT
{
	public unsafe partial struct IStorageProviderStatusUI : IComIID
	{
#pragma warning disable CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'
		private void** lpVtbl;
#pragma warning restore CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT GetQuotaUI(IStorageProviderQuotaUI** result)
		{
			return (HRESULT)((delegate* unmanaged[MemberFunction]<IStorageProviderStatusUI*, IStorageProviderQuotaUI**, int>)lpVtbl[14])((IStorageProviderStatusUI*)Unsafe.AsPointer(ref this), result);
		}

		[GuidRVAGen.Guid("D6B6A758-198D-5B80-977F-5FF73DA33118")]
		public static partial ref readonly Guid Guid { get; }
	}
}
