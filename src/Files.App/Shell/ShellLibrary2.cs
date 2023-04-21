using System.Windows.Forms;
using Vanara.Extensions;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static Vanara.PInvoke.Shell32;

namespace Files.App.Shell
{
	/// <summary>Shell library encapsulation.</summary>
	public class ShellLibrary2 : ShellFolder
	{
		//private const string ext = ".library-ms";
		internal IShellLibrary lib;

		private ShellLibraryFolders folders;
		private string name;

		/// <summary>Initializes a new instance of the <see cref="ShellLibrary"/> class.</summary>
		/// <param name="knownFolderId">The known folder identifier.</param>
		/// <param name="readOnly">if set to <c>true</c> [read only].</param>
		public ShellLibrary2(KNOWNFOLDERID knownFolderId, bool readOnly = false)
		{
			lib = new IShellLibrary();
			lib.LoadLibraryFromKnownFolder(knownFolderId.Guid(), readOnly ? STGM.STGM_READ : STGM.STGM_READWRITE);

			Init(knownFolderId.GetIShellItem());
		}

		/// <summary>Initializes a new instance of the <see cref="ShellLibrary"/> class.</summary>
		/// <param name="libraryName">Name of the library.</param>
		/// <param name="kf">The known folder identifier.</param>
		/// <param name="overwrite">if set to <c>true</c> [overwrite].</param>
		public ShellLibrary2(string libraryName, KNOWNFOLDERID kf = KNOWNFOLDERID.FOLDERID_Libraries, bool overwrite = false)
		{
			lib = new IShellLibrary();
			name = libraryName;
			var item = lib.SaveInKnownFolder(kf.Guid(), libraryName, overwrite ? LIBRARYSAVEFLAGS.LSF_OVERRIDEEXISTING : LIBRARYSAVEFLAGS.LSF_FAILIFTHERE);

			Init(item);
		}

		/// <summary>Initializes a new instance of the <see cref="ShellLibrary"/> class.</summary>
		/// <param name="libraryName">Name of the library.</param>
		/// <param name="parent">The parent.</param>
		/// <param name="overwrite">if set to <c>true</c> [overwrite].</param>
		public ShellLibrary2(string libraryName, ShellFolder parent, bool overwrite = false)
		{
			lib = new IShellLibrary();
			name = libraryName;
			var item = lib.Save(parent.IShellItem, libraryName, overwrite ? LIBRARYSAVEFLAGS.LSF_OVERRIDEEXISTING : LIBRARYSAVEFLAGS.LSF_FAILIFTHERE);

			Init(item);
		}

		/// <summary>Initializes a new instance of the <see cref="ShellLibrary"/> class.</summary>
		/// <param name="iItem">The i item.</param>
		/// <param name="readOnly">if set to <c>true</c> [read only].</param>
		public ShellLibrary2(IShellItem iItem, bool readOnly = false)
		{
			lib = new IShellLibrary();
			lib.LoadLibraryFromItem(iItem, readOnly ? STGM.STGM_READ : STGM.STGM_READWRITE);

			Init(iItem);
		}

		/// <summary>Gets or sets the default target folder the library uses for save operations.</summary>
		/// <value>The default save folder.</value>
		public ShellItem DefaultSaveFolder
		{
			get => Open(lib.GetDefaultSaveFolder<IShellItem>(DEFAULTSAVEFOLDERTYPE.DSFT_DETECT));
			set => lib.SetDefaultSaveFolder(DEFAULTSAVEFOLDERTYPE.DSFT_DETECT, value.IShellItem);
		}

		/// <summary>Gets the set of child folders that are contained in the library.</summary>
		/// <value>A <see cref="ShellItemArray"/> containing the child folders.</value>
		public ShellLibraryFolders Folders => folders ??= GetFilteredFolders();

		public void Reload()
		{
			folders = GetFilteredFolders();
		}

		/// <summary>
		/// Gets or sets a string that describes the location of the default icon. The string must be formatted as
		/// <c>ModuleFileName,ResourceIndex or ModuleFileName,-ResourceID</c>.
		/// </summary>
		/// <value>The default icon location.</value>
		public IconLocation IconLocation
		{
			get
			{
				_ = IconLocation.TryParse(lib.GetIcon(), out var l);
				return l;
			}
			set => lib.SetIcon(value.ToString());
		}

		/// <summary>Gets the name relative to the parent for the item.</summary>
		public override string Name { get => name; protected set => name = value; }

		/// <summary>Gets or sets a value indicating whether to pin the library to the navigation pane.</summary>
		/// <value><c>true</c> if pinned to the navigation pane; otherwise, <c>false</c>.</value>
		public bool PinnedToNavigationPane
		{
			get => lib.GetOptions().IsFlagSet(LIBRARYOPTIONFLAGS.LOF_PINNEDTONAVPANE);
			set => lib.SetOptions(LIBRARYOPTIONFLAGS.LOF_PINNEDTONAVPANE, value ? LIBRARYOPTIONFLAGS.LOF_PINNEDTONAVPANE : 0);
		}

