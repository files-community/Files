// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;

namespace Files.Storage.Interop;

// Undocumented Shell contracts. Preserve the verified method order and query these IIDs before using them.
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper, StringMarshalling = StringMarshalling.Utf16)]
[Guid("692D40A4-EFA1-4089-88F8-15FD6F5F8B64")]
public partial interface ISyncRootManager
{
	[PreserveSig]
	HRESULT GetStorageProviderInfo(string identifier, out IStorageProviderInfo? info);
	[PreserveSig]
	HRESULT GetStorageProviderInfoFromPath(string path, out IStorageProviderInfo? info, out string? resolvedPath, out uint comparisonFlags);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper, StringMarshalling = StringMarshalling.Utf16)]
[Guid("CA01C124-2769-4576-BF12-8A54EE671A86")]
public partial interface IStorageProviderInfo
{
	[PreserveSig] HRESULT GetFullProviderAndUserAndAccountIdentifier(out string? identifier);
	[PreserveSig] HRESULT SetFullProviderAndUserAndAccountIdentifier(string identifier);
	[PreserveSig] HRESULT GetProviderIdentifier(out string? identifier);
	[PreserveSig] HRESULT IsSameIdentifier(IStorageProviderInfo other);
	[PreserveSig] HRESULT GetDisplayName(out string? displayName);
	[PreserveSig] HRESULT GetDisplayNameResource(out string? resource);
	[PreserveSig] HRESULT SetDisplayNameResource(string resource);
	[PreserveSig] HRESULT GetIcon(out string? path, out int index);
	[PreserveSig] HRESULT GetIconResource(out string? resource);
	[PreserveSig] HRESULT SetIconResource(string resource);
	[PreserveSig] HRESULT SetHandlerClsid(in Guid classId);
	[PreserveSig] HRESULT GetHandler(in Guid classId, out nint handler);
	[PreserveSig] HRESULT SetBannerHandlerClsid(in Guid classId);
	[PreserveSig] HRESULT GetBannerHandler(in Guid classId, out nint handler);
	[PreserveSig] HRESULT SetCustomStateHandlerClsid(in Guid classId);
	[PreserveSig] HRESULT GetCustomStateHandler(in Guid classId, out nint handler);
	[PreserveSig] HRESULT SetThumbnailProviderClsid(in Guid classId);
	[PreserveSig] HRESULT GetThumbnailProvider(string path, in Guid classId, out nint handler);
	[PreserveSig] HRESULT GetFlags(out uint flags);
	[PreserveSig] HRESULT SetFlags(uint flags, uint mask);
	[PreserveSig] HRESULT GetCustomStateIdList([MarshalUsing(CountElementName = nameof(count))] out uint[]? stateIds, out uint count);
	[PreserveSig] HRESULT GetCustomStateDisplayName(uint stateId, out string? name);
	[PreserveSig] HRESULT GetProtectionMode(out string? mode);
	[PreserveSig] HRESULT SetProtectionMode(string mode);
	[PreserveSig] HRESULT GetRecycleBinUrl(out string? url);
	[PreserveSig] HRESULT SetRecycleBinUrl(string url);
	[PreserveSig] HRESULT SetExtendedPropertiesHandlerClsid(in Guid classId);
	[PreserveSig] HRESULT GetExtendedPropertiesHandler(in Guid classId, out nint handler);
	[PreserveSig] HRESULT GetContextMenuVerbs(out uint count, [MarshalUsing(CountElementName = nameof(count))] out Guid[]? classIds, out int reserved);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper, StringMarshalling = StringMarshalling.Utf16)]
[Guid("CF745728-CBBE-415E-B530-066B3ED219B6")]
public partial interface IStorageProviderAppInfo
{
	[PreserveSig] HRESULT SetPackageFullName(string name);
	[PreserveSig] HRESULT GetPackageFullName(out string? name);
	[PreserveSig] HRESULT SetLogoPath(string path);
	[PreserveSig] HRESULT GetLogoPath(out string? path);
	[PreserveSig] HRESULT SetAppDisplayName(string name);
	[PreserveSig] HRESULT GetAppDisplayName(out string? name);
}
