// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Represents a Windows Shell library backed by source-generated COM interop.
	/// </summary>
	[WinRT.GeneratedWinRTExposedType]
	public sealed partial class ShellLibraryEx : ShellFolder
	{
		private IShellLibrary? library;
		private ShellLibraryFolders? folders;

		/// <summary>Initializes a library from its Shell item.</summary>
		/// <param name="libraryItem">The Shell item for the library definition file.</param>
		/// <param name="readOnly">Whether to open the library read-only.</param>
		public ShellLibraryEx(IShellItem libraryItem, bool readOnly = false) : this(Load(libraryItem, readOnly))
		{
		}

		/// <summary>Creates a library in a known folder.</summary>
		/// <param name="libraryName">The library name.</param>
		/// <param name="knownFolderId">The known folder in which to save the library.</param>
		/// <param name="overwrite">Whether to overwrite an existing library with the same name.</param>
		public ShellLibraryEx(string libraryName, Guid knownFolderId, bool overwrite = false) : this(Create(libraryName, knownFolderId, overwrite))
		{
		}

		private ShellLibraryEx((IShellLibrary Library, IShellItem Item) state) : base(state.Item)
		{
			library = state.Library;
		}

		private IShellLibrary Library => library ?? throw new ObjectDisposedException(nameof(ShellLibraryEx));

		/// <summary>Gets or sets the default target folder used for save operations.</summary>
		public ShellItem DefaultSaveFolder
		{
			get
			{
				Library.GetDefaultSaveFolder(DEFAULTSAVEFOLDERTYPE.DSFT_DETECT, out IShellItem item).ThrowOnFailure();
				return Open(item);
			}
			set => Library.SetDefaultSaveFolder(DEFAULTSAVEFOLDERTYPE.DSFT_DETECT, value.IShellItem).ThrowOnFailure();
		}

		/// <summary>Gets the child folders contained in the library.</summary>
		public ShellLibraryFolders Folders => folders ??= GetFolders();

		/// <summary>Gets or sets whether the library is pinned to the navigation pane.</summary>
		public bool PinnedToNavigationPane
		{
			get
			{
				Library.GetOptions(out LIBRARYOPTIONFLAGS options).ThrowOnFailure();
				return (options & LIBRARYOPTIONFLAGS.LOF_PINNEDTONAVPANE) is not 0;
			}
			set => Library.SetOptions(LIBRARYOPTIONFLAGS.LOF_PINNEDTONAVPANE, value ? LIBRARYOPTIONFLAGS.LOF_PINNEDTONAVPANE : LIBRARYOPTIONFLAGS.LOF_DEFAULT).ThrowOnFailure();
		}

		/// <summary>Reloads the library folder collection.</summary>
		public void Reload()
		{
			folders?.Dispose();
			folders = GetFolders();
		}

		/// <summary>Commits library updates.</summary>
		public void Commit() => Library.Commit().ThrowOnFailure();

		/// <inheritdoc/>
		public override void Dispose()
		{
			folders?.Dispose();
			folders = null;
			library = null;
			base.Dispose();
		}

		private ShellLibraryFolders GetFolders()
		{
			Library.GetFolders(LIBRARYFOLDERFILTER.LFF_ALLITEMS, out IShellItemArray itemArray).ThrowOnFailure();
			return new(Library, itemArray);
		}

		private static (IShellLibrary Library, IShellItem Item) Load(IShellItem item, bool readOnly)
		{
			IShellLibrary library = Windows.Win32.UI.Shell.ShellLibrary.CreateInstance<IShellLibrary>();
			library.LoadLibraryFromItem(item, (uint)(readOnly ? STGM.STGM_READ : STGM.STGM_READWRITE)).ThrowOnFailure();
			return (library, item);
		}

		private static (IShellLibrary Library, IShellItem Item) Create(string name, Guid knownFolderId, bool overwrite)
		{
			IShellLibrary library = Windows.Win32.UI.Shell.ShellLibrary.CreateInstance<IShellLibrary>();
			library.SaveInKnownFolder(knownFolderId, name, overwrite ? LIBRARYSAVEFLAGS.LSF_OVERRIDEEXISTING : LIBRARYSAVEFLAGS.LSF_FAILIFTHERE, out IShellItem item).ThrowOnFailure();
			return (library, item);
		}
	}
}