		/// <summary>Gets or sets the library's View Template.</summary>
		/// <value>The View Template.</value>
		public LibraryViewTemplate ViewTemplate
		{
			get => (LibraryViewTemplate)ShlGuidExt.Lookup<FOLDERTYPEID>(ViewTemplateId);
			set
			{
				if (value != LibraryViewTemplate.Custom)
					ViewTemplateId = ((FOLDERTYPEID)value).Guid();
			}
		}

		/// <summary>Gets or sets the library's View Template identifier.</summary>
		/// <value>The View Template identifier.</value>
		public Guid ViewTemplateId
		{
			get => lib.GetFolderType();
			set => lib.SetFolderType(value);
		}

		/// <summary>Commits library updates.</summary>
		public void Commit() => lib.Commit();

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public override void Dispose()
		{
			lib = null;

			folders?.Dispose();
			folders = null;

			base.Dispose();
			GC.SuppressFinalize(this);
		}

		/// <summary>Gets the set of child folders that are contained in the library.</summary>
		/// <param name="filter">A value that determines the folders to get.</param>
		/// <returns>A <see cref="ShellItemArray"/> containing the child folders.</returns>
		public ShellLibraryFolders GetFilteredFolders(LibraryFolderFilter filter = LibraryFolderFilter.AllItems)
			=> new(lib, lib.GetFolders<IShellItemArray>((LIBRARYFOLDERFILTER)filter));

		/// <summary>Resolves the target location of a library folder, even if the folder has been moved or renamed.</summary>
		/// <param name="item">A ShellItem object that represents the library folder to locate.</param>
		/// <param name="timeout">
		/// The maximum time the method will attempt to locate the folder before returning. If the folder could not be located before the
		/// specified time elapses, an error is returned.
		/// </param>
		/// <returns>The resulting target location.</returns>
		public ShellItem ResolveFolder(ShellItem item, TimeSpan timeout)
			=> Open(lib.ResolveFolder<IShellItem>(item.IShellItem, Convert.ToUInt32(timeout.TotalMilliseconds)));

		/// <summary>Shows the library management dialog box, which enables users to manage the library folders and default save location.</summary>
		/// <param name="parentWindow">
		/// The handle for the window that owns the library management dialog box. The value of this parameter can be NULL.
		/// </param>
		/// <param name="title">
		/// The title for the library management dialog. To display the generic title string, set the value of this parameter to NULL.
		/// </param>
		/// <param name="instruction">
		/// The help string to display below the title string in the library management dialog box. To display the generic help string, set
		/// the value of this parameter to NULL.
		/// </param>
		/// <param name="allowUnindexableLocations">
		/// if set to <c>true</c> do not display a warning dialog to the user in collisions that concern network locations that cannot be indexed.
		/// </param>
		public void ShowLibraryManagementDialog(IWin32Window parentWindow = null, string title = null, string instruction = null, bool allowUnindexableLocations = false)
		{
			SHShowManageLibraryUI(IShellItem, parentWindow?.Handle ?? IntPtr.Zero, title, instruction,
				allowUnindexableLocations ? LIBRARYMANAGEDIALOGOPTIONS.LMD_ALLOWUNINDEXABLENETWORKLOCATIONS : LIBRARYMANAGEDIALOGOPTIONS.LMD_DEFAULT).ThrowIfFailed();
		}

		/// <summary>Folders of a <see cref="ShellLibrary"/>.</summary>
		/// <seealso cref="ShellItemArray"/>
		/// <seealso cref="ICollection{ShellItem}"/>
		public class ShellLibraryFolders : ShellItemArray, ICollection<ShellItem>
		{
			private IShellLibrary lib;

			/// <summary>Initializes a new instance of the <see cref="ShellLibraryFolders"/> class.</summary>
			/// <param name="lib">The library.</param>
			/// <param name="shellItemArray">The shell item array.</param>
			internal ShellLibraryFolders(IShellLibrary lib, IShellItemArray shellItemArray) : base(shellItemArray)
				=> this.lib = lib;

			/// <summary>Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</summary>
			bool ICollection<ShellItem>.IsReadOnly
				=> false;

			/// <summary>Adds the specified location.</summary>
			/// <param name="location">The location.</param>
			/// <exception cref="ArgumentNullException">location</exception>
			public void Add(ShellItem location)
			{
				if (location is null) throw new ArgumentNullException(nameof(location));
				lib.AddFolder(location.IShellItem);
			}

			/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
			public override void Dispose()
			{
				lib = null;
				base.Dispose();
				GC.SuppressFinalize(this);
			}

			/// <summary>Removes the specified location.</summary>
			/// <param name="location">The location.</param>
			/// <returns><c>true</c> on success.</returns>
			/// <exception cref="ArgumentNullException">location</exception>
			public bool Remove(ShellItem location)
			{
				if (location is null)
					throw new ArgumentNullException(nameof(location));

				try
				{
					lib.RemoveFolder(location.IShellItem);
					return true;
				}
				catch
				{
					return false;
				}
			}

			/// <summary>Removes all items from the <see cref="ICollection{ShellItem}"/>.</summary>
			/// <exception cref="NotImplementedException"></exception>
			void ICollection<ShellItem>.Clear()
				=> throw new NotImplementedException();
		}
	}
}
