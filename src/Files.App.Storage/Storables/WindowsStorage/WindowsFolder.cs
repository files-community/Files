// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	[DebuggerDisplay("{" + nameof(ToString) + "()")]
	public sealed class WindowsFolder : WindowsStorable, IChildFolder, IDisposable
	{
		public WindowsFolder(ComPtr<IShellItem> nativeObject)
		{
			ThisPtr = nativeObject;
		}

		public unsafe WindowsFolder(IShellItem* nativeObject)
		{
			ComPtr<IShellItem> ptr = default;
			ptr.Attach(nativeObject);
			ThisPtr = ptr;
		}

		public unsafe WindowsFolder(Guid folderId)
		{
			Guid folderIdLocal = folderId;
			Guid IID_IShellItem = IShellItem.IID_Guid;
			ComPtr<IShellItem> pItem = default;

			HRESULT hr = PInvoke.SHGetKnownFolderItem(&folderIdLocal, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, HANDLE.Null, &IID_IShellItem, (void**)pItem.GetAddressOf());
			if (hr.Succeeded)
			{
				ThisPtr = pItem;
				return;
			}

			fixed (char* pszShellPath = $"Shell:::{folderId:B}")
				hr = PInvoke.SHCreateItemFromParsingName(pszShellPath, null, &IID_IShellItem, (void**)pItem.GetAddressOf());
			if (hr.Succeeded)
			{
				ThisPtr = pItem;
				return;
			}
		}

		public async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			using ComPtr<IEnumShellItems> pEnumShellItems = GetEnumShellItems();
			while (GetNext(pEnumShellItems) is { } pShellItem && !pShellItem.IsNull)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var pShellFolder = pShellItem.As<IShellFolder>();
				var isFolder = IsFolder(pShellItem);

				if (type is StorableType.File && !isFolder)
				{
					yield return new WindowsFile(pShellItem);
				}
				else if (type is StorableType.Folder && isFolder)
				{
					yield return new WindowsFile(pShellItem);
				}
				else
				{
					continue;
				}

				await Task.Yield();
			}

			unsafe ComPtr<IEnumShellItems> GetEnumShellItems()
			{
				ComPtr<IEnumShellItems> pEnumShellItems = default;
				Guid IID_IEnumShellItems = typeof(IEnumShellItems).GUID;
				Guid BHID_EnumItems = PInvoke.BHID_EnumItems;
				HRESULT hr = ThisPtr.Get()->BindToHandler(null, &BHID_EnumItems, &IID_IEnumShellItems, (void**)pEnumShellItems.GetAddressOf());
				return pEnumShellItems;
			}

			unsafe ComPtr<IShellItem> GetNext(ComPtr<IEnumShellItems> pEnumShellItems)
			{
				ComPtr<IShellItem> pShellItem = default;
				HRESULT hr = pEnumShellItems.Get()->Next(1, pShellItem.GetAddressOf());
				return pShellItem;
			}

			unsafe bool IsFolder(ComPtr<IShellItem> pShellItem)
			{
				return pShellItem.Get()->GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var specifiedAttribute).Succeeded &&
					specifiedAttribute is SFGAO_FLAGS.SFGAO_FOLDER;
			}
		}

		// Disposer

		/// <inheritdoc/>
		public void Dispose()
		{
			ThisPtr.Dispose();
		}
	}
}
