// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Windows.Win32.System.Com
{
	/// <summary>
	/// Defines unmanaged raw vtable for the <see cref="IInternalCustomDestinationList"/> interface.
	/// </summary>
	/// <remarks>
	/// - <a href="https://github.com/GigabyteProductions/classicshell/blob/HEAD/src/ClassicStartMenu/ClassicStartMenuDLL/JumpLists.cpp"/>
	/// </remarks>
	public unsafe partial struct IInternalCustomDestinationList : IComIID
	{
#pragma warning disable CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'
		private void** lpVtbl;
#pragma warning restore CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT SetMinItems(uint dwMinItems)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IInternalCustomDestinationList*, uint, int>)lpVtbl[3])(
				(IInternalCustomDestinationList*)Unsafe.AsPointer(ref this), dwMinItems);

		/// <summary>
		/// Initializes this instance of <see cref="IInternalCustomDestinationList"/> with the specified Application User Model ID (AMUID).
		/// </summary>
		/// <param name="pszAppID">The Application User Model ID to initialize this instance of <see cref="IInternalCustomDestinationList"/> with.</param>
		/// <returns>Returns <see cref="HRESULT.S_OK"/> if successful, or an error value otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT SetApplicationID(PCWSTR pszAppID)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IInternalCustomDestinationList*, PCWSTR, int>)lpVtbl[4])(
				(IInternalCustomDestinationList*)Unsafe.AsPointer(ref this), pszAppID);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT GetSlotCount(uint* pSlotCount)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IInternalCustomDestinationList*, uint*, int>)lpVtbl[5])(
				(IInternalCustomDestinationList*)Unsafe.AsPointer(ref this), pSlotCount);

		/// <summary>
		/// Gets the number of categories in the custom destination list.
		/// </summary>
		/// <param name="pdwCategoryCount">A pointer that points to a valid <see langword="uint"/> var.</param>
		/// <returns>Returns <see cref="HRESULT.S_OK"/> if successful, or an error value otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT GetCategoryCount(uint* pCategoryCount)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IInternalCustomDestinationList*, uint*, int>)lpVtbl[6])(
				(IInternalCustomDestinationList*)Unsafe.AsPointer(ref this), pCategoryCount);

		/// <summary>
		/// Gets the category at the specified index in the custom destination list.
		/// </summary>
		/// <param name="index">The index to get the category in the custom destination list at.</param>
		/// <param name="flags">The flags to filter up the queried destinations.</param>
		/// <param name="pCategory">A pointer that points to a valid <see cref="APPDESTCATEGORY"/> var.</param>
		/// <returns>Returns <see cref="HRESULT.S_OK"/> if successful, or an error value otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT GetCategory(uint index, GETCATFLAG flags, APPDESTCATEGORY* pCategory)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IInternalCustomDestinationList*, uint, GETCATFLAG, APPDESTCATEGORY*, int>)lpVtbl[7])(
				(IInternalCustomDestinationList*)Unsafe.AsPointer(ref this), index, flags, pCategory);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT DeleteCategory(uint index, BOOL deletePermanently)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IInternalCustomDestinationList*, uint, BOOL, int>)lpVtbl[8])(
				(IInternalCustomDestinationList*)Unsafe.AsPointer(ref this), index, deletePermanently);

		/// <summary>
		/// Enumerates the destinations at the specific index in the categories in the custom destinations.
		/// </summary>
		/// <param name="index">The index to get the destinations at in the categories.</param>
		/// <param name="riid">A reference to the interface identifier (IID) of the interface being queried for.</param>
		/// <param name="ppvObject">The address of a pointer to an interface with the IID specified in the riid parameter.</param>
		/// <returns>Returns <see cref="HRESULT.S_OK"/> if successful, or an error value otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT EnumerateCategoryDestinations(uint index, Guid* riid, void** ppvObject)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IInternalCustomDestinationList*, uint, Guid*, void**, int>)lpVtbl[9])(
				(IInternalCustomDestinationList*)Unsafe.AsPointer(ref this), index, riid, ppvObject);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT RemoveDestination(IUnknown* pUnk)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IInternalCustomDestinationList*, IUnknown*, int>)lpVtbl[10])
			((IInternalCustomDestinationList*)Unsafe.AsPointer(ref this), pUnk);

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public HRESULT ResolveDestination(...)
		//	=> (HRESULT)((delegate* unmanaged[MemberFunction]<IInternalCustomDestinationList*, ..., int>)lpVtbl[11])
		//	((IInternalCustomDestinationList*)Unsafe.AsPointer(ref this), ...);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT HasListEx(int* a1, int* a2)
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IInternalCustomDestinationList*, int*, int*, int>)lpVtbl[12])
			((IInternalCustomDestinationList*)Unsafe.AsPointer(ref this), a1, a2);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public HRESULT ClearRemovedDestinations()
			=> (HRESULT)((delegate* unmanaged[MemberFunction]<IInternalCustomDestinationList*, int>)lpVtbl[13])
			((IInternalCustomDestinationList*)Unsafe.AsPointer(ref this));

		static ref readonly Guid IComIID.Guid => throw new NotImplementedException();
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct APPDESTCATEGORY
	{
		[StructLayout(LayoutKind.Explicit)]
		public struct _Anonymous_e__Union
		{
			[FieldOffset(0)]
			public PWSTR Name;

			[FieldOffset(0)]
			public int SubType;
		}

		public APPDESTCATEGORYTYPE Type;

		public _Anonymous_e__Union Anonymous;

		public int Count;

		public fixed int Padding[10];
	}

	/// <summary>
	/// Defines constants that specify category enumeration behavior.
	/// </summary>
	public enum GETCATFLAG : uint
	{
		/// <summary>
		/// The default behavior. Only this value is currently valid.
		/// </summary>
		DEFAULT = 1,
	}

	public enum APPDESTCATEGORYTYPE : uint
	{
		CUSTOM = 0,
		KNOWN = 1,
		TASKS = 2,
	}
}
