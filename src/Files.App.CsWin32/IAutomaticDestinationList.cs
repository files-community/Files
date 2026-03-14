// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Attributes;
using System;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.UI.Shell.PropertiesSystem;

namespace Windows.Win32.System.Com;

public unsafe partial struct IAutomaticDestinationList2 : IComIID
{
	[GeneratedVTableFunction(Index = 3)]
	public partial HRESULT Initialize(PCWSTR szAppId, PCWSTR exePath, PCWSTR unknown);

	[GeneratedVTableFunction(Index = 4)]
	public partial HRESULT HasList(BOOL* hasList);

	[GeneratedVTableFunction(Index = 5)]
	public partial HRESULT GetList(DESTLISTTYPE type, int count, GETDESTLISTFLAGS flags, Guid* riid, void** ppv);

	[GeneratedVTableFunction(Index = 6)]
	public partial HRESULT AddUsagePoint(IUnknown* punk);

	[GeneratedVTableFunction(Index = 7)]
	public partial HRESULT PinItem(IUnknown* punk, int pinState);

	[GeneratedVTableFunction(Index = 8)]
	public partial HRESULT GetPinIndex(IUnknown* punk, int* index);

	[GeneratedVTableFunction(Index = 9)]
	public partial HRESULT RemoveDestination(IUnknown* punk);

	[GeneratedVTableFunction(Index = 10)]
	public partial HRESULT SetUsageData(IUnknown* punk, float* usagePoints, long* lastAccessTime);

	[GeneratedVTableFunction(Index = 11)]
	public partial HRESULT GetUsageData(IUnknown* punk, float* usagePoints, long* lastAccessTime);

	[GeneratedVTableFunction(Index = 12)]
	public partial HRESULT ResolveDestination(HWND hwnd, uint flags, IUnknown* shellItem, Guid* riid, void** ppv);

	[GeneratedVTableFunction(Index = 13)]
	public partial HRESULT ClearList(BOOL unknown);

	[GeneratedVTableFunction(Index = 14)]
	public partial HRESULT AddUsagePointsEx(IUnknown* punk, BOOL createDestinationItem, int action);

	[GeneratedVTableFunction(Index = 15)]
	public partial HRESULT BlockItem(IUnknown* punk);

	[GeneratedVTableFunction(Index = 16)]
	public partial HRESULT ClearBlocked();

	[GeneratedVTableFunction(Index = 17)]
	public partial HRESULT TransferPoints(void* from, void* to);

	[GeneratedVTableFunction(Index = 18)]
	public partial HRESULT HasListEx(int* hasList, int* unknown);

	[GeneratedVTableFunction(Index = 19)]
	public partial HRESULT SetDataInternal(void* punk, float* usagePoints, FILETIME* lastAccessTime, int unknown);

	[GeneratedVTableFunction(Index = 20)]
	public partial HRESULT GetDataInternal(void* punk, int matchTarget, float* usagePoints, FILETIME* lastAccessTime, uint* unknownOut1, int* indexOrUnknownOut2);

	[GeneratedVTableFunction(Index = 21)]
	public partial HRESULT UpdateRenamedItems(void* oldItems, void* newItems, int* updatedCount);

	[GeneratedVTableFunction(Index = 22)]
	public partial HRESULT RemoveDeletedItems(void* deletedItems, int* removedCount);

	[GeneratedVTableFunction(Index = 23)]
	public partial HRESULT AddUsagePointsForFolders(void* folders, int action);

	[GeneratedVTableFunction(Index = 24)]
	public partial HRESULT UpdateCachedItems(void* items, int* updatedCount);

	[GeneratedVTableFunction(Index = 25)]
	public partial HRESULT TryAddUsagePointsIfExists(void* punk, int* updated);

	[GeneratedVTableFunction(Index = 26)]
	public partial HRESULT AddFileUsagePoints(void* punk, int createDestinationItem, uint actionOrFlags);

	[GuidRVAGen.Guid("8DC24A1A-6314-4769-9D68-179786F4CED6")]
	public static partial ref readonly Guid Guid { get; }
}

public unsafe partial struct IAutomaticDestinationListPropertyStore : IComIID
{
	[GeneratedVTableFunction(Index = 3)]
	public partial HRESULT GetPropertyStorageForItem(IUnknown* item, IPropertyStore** propertyStore);

	[GeneratedVTableFunction(Index = 4)]
	public partial HRESULT SetPropertyStorageForItem(IUnknown* item, IPropertyStore* propertyStore);

	[GeneratedVTableFunction(Index = 5)]
	public partial HRESULT GetPropertyForItem(IUnknown* item, PCWSTR propertyName, PROPVARIANT* value);

	[GeneratedVTableFunction(Index = 6)]
	public partial HRESULT SetPropertyForItem(IUnknown* item, PCWSTR propertyName, PROPVARIANT* value);

	[GeneratedVTableFunction(Index = 7)]
	public partial HRESULT GetPropertyStorageForList(IPropertyStore** propertyStore);

	[GeneratedVTableFunction(Index = 8)]
	public partial HRESULT SetPropertyStorageForList(IPropertyStore* propertyStore);

	[GeneratedVTableFunction(Index = 9)]
	public partial HRESULT GetPropertyForList(PCWSTR propertyName, PROPVARIANT** value);

	[GeneratedVTableFunction(Index = 10)]
	public partial HRESULT SetPropertyForList(PCWSTR propertyName, PROPVARIANT* value);

	[GuidRVAGen.Guid("8DC24A1A-6314-4769-9D68-179786F4CED6")]
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
