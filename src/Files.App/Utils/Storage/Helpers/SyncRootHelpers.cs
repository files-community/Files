// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.System.WinRT;

namespace Files.App.Utils.Storage
{
	internal static class SyncRootHelpers
	{
		private static (bool Success, ulong Capacity, ulong Used) GetSyncRootQuotaFromSyncRootId(string syncRootId)
		{
			using var key = Registry.LocalMachine.OpenSubKey($"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\SyncRootManager\\{syncRootId}");
			if (key?.GetValue("StorageProviderStatusUISourceFactory") is not string factoryClsidString ||
				!Guid.TryParse(factoryClsidString, out var factoryClsid))
				return (false, 0, 0);

			ulong totalSize = 0ul, usedSize = 0ul;

			if (PInvoke.CoCreateInstance(
				factoryClsid,
				null,
				CLSCTX.CLSCTX_LOCAL_SERVER,
				out IStorageProviderStatusUISourceFactory statusUISourceFactory).ThrowIfFailedOnDebug().Failed)
				return (false, 0, 0);

			if (statusUISourceFactory.GetStatusUISource(syncRootId, out var statusUISource).ThrowIfFailedOnDebug().Failed ||
				statusUISource.GetStatusUI(out var statusUI).ThrowIfFailedOnDebug().Failed ||
				statusUI.get_QuotaUI(out var quotaUI).ThrowIfFailedOnDebug().Failed ||
				quotaUI.get_QuotaTotalInBytes(out totalSize).ThrowIfFailedOnDebug().Failed ||
				quotaUI.get_QuotaUsedInBytes(out usedSize).ThrowIfFailedOnDebug().Failed)
				return (false, 0, 0);

			return (true, totalSize, usedSize);
		}

		public static async Task<(bool Success, ulong Capacity, ulong Used)> GetSyncRootQuotaAsync(string path)
		{
			Windows.Storage.StorageFolder folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(path);
			Windows.Storage.Provider.StorageProviderSyncRootInfo? syncRootInfo = null;

			try
			{
				syncRootInfo = Windows.Storage.Provider.StorageProviderSyncRootManager.GetSyncRootInformationForFolder(folder);
			}
			catch
			{
				return (false, 0, 0);
			}

			if (syncRootInfo is null || syncRootInfo.Id is null)
			{
				return (false, 0, 0);
			}

			return GetSyncRootQuotaFromSyncRootId(syncRootInfo.Id);
		}
	}
}
