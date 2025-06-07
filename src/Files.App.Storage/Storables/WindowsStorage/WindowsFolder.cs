// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	[DebuggerDisplay("{" + nameof(ToString) + "()}")]
	public unsafe class WindowsFolder : WindowsStorable, IChildFolder
	{
		internal IContextMenu* NewMenuPtr
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		}

		internal IContextMenu* ContextMenuPtr
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		}

		public WindowsFolder(IShellItem* ptr)
		{
			ThisPtr = ptr;
		}

		public WindowsFolder(Guid folderId)
		{
			IShellItem* pItem = default;

			HRESULT hr = PInvoke.SHGetKnownFolderItem(&folderId, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, HANDLE.Null, IID.IID_IShellItem, (void**)&pItem);
			if (hr.Failed)
			{
				fixed (char* pszShellPath = $"Shell:::{folderId:B}")
					hr = PInvoke.SHCreateItemFromParsingName(pszShellPath, null, IID.IID_IShellItem, (void**)&pItem);

				// Invalid FOLDERID; this should never happen.
				hr.ThrowOnFailure();
			}

			ThisPtr = pItem;
		}

		public IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, CancellationToken cancellationToken = default)
		{
			ComPtr<IEnumShellItems> pEnumShellItems = default;
			HRESULT hr = ThisPtr->BindToHandler(null, BHID.BHID_EnumItems, IID.IID_IEnumShellItems, (void**)pEnumShellItems.GetAddressOf());
			if (hr.ThrowIfFailedOnDebug().Failed)
				return Enumerable.Empty<IStorableChild>().ToAsyncEnumerable();

			List<IStorableChild> items = [];
			IShellItem* pShellItem = default;
			while (pEnumShellItems.Get()->Next(1, &pShellItem).Succeeded && pShellItem is not null)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var isFolder = pShellItem->GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var returnedAttributes).Succeeded &&
					returnedAttributes == SFGAO_FLAGS.SFGAO_FOLDER;

				if (type is StorableType.File && !isFolder)
				{
					items.Add(new WindowsFile(pShellItem));
				}
				else if (type is StorableType.Folder && isFolder)
				{
					items.Add(new WindowsFolder(pShellItem));
				}
				else
				{
					continue;
				}
			}

			return items.ToAsyncEnumerable();
		}
	}
}
