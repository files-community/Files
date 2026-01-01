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

		/// <summary>
		/// Initializes this instance of <see cref="IAutomaticDestinationList"/> with the specified Application User Model ID (AMUID).
		/// </summary>
		/// <param name="szAppId">The Application User Model ID to initialize this instance of <see cref="IAutomaticDestinationList"/> with.</param>
		/// <param name="a2">Unknown argument. Apparently this can be NULL.</param>
		/// <param name="a3">Unknown argument. Apparently this can be NULL.</param>
		/// <returns>Returns <see cref="HRESULT.S_OK"/> if successful, or an error value otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT Initialize(PCWSTR szAppId, PCWSTR a2, PCWSTR a3)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, PCWSTR, PCWSTR, PCWSTR, int>)lpVtbl[3])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), szAppId, a2, a3);

		/// <summary>
		/// Gets a value that determines whether this <see cref="IAutomaticDestinationList"/> has any list.
		/// </summary>
		/// <param name="pfHasList">A pointer to a <see cref="BOOL"/> that receives the result. <see cref="BOOL.TRUE"/> if there's any list; otherwise, <see cref="BOOL.FALSE"/>.</param>
		/// <returns>Returns <see cref="HRESULT.S_OK"/> if successful, or an error value otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT HasList(BOOL* pfHasList)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, BOOL*, int>)lpVtbl[4])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), pfHasList);

		/// <summary>
		/// Gets the list of automatic destinations of the specified type.
		/// </summary>
		/// <param name="type">The type to get the automatic destinations of.</param>
		/// <param name="maxCount">The max count to get the automatic destinations up to.</param>
		/// <param name="flags">The flags to filter up the queried destinations.</param>
		/// <param name="riid">A reference to the interface identifier (IID) of the interface being queried for.</param>
		/// <param name="ppvObject">The address of a pointer to an interface with the IID specified in the riid parameter.</param>
		/// <returns>Returns <see cref="HRESULT.S_OK"/> if successful, or an error value otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT GetList(DESTLISTTYPE type, int maxCount, GETDESTLISTFLAGS flags, Guid* riid, void** ppvObject)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, DESTLISTTYPE, int, GETDESTLISTFLAGS, Guid*, void**, int>)lpVtbl[5])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), type, maxCount, flags, riid, ppvObject);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT AddUsagePoint(IUnknown* pUnk)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, IUnknown*, int>)lpVtbl[6])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), pUnk);

		/// <summary>
		/// Pins an item to the list.
		/// </summary>
		/// <param name="pUnk">The native object to pin to the list.</param>
		/// <param name="index">-1 to pin to the last, -2 to unpin, zero or positive numbers (>= 0) indicate the index to pin to the list at. Passing the other numbers are *UB*.</param>
		/// <returns>Returns <see cref="HRESULT.S_OK"/> if successful, or an error value otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT PinItem(IUnknown* pUnk, int index)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, IUnknown*, int, int>)lpVtbl[7])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), pUnk, index);

		/// <summary>
		/// Gets the index of a pinned item in the Pinned list.
		/// </summary>
		/// <remarks>
		/// According to the debug symbols, this method is called "IsPinned" and other definitions out there also define so
		/// but it is inappropriate based on the fact it actually calls an internal method that gets the index of a pinned item
		/// and returns it in the second argument. If you want to check if an item is pinned, you should use IShellItem::Compare for IShellItem,
		/// or compare IShellLinkW::GetPath, IShellLinkW::GetArguments and PKEY_Title for IShellLinkW, which is actually done, at least, in Windows 7 era.
		/// </remarks>
		/// <param name="punk">The native object to get its index in the list.</param>
		/// <param name="piIndex">A pointer that points to an int value that takes the index of the item passed.</param>
		/// <returns>Returns <see cref="HRESULT.S_OK"/> if successful, or an error value otherwise. If the passed item doesn't belong to the <see cref="DESTLISTTYPE.PINNED"/> list, <see cref="HRESULT.E_NOT_SET"/> is returned.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT GetPinIndex(IUnknown* punk, int* piIndex)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, IUnknown*, int*, int>)lpVtbl[8])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), punk, piIndex);

		/// <summary>
		/// Removes a destination from the automatic destinations list.
		/// </summary>
		/// <param name="psi">The destination to remove from the automatic destinations list.</param>
		/// <returns>Returns <see cref="HRESULT.S_OK"/> if successful, or an error value otherwise.
		public HRESULT RemoveDestination(IUnknown* psi)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, IUnknown*, int>)lpVtbl[9])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), psi);

		public HRESULT SetUsageData(IUnknown* pItem, float* a2, long* pFileTime)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, IUnknown*, float*, long*, int>)lpVtbl[10])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), pItem, a2, pFileTime);

		public HRESULT GetUsageData(IUnknown* pItem, float* a2, long* pFileTime)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, IUnknown*, float*, long*, int>)lpVtbl[11])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), pItem, a2, pFileTime);

		public HRESULT ResolveDestination(HWND hWnd, int a2, IShellItem* pShellItem, Guid* riid, void** ppvObject)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, HWND, int, IShellItem*, Guid*, void**, int>)lpVtbl[12])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), hWnd, a2, pShellItem, riid, ppvObject);

		public HRESULT ClearList(BOOL clearPinsToo)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IAutomaticDestinationList*, BOOL, int>)lpVtbl[13])
				((IAutomaticDestinationList*)Unsafe.AsPointer(ref this), clearPinsToo);

		[GuidRVAGen.Guid("E9C5EF8D-FD41-4F72-BA87-EB03BAD5817C")]
		public static partial ref readonly Guid Guid { get; }

		internal static ref readonly Guid IID_Guid
			=> ref MemoryMarshal.AsRef<Guid>([0xBF, 0xDE, 0x32, 0x63, 0xB5, 0x87, 0x70, 0x46, 0x90, 0xC0, 0x5E, 0x57, 0xB4, 0x08, 0xA4, 0x9E]);

		internal static Guid* IID_Guid2
			=> (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID_Guid));

		static ref readonly Guid IComIID.Guid => ref IID_Guid;
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
