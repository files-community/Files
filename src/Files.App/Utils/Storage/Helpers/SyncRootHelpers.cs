using Microsoft.Win32;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Storage.Provider;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinRT;

namespace Files.App.Utils.Storage
{
	internal static class SyncRootHelpers
	{
		private unsafe struct IStorageProviderStatusUISourceFactory : IComIID
		{
			private void** vtbl;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public HRESULT GetStatusUISource(nint syncRootId, IStorageProviderStatusUISource** result)
			{
				return ((delegate* unmanaged[Stdcall]<IStorageProviderStatusUISourceFactory*, nint, IStorageProviderStatusUISource**, HRESULT>)vtbl[6])((IStorageProviderStatusUISourceFactory*)Unsafe.AsPointer(ref this), syncRootId, result);
			}

			public static ref readonly Guid Guid
			{
				get
				{
					// 12e46b74-4e5a-58d1-a62f-0376e8ee7dd8
					ReadOnlySpan<byte> data = new byte[]
					{
						0x74, 0x6b, 0xe4, 0x12,
						0x5a, 0x4e,
						0xd1, 0x58,
						0xa6, 0x2f,
						0x03, 0x76, 0xe8, 0xee, 0x7d, 0xd8
					};
					Debug.Assert(data.Length == sizeof(Guid));
					return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
				}
			}
		}

		private unsafe struct IStorageProviderStatusUISource : IComIID
		{
			private void** vtbl;

			public HRESULT GetStatusUI(IStorageProviderStatusUI** result)
			{
				return ((delegate* unmanaged[Stdcall]<IStorageProviderStatusUISource*, IStorageProviderStatusUI**, HRESULT>)vtbl[6])((IStorageProviderStatusUISource*)Unsafe.AsPointer(ref this), result);
			}

			public static ref readonly Guid Guid
			{
				get
				{
					// a306c249-3d66-5e70-9007-e43df96051ff
					ReadOnlySpan<byte> data = new byte[]
					{
						0x49, 0xc2, 0x06, 0xa3,
						0x66, 0x3d,
						0x70, 0x5e,
						0x90, 0x07,
						0xe4, 0x3d, 0xf9, 0x60, 0x51, 0xff
					};
					Debug.Assert(data.Length == sizeof(Guid));
					return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
				}
			}
		}

		private unsafe struct IStorageProviderStatusUI : IComIID
		{
			public static ref readonly Guid Guid
			{
				get
				{
					// d6b6a758-198d-5b80-977f-5ff73da33118
					ReadOnlySpan<byte> data = new byte[]
					{
						0x58, 0xa7, 0xb6, 0xd6,
						0x8d, 0x19,
						0x80, 0x5b,
						0x97, 0x7f,
						0x5f, 0xf7, 0x3d, 0xa3, 0x31, 0x18
					};
					Debug.Assert(data.Length == sizeof(Guid));
					return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
				}
			}
		}

		private static unsafe (bool Success, ulong Capacity, ulong Used) GetSyncRootQuotaFromSyncRootId(string syncRootId)
		{
			RegistryKey? key;
			if ((key = Registry.LocalMachine.OpenSubKey($"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\SyncRootManager\\{syncRootId}")) is null)
			{
				return (false, 0, 0);
			}

			using (key)
			{
				if (key.GetValue("StorageProviderStatusUISourceFactory") is string statusUIclass)
				{
					StorageProviderStatusUI statusUI;
					using (ComPtr<IStorageProviderStatusUISourceFactory> sourceFactoryNative = default)
					{
						Guid statusUIclassGuid = Guid.Parse(statusUIclass);
						if (PInvoke.CoCreateInstance(&statusUIclassGuid, null, Windows.Win32.System.Com.CLSCTX.CLSCTX_LOCAL_SERVER, (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IStorageProviderStatusUISourceFactory.Guid)), (void**)sourceFactoryNative.GetAddressOf()) != 0)
						{
							return (false, 0, 0);
						}

						MarshalString.Pinnable syncRootIdHstring = new(syncRootId);
						fixed (char* ptr = syncRootIdHstring)
							using (ComPtr<IStorageProviderStatusUISource> sourceNative = default)
							{
								ExceptionHelpers.ThrowExceptionForHR(sourceFactoryNative.Get()->GetStatusUISource(syncRootIdHstring.GetAbi(), sourceNative.GetAddressOf()));

								using (ComPtr<IStorageProviderStatusUI> statusNative = default)
								{
									ExceptionHelpers.ThrowExceptionForHR(sourceNative.Get()->GetStatusUI(statusNative.GetAddressOf()));
									statusUI = StorageProviderStatusUI.FromAbi((nint)statusNative.Get());
								}
							}
					}
					return (true, statusUI.QuotaUI.QuotaTotalInBytes, statusUI.QuotaUI.QuotaUsedInBytes);
				}
				else
				{
					return (false, 0, 0);
				}
			}
		}

		public static async Task<(bool Success, ulong Capacity, ulong Used)> GetSyncRootQuotaAsync(string path)
		{
			Windows.Storage.StorageFolder folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(path);
			StorageProviderSyncRootInfo? syncRootInfo = null;

			try
			{
				syncRootInfo = StorageProviderSyncRootManager.GetSyncRootInformationForFolder(folder);
			}
			catch
			{
				return (false, 0, 0);
			}

			return GetSyncRootQuotaFromSyncRootId(syncRootInfo.Id);
		}
	}
}
