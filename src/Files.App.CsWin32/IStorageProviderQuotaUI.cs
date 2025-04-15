// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT
{
	public unsafe partial struct IStorageProviderQuotaUI : IComIID
	{
#pragma warning disable CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'
		private void** lpVtbl;
#pragma warning restore CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT GetQuotaTotalInBytes(ulong* value)
		{
			return (HRESULT)((delegate* unmanaged[MemberFunction]<IStorageProviderQuotaUI*, ulong*, int>)(lpVtbl[6]))((IStorageProviderQuotaUI*)Unsafe.AsPointer(ref this), value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT GetQuotaUsedInBytes(ulong* value)
		{
			return (HRESULT)((delegate* unmanaged[MemberFunction]<IStorageProviderQuotaUI*, ulong*, int>)(lpVtbl[8]))((IStorageProviderQuotaUI*)Unsafe.AsPointer(ref this), value);
		}

		[GuidRVAGen.Guid("BA6295C3-312E-544F-9FD5-1F81B21F3649")]
		public static partial ref readonly Guid Guid { get; }
	}
}
