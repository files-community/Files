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
		public IContextMenu ShellNewMenu
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		}

		public WindowsFolder(IShellItem shellItem)
		{
			ThisPtr = shellItem;
		}

		public WindowsFolder(Guid folderId)
		{
			HRESULT hr = PInvoke.SHGetKnownFolderItem(&folderId, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, HANDLE.Null, IID.IID_IShellItem, out var shellItemObj);
			var shellItem = (IShellItem)shellItemObj;
			if (hr.Failed)
			{
				hr = PInvoke.SHCreateItemFromParsingName($"Shell:::{folderId:B}", null, typeof(IShellItem).GUID, out shellItemObj);
				shellItem = (IShellItem)shellItemObj;

				// Invalid FOLDERID; this should never happen.
				hr.ThrowOnFailure();
			}

			ThisPtr = shellItem;
		}

		public IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, CancellationToken cancellationToken = default)
		{
			HRESULT hr = ThisPtr.BindToHandler(null, BHID.BHID_EnumItems, IID.IID_IEnumShellItems, out var enumShellItemsObj);
			var enumShellItems = (IEnumShellItems)enumShellItemsObj;
			if (hr.ThrowIfFailedOnDebug().Failed)
				return Enumerable.Empty<IStorableChild>().ToAsyncEnumerable();

			List<IStorableChild> childItems = [];

			var childItemArray = new IShellItem[1];
			while ((hr = enumShellItems.Next(1, childItemArray)) == HRESULT.S_OK)
			{
				IShellItem childItem = childItemArray[0];

				bool isFolder = childItem.GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var dwAttributes).Succeeded && dwAttributes is SFGAO_FLAGS.SFGAO_FOLDER;

				if (type.HasFlag(StorableType.File) && !isFolder)
				{
					childItems.Add(new WindowsFile(childItem));
				}
				else if (type.HasFlag(StorableType.Folder) && isFolder)
				{
					childItems.Add(new WindowsFolder(childItem));
				}
			}

			if (hr.ThrowIfFailedOnDebug().Failed)
				return Enumerable.Empty<IStorableChild>().ToAsyncEnumerable();

			return childItems.ToAsyncEnumerable();
		}
	}
}
