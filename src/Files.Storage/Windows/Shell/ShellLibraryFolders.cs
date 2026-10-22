// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections;
using Windows.Win32.UI.Shell;

namespace Files.App.Utils.Shell
{
	/// <summary>Represents the child folders contained in a Shell library.</summary>
	[WinRT.GeneratedWinRTExposedType]
	public sealed partial class ShellLibraryFolders : ICollection<ShellItem>, IDisposable
	{
		private IShellLibrary? library;
		private readonly List<ShellItem> items = [];

		internal ShellLibraryFolders(IShellLibrary library, IShellItemArray itemArray)
		{
			this.library = library;
			itemArray.GetCount(out uint count).ThrowOnFailure();
			for (uint index = 0; index < count; index++)
			{
				itemArray.GetItemAt(index, out IShellItem item).ThrowOnFailure();
				items.Add(ShellItem.Open(item));
			}
		}

		private IShellLibrary Library => library ?? throw new ObjectDisposedException(nameof(ShellLibraryFolders));

		/// <inheritdoc/>
		public int Count => items.Count;

		/// <inheritdoc/>
		public bool IsReadOnly => false;

		/// <summary>Adds a folder to the library.</summary>
		public void Add(ShellItem item)
		{
			ArgumentNullException.ThrowIfNull(item);
			Library.AddFolder(item.IShellItem).ThrowOnFailure();
			items.Add(ShellItem.Open(item.IShellItem));
		}

		/// <summary>Removes a folder from the library.</summary>
		public bool Remove(ShellItem item)
		{
			ArgumentNullException.ThrowIfNull(item);
			if (Library.RemoveFolder(item.IShellItem).Failed)
				return false;

			int index = items.FindIndex(existing => ReferenceEquals(existing, item) ||
				existing.IShellItem.Compare(item.IShellItem, (uint)_SICHINTF.SICHINT_CANONICAL, out int order).Succeeded && order is 0);
			if (index >= 0)
			{
				ShellItem cachedItem = items[index];
				items.RemoveAt(index);
				// Dispose only the cached wrapper; the caller owns the supplied item.
				if (!ReferenceEquals(cachedItem, item))
					cachedItem.Dispose();
			}

			return true;
		}

		public bool Contains(ShellItem item) => items.Contains(item);

		public void CopyTo(ShellItem[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);

		public IEnumerator<ShellItem> GetEnumerator() => items.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		void ICollection<ShellItem>.Clear() => throw new NotSupportedException();

		/// <inheritdoc/>
		public void Dispose()
		{
			foreach (ShellItem item in items)
				item.Dispose();
			items.Clear();
			library = null;
		}
	}
}
