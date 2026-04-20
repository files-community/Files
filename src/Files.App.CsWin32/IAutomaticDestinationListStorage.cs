// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Attributes;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.UI.Shell.PropertiesSystem;

namespace Windows.Win32.System.WinRT;

public unsafe partial struct IAutomaticDestinationListStorage : IComIID
{
	[GeneratedVTableFunction(Index = 6)]
	public partial HRESULT ItemCount(uint* value);

	[GeneratedVTableFunction(Index = 7)]
	public partial HRESULT PinnedItemCount(uint* value);

	[GeneratedVTableFunction(Index = 8)]
	public partial HRESULT AddItem(nint storageItem, bool flag, int* index);

	[GeneratedVTableFunction(Index = 9)]
	public partial HRESULT RemoveItem(nint storageItem);

	[GeneratedVTableFunction(Index = 10)]
	public partial HRESULT PinItem(nint storageItem, int pinPosition);

	[GeneratedVTableFunction(Index = 11)]
	public partial HRESULT UnpinItem(nint storageItem);

	[GeneratedVTableFunction(Index = 12)]
	public partial HRESULT ClearList();

	[GeneratedVTableFunction(Index = 13)]
	public partial HRESULT GetItemAtIndex(uint index, nint* storageItem);

	[GeneratedVTableFunction(Index = 14)]
	public partial HRESULT AddUsagePointsForItem(nint storageItem, double usagePoints, bool flag);

	[GeneratedVTableFunction(Index = 15)]
	public partial HRESULT GetInfoForItem(void* storageItem, IAutomaticDestinationListItemInfo** itemInfo);

	[GeneratedVTableFunction(Index = 16)]
	public partial HRESULT UpdateInfoForItem(void* storageItem, IAutomaticDestinationListItemInfo* itemInfo);

	[GeneratedVTableFunction(Index = 17)]
	public partial HRESULT Save();

	[GeneratedVTableFunction(Index = 18)]
	public partial HRESULT Load(int access, HSTRING appFullPath, HSTRING appId, HSTRING customAutoDestFullFilePath);

	[GeneratedVTableFunction(Index = 19)]
	public partial HRESULT get_ExtendedProperties(nint* valueSet);

	[GeneratedVTableFunction(Index = 20)]
	public partial HRESULT put_ExtendedProperties(nint valueSet);

	[GeneratedVTableFunction(Index = 21)]
	public partial HRESULT Close();

	[GuidRVAGen.Guid("4DBD7969-19C5-5F8D-BAA0-0489BD97DE0E")]
	public static partial ref readonly Guid Guid { get; }
}

public unsafe partial struct IAutomaticDestinationListStorage2 : IComIID
{
	[GeneratedVTableFunction(Index = 6)]
	public partial HRESULT GetInfoAtIndex(uint index, void** itemInfo);

	[GeneratedVTableFunction(Index = 7)]
	public partial HRESULT RemoveItemAtIndex(uint index);

	[GeneratedVTableFunction(Index = 8)]
	public partial HRESULT UpdateInfoAtIndex(uint index, IAutomaticDestinationListItemInfo* itemInfo);

	[GuidRVAGen.Guid("06CCF5F7-EC19-5AD3-8553-DF68583123E0")]
	public static partial ref readonly Guid Guid { get; }
}
