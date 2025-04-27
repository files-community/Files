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
	public sealed class WindowsFolder : WindowsStorable, IChildFolder
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
			ComPtr<IShellItem> pItem = default;

			HRESULT hr = PInvoke.SHGetKnownFolderItem(&folderId, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, HANDLE.Null, IID.IID_IShellItem, (void**)pItem.GetAddressOf());
			if (hr.Failed)
			{
				fixed (char* pszShellPath = $"Shell:::{folderId:B}")
					hr = PInvoke.SHCreateItemFromParsingName(pszShellPath, null, IID.IID_IShellItem, (void**)pItem.GetAddressOf());

				// Invalid FOLDERID; this should never happen.
				hr.ThrowOnFailure();
			}

			ThisPtr = pItem;
		}

		public IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, CancellationToken cancellationToken = default)
		{
			return GetItems().ToAsyncEnumerable();

			unsafe IEnumerable<IStorableChild> GetItems()
			{
				ComPtr<IEnumShellItems> pEnumShellItems = default;
				GetEnumerator();

				ComPtr<IShellItem> pShellItem = default;
				while (GetNext() && !pShellItem.IsNull)
				{
					cancellationToken.ThrowIfCancellationRequested();
					var isFolder = pShellItem.HasShellAttributes(SFGAO_FLAGS.SFGAO_FOLDER);

					if (type is StorableType.File && !isFolder)
					{
						yield return new WindowsFile(pShellItem);
					}
					else if (type is StorableType.Folder && isFolder)
					{
						yield return new WindowsFolder(pShellItem);
					}
					else
					{
						continue;
					}
				}

				yield break;

				unsafe void GetEnumerator()
				{
					HRESULT hr = ThisPtr.Get()->BindToHandler(null, BHID.BHID_EnumItems, IID.IID_IEnumShellItems, (void**)pEnumShellItems.GetAddressOf());
					hr.ThrowIfFailedOnDebug();
				}

				unsafe bool GetNext()
				{
					HRESULT hr = pEnumShellItems.Get()->Next(1, pShellItem.GetAddressOf());
					return hr.ThrowIfFailedOnDebug() == HRESULT.S_OK;
				}
			}
		}
	}
}
