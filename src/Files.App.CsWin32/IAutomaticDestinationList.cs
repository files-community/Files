// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Windows.Win32.System.Com
{
	/// <summary>
	/// Defines unmanaged raw vtable for the <see cref="IAutomaticDestinationList"/> interface.
	/// </summary>
	public unsafe partial struct IAutomaticDestinationList : IComIID
	{
#pragma warning disable CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'
		private void** lpVtbl;
#pragma warning restore CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT Initialize(PCWSTR szAppId, PCWSTR a2, PCWSTR a3)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, PCWSTR, PCWSTR, PCWSTR, int>)lpVtbl[3])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), szAppId, a2, a3);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT HasList(BOOL* pfHasList)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, BOOL*, int>)lpVtbl[4])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), pfHasList);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT GetList(DESTLISTTYPE type, int maxCount, GETDESTLISTFLAGS flags, Guid* riid, void** ppvObject)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, DESTLISTTYPE, int, GETDESTLISTFLAGS, Guid*, void**, int>)lpVtbl[5])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), type, maxCount, flags, riid, ppvObject);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT AddUsagePoint(IUnknown* pUnk)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, IUnknown*, int>)lpVtbl[6])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), pUnk);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT PinItem(IUnknown* pUnk, int index)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, IUnknown*, int, int>)lpVtbl[7])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), pUnk, index);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT GetPinIndex(IUnknown* punk, int* piIndex)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, IUnknown*, int*, int>)lpVtbl[8])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), punk, piIndex);

		public HRESULT RemoveDestination(IUnknown* psi)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, IUnknown*, int>)lpVtbl[9])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), psi);

		public HRESULT SetUsageData(IUnknown* pItem, float* accessCount, long* pFileTime)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, IUnknown*, float*, long*, int>)lpVtbl[10])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), pItem, accessCount, pFileTime);

		public HRESULT GetUsageData(IUnknown* pItem, float* accessCount, long* pFileTime)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, IUnknown*, float*, long*, int>)lpVtbl[11])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), pItem, accessCount, pFileTime);

		public HRESULT ResolveDestination(HWND hWnd, int a2, IShellItem* pShellItem, Guid* riid, void** ppvObject)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, HWND, int, IShellItem*, Guid*, void**, int>)lpVtbl[12])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), hWnd, a2, pShellItem, riid, ppvObject);

		public HRESULT ClearList(BOOL clearPinsToo)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, BOOL, int>)lpVtbl[13])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), clearPinsToo);

		[GuidRVAGen.Guid("E9C5EF8D-FD41-4F72-BA87-EB03BAD5817C")]
		public static partial ref readonly Guid Guid { get; }
	}

	public enum DESTLISTTYPE : uint
	{
		PINNED,
		RECENT,
		FREQUENT,
	}

	public enum GETDESTLISTFLAGS : uint
	{
		NONE,
		EXCLUDE_UNNAMED_DESTINATIONS,
	}
}
