// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT
{
	public unsafe partial struct IStorageProviderStatusUISource : IComIID
	{
#pragma warning disable CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'
		private void** lpVtbl;
#pragma warning restore CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT GetStatusUI(IStorageProviderStatusUI** result)
		{
			return (HRESULT)((delegate* unmanaged[MemberFunction]<IStorageProviderStatusUISource*, IStorageProviderStatusUI**, int>)lpVtbl[6])((IStorageProviderStatusUISource*)Unsafe.AsPointer(ref this), result);
		}

		[GuidRVAGen.Guid("A306C249-3D66-5E70-9007-E43DF96051FF")]
		public static partial ref readonly Guid Guid { get; }
	}
}
