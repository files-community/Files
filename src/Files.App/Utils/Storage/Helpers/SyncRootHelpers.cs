// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32;
using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.System.WinRT;
using WinRT;

namespace Files.App.Utils.Storage
{
	internal static class SyncRootHelpers
	{
		private static unsafe (bool Success, ulong Capacity, ulong Used) GetSyncRootQuotaFromSyncRootId(string syncRootId)
		{
			using var key = Registry.LocalMachine.OpenSubKey($"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\SyncRootManager\\{syncRootId}");
			if (key?.GetValue("StorageProviderStatusUISourceFactory") is not string factoryClsidString ||
				!Guid.TryParse(factoryClsidString, out var factoryClsid))
				return (false, 0, 0);

			ulong ulTotalSize = 0ul, ulUsedSize = 0ul;
			using ComPtr<IStorageProviderStatusUISourceFactory> pStorageProviderStatusUISourceFactory = default;
			using ComPtr<IStorageProviderStatusUISource> pStorageProviderStatusUISource = default;
			using ComPtr<IStorageProviderStatusUI> pStorageProviderStatusUI = default;
			using ComPtr<IStorageProviderQuotaUI> pStorageProviderQuotaUI = default;

			var hr = PInvoke.CoCreateInstance(
				&factoryClsid,
				null,
				CLSCTX.CLSCTX_LOCAL_SERVER,
				(Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IStorageProviderStatusUISourceFactory.Guid)),
				(void**)pStorageProviderStatusUISourceFactory.GetAddressOf());
			if (hr.Failed)
				return (false, 0, 0);

			var syncRootIdHString = new MarshalString.Pinnable(syncRootId);
			fixed (char* pSyncRootIdHString = syncRootIdHString)
			{
				hr = pStorageProviderStatusUISourceFactory.Get()->GetStatusUISource(syncRootIdHString.GetAbi(), pStorageProviderStatusUISource.GetAddressOf()).ThrowOnFailure();
				hr = pStorageProviderStatusUISource.Get()->GetStatusUI(pStorageProviderStatusUI.GetAddressOf()).ThrowOnFailure();
				hr = pStorageProviderStatusUI.Get()->GetQuotaUI(pStorageProviderQuotaUI.GetAddressOf()).ThrowOnFailure();

				hr = pStorageProviderQuotaUI.Get()->GetQuotaTotalInBytes(&ulTotalSize);
				hr = pStorageProviderQuotaUI.Get()->GetQuotaUsedInBytes(&ulUsedSize);
			}

			return (true, ulTotalSize, ulUsedSize);
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

			return GetSyncRootQuotaFromSyncRootId(syncRootInfo.Id);
		}
	}
}
