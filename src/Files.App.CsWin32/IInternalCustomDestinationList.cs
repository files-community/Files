// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT License.

using Files.Shared.Attributes;
using System;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.Com;

public unsafe partial struct IInternalCustomDestinationList : IComIID
{
	[GeneratedVTableFunction(Index = 3)]
	public partial HRESULT SetMinItems(uint dwMinItems);

	[GeneratedVTableFunction(Index = 4)]
	public partial HRESULT SetApplicationID(PCWSTR pszAppID);

	[GeneratedVTableFunction(Index = 5)]
	public partial HRESULT GetSlotCount(uint* pSlotCount);

	[GeneratedVTableFunction(Index = 6)]
	public partial HRESULT GetCategoryCount(uint* pCategoryCount);

	[GeneratedVTableFunction(Index = 7)]
	public partial HRESULT GetCategory(uint index, GETCATFLAG flags, APPDESTCATEGORY* pCategory);

	[GeneratedVTableFunction(Index = 8)]
	public partial HRESULT DeleteCategory(uint index, BOOL deletePermanently);

	[GeneratedVTableFunction(Index = 9)]
	public partial HRESULT EnumerateCategoryDestinations(uint index, Guid* riid, void** ppvObject);

	[GeneratedVTableFunction(Index = 10)]
	public partial HRESULT RemoveDestination(IUnknown* pUnk);

	[GeneratedVTableFunction(Index = 11)]
	public partial HRESULT HasListEx(int* a1, int* a2);

	[GeneratedVTableFunction(Index = 12)]
	public partial HRESULT ClearRemovedDestinations();

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
