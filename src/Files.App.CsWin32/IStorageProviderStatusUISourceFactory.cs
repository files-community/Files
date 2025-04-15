// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT
{
	public unsafe partial struct IStorageProviderStatusUISourceFactory : IComIID
	{
#pragma warning disable CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'
		private void** lpVtbl;
#pragma warning restore CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT GetStatusUISource(nint syncRootId, IStorageProviderStatusUISource** result)
		{
			return (HRESULT)((delegate* unmanaged[MemberFunction]<IStorageProviderStatusUISourceFactory*, nint, IStorageProviderStatusUISource**, int>)lpVtbl[6])((IStorageProviderStatusUISourceFactory*)Unsafe.AsPointer(ref this), syncRootId, result);
		}

		[GuidRVAGen.Guid("12E46B74-4E5A-58D1-A62F-0376E8EE7DD8")]
		public static partial ref readonly Guid Guid { get; }
	}
}
