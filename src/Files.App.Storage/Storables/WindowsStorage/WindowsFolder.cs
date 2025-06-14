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
	public unsafe class WindowsFolder : WindowsStorable, IWindowsFolder
	{
		/// <inheritdoc/>
		public IContextMenu* ShellNewMenu
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected internal set;
		}

		public WindowsFolder(IShellItem* ptr)
		{
			ThisPtr = ptr;
		}

		public WindowsFolder(Guid folderId)
		{
			IShellItem* pShellItem = default;

			HRESULT hr = PInvoke.SHGetKnownFolderItem(&folderId, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, HANDLE.Null, IID.IID_IShellItem, (void**)&pShellItem);
			if (hr.Failed)
			{
				fixed (char* pszShellPath = $"Shell:::{folderId:B}")
					hr = PInvoke.SHCreateItemFromParsingName(pszShellPath, null, IID.IID_IShellItem, (void**)&pShellItem);

				// Invalid FOLDERID; this should never happen.
				hr.ThrowOnFailure();
			}

			ThisPtr = pShellItem;
		}

		public IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, CancellationToken cancellationToken = default)
		{
			using ComPtr<IEnumShellItems> pEnumShellItems = default;

			HRESULT hr = ThisPtr->BindToHandler(null, BHID.BHID_EnumItems, IID.IID_IEnumShellItems, (void**)pEnumShellItems.GetAddressOf());
			if (hr.ThrowIfFailedOnDebug().Failed)
				return Enumerable.Empty<IStorableChild>().ToAsyncEnumerable();

			List<IStorableChild> childItems = [];

			IShellItem* pChildShellItem = null;
			while ((hr = pEnumShellItems.Get()->Next(1, &pChildShellItem)) == HRESULT.S_OK)
			{
				bool isFolder = pChildShellItem->GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var dwAttributes).Succeeded && dwAttributes is SFGAO_FLAGS.SFGAO_FOLDER;

				if (type.HasFlag(StorableType.File) && !isFolder)
				{
					childItems.Add(new WindowsFile(pChildShellItem));
				}
				else if (type.HasFlag(StorableType.Folder) && isFolder)
				{
					childItems.Add(new WindowsFolder(pChildShellItem));
				}
			}

			if (hr.ThrowIfFailedOnDebug().Failed)
				return Enumerable.Empty<IStorableChild>().ToAsyncEnumerable();

			return childItems.ToAsyncEnumerable();
		}

		public override void Dispose()
		{
			base.Dispose();

			if (ShellNewMenu is not null) ShellNewMenu->Release();
		}
	}
}
