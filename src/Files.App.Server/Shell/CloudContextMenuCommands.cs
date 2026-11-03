// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Storage.Interop;
using Microsoft.Extensions.Logging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Files.App.Server;

internal static class CloudContextMenuCommands
{
	private static readonly Guid SyncRootManagerClass = new("F324E4F9-8496-40B2-A1FF-9617C1C9AFFE");

	internal static IReadOnlyList<ExplorerCommandRegistration> GetCommands(IEnumerable<string> paths)
	{
		try
		{
			if (PInvoke.CoCreateInstance(SyncRootManagerClass, null, CLSCTX.CLSCTX_INPROC_SERVER, out ISyncRootManager manager).Failed)
				return [];

			IStorageProviderInfo? provider = null;
			foreach (var path in paths)
			{
				var hr = manager.GetStorageProviderInfoFromPath(path, out var candidate, out _, out _);
				if (hr.Failed || candidate is null)
					return [];
				if (provider is null)
					provider = candidate;
				else if (provider.IsSameIdentifier(candidate) != HRESULT.S_OK)
					return [];
			}

			if (provider is null)
				return [];

			string? name = null;
			string? packageFullName = null;
			if (provider is IStorageProviderAppInfo appInfo)
			{
				appInfo.GetAppDisplayName(out name);
				appInfo.GetPackageFullName(out packageFullName).ThrowOnFailure();
			}

			string groupId;
			if (!string.IsNullOrEmpty(packageFullName))
			{
				groupId = $"package:{packageFullName}";
			}
			else
			{
				if (provider.GetFullProviderAndUserAndAccountIdentifier(out var providerId).Failed || string.IsNullOrEmpty(providerId))
					return [];
				groupId = $"provider:{providerId}";
			}

			if (string.IsNullOrWhiteSpace(name) || name.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase))
				provider.GetDisplayName(out name);
			if (string.IsNullOrWhiteSpace(name))
				return [];

			string? icon = null;
			if (provider.GetIcon(out var iconPath, out var iconIndex).Succeeded && !string.IsNullOrEmpty(iconPath))
				icon = FormattableString.Invariant($"{iconPath},{iconIndex}");

			if (provider.GetContextMenuVerbs(out _, out var classIds, out _).Failed || classIds is null)
				return [];

			return classIds.Select(classId => new ExplorerCommandRegistration(classId, classId.ToString("B"), name, groupId, IsCloudProvider: true, GroupIcon: icon)).ToList();
		}
		catch (Exception ex)
		{
			Program.Logger.LogDebug(ex, "Could not read cloud-provider context menu registrations.");
			return [];
		}
	}
}
