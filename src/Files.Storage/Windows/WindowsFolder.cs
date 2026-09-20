// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

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
		public IContextMenu? ShellNewMenu
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		}

		public WindowsFolder(IShellItem ptr)
		{
			ThisPtr = ptr;
		}

		public WindowsFolder(Guid folderId)
		{
			HRESULT hr = PInvoke.SHGetKnownFolderItem(folderId, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, null, out IShellItem shellItem);
			if (hr.Failed)
			{
				hr = PInvoke.SHCreateItemFromParsingName($"Shell:::{folderId:B}", null, out shellItem);

				// Invalid FOLDERID; this should never happen.
				hr.ThrowOnFailure();
			}

			ThisPtr = shellItem;
		}

		public IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, CancellationToken cancellationToken = default)
		{
			HRESULT hr = ThisPtr.BindToHandler(null, PInvoke.BHID_EnumItems, out IEnumShellItems? pEnumShellItems);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return Enumerable.Empty<IStorableChild>().ToAsyncEnumerable();

			List<IStorableChild> childItems = [];
			IShellItem[] pChildShellItems = new IShellItem[1];

			while ((hr = pEnumShellItems!.Next(1, pChildShellItems, null)) == HRESULT.S_OK)
			{
				IShellItem pChildShellItem = pChildShellItems[0];
				bool isFolder = pChildShellItem.GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var dwAttributes).Succeeded && dwAttributes is SFGAO_FLAGS.SFGAO_FOLDER;

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
			ShellNewMenu = null;
		}
	}
}
