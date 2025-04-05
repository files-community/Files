// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage.Storables
{
	public sealed class WindowsFolder : WindowsStorable, IChildFolder, IDisposable
	{
		public string Id => throw new NotImplementedException();

		public string Name => throw new NotImplementedException();

		public WindowsFolder(ComPtr<IShellItem> nativeObject)
		{
			ThisPtr = nativeObject;
		}

		public unsafe WindowsFolder(Guid folderId)
		{
			var IID_IShellItem = typeof(IShellItem).GUID;

			HRESULT hr = PInvoke.SHGetKnownFolderItem(
				&folderId,
				KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT,
				HANDLE.Null,
				&IID_IShellItem,
				(void**)ThisPtr.GetAddressOf());
		}

		public async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, CancellationToken cancellationToken = default)
		{
			await Task.Yield();

			foreach (var item in GetItems())
				yield return item;

			IEnumerable<IStorableChild> GetItems()
			{
				ComPtr<IEnumShellItems> pEnumShellItems = GetEnumShellItems();
				while (GetNext(pEnumShellItems) is { } pShellItem && !pShellItem.IsNull)
				{
					var pShellFolder = pShellItem.As<IShellFolder>();

					if (type is StorableType.File)
					{
						yield return new WindowsFile(pShellItem);
					}
					else if (type is StorableType.Folder)
					{
						yield return new WindowsFile(pShellItem);
					}
					else
					{
						yield return pShellFolder.IsNull
							? new WindowsFile(pShellItem)
							: new WindowsFolder(pShellItem);
					}
				}
			}

			unsafe ComPtr<IEnumShellItems> GetEnumShellItems()
			{
				using ComPtr<IEnumShellItems> pEnumShellItems = default;
				Guid IID_IEnumShellItems = typeof(IEnumShellItems).GUID;
				var BHID_EnumItems = PInvoke.BHID_EnumItems;
				HRESULT hr = ThisPtr.Get()->BindToHandler(null, &BHID_EnumItems, &IID_IEnumShellItems, (void**)pEnumShellItems.GetAddressOf());
				return pEnumShellItems;
			}

			unsafe ComPtr<IShellItem> GetNext(ComPtr<IEnumShellItems> pEnumShellItems)
			{
				ComPtr<IShellItem> pShellItem = default;
				pEnumShellItems.Get()->Next(1, pShellItem.GetAddressOf());
				return pShellItem;
			}
		}

		public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}

		// Disposer

		public void Dispose()
		{
			ThisPtr.Dispose();
		}
	}
}
