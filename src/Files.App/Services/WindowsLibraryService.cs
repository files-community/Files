// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.IO;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace Files.App.Services
{
	public class WindowsLibraryService : IWindowsLibraryService, IDisposable
	{
		private readonly FileSystemWatcher _librariesWatcher;

		public event EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;

		private readonly List<LibraryLocationItem> _Libraries = [];
		public IReadOnlyList<LibraryLocationItem> Libraries
		{
			get
			{
				lock (_Libraries)
					return _Libraries.ToList().AsReadOnly();
			}
		}

		public WindowsLibraryService()
		{
			_librariesWatcher = new()
			{
				Path = ShellLibraryItem.LibrariesPath,
				Filter = "*" + ShellLibraryItem.EXTENSION,
				NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastWrite | NotifyFilters.FileName,
				IncludeSubdirectories = true,
			};

			_librariesWatcher.Created += OnLibraryChanged;
			_librariesWatcher.Changed += OnLibraryChanged;
			_librariesWatcher.Deleted += OnLibraryChanged;
			_librariesWatcher.Renamed += OnLibraryRenamed;
		}

		/// <inheritdoc/>
		public async Task<List<LibraryLocationItem>> GetLibrariesAsync()
		{
			var libraries = await Win32Helper.StartSTATask(() =>
			{
				try
				{
					var libraryItems = new List<ShellLibraryItem>();

					// https://learn.microsoft.com/windows/win32/search/-search-win7-development-scenarios#library-descriptions
					var shellFiles = Directory.EnumerateFiles(ShellLibraryItem.LibrariesPath, "*" + ShellLibraryItem.EXTENSION);
					foreach (var item in shellFiles)
					{
						using var shellItem = new ShellLibraryEx(Shell32.ShellUtil.GetShellItemForPath(item), true);

						if (shellItem is ShellLibraryEx libraryItem)
							libraryItems.Add(ShellFolderExtensions.GetShellLibraryItem(libraryItem, item));
					}

					return libraryItems;
				}
				catch (Exception e)
				{
					App.Logger.LogWarning(e, null);
					return [];
				}
			});

			return libraries!.Select(lib => new LibraryLocationItem(lib)).ToList();
		}

		/// <inheritdoc/>
		public async Task UpdateLibrariesAsync()
		{
			lock (_Libraries)
				_Libraries.Clear();

			var libs = await GetLibrariesAsync();
			if (libs is not null)
			{
				libs.Sort();

				lock (_Libraries)
					_Libraries.AddRange(libs);
			}

			DataChanged?.Invoke(SectionType.Library, new(NotifyCollectionChangedAction.Reset));
		}

		/// <inheritdoc/>
		public async Task<LibraryLocationItem> UpdateLibraryAsync(string libraryPath, string? defaultSaveFolder = null, string[]? folders = null, bool? isPinned = null)
		{
			// Nothing to update
			if (string.IsNullOrWhiteSpace(libraryPath) || (defaultSaveFolder is null && folders is null && isPinned is null))
				return null;

			var item = await Win32Helper.StartSTATask(() =>
			{
				try
				{
					bool updated = false;

					using var library = new ShellLibraryEx(Shell32.ShellUtil.GetShellItemForPath(libraryPath), false);
					if (folders is not null)
					{
						if (folders.Length > 0)
						{
							var foldersToRemove = library.Folders.Where(f => !folders.Any(folderPath => string.Equals(folderPath, f.FileSystemPath, StringComparison.OrdinalIgnoreCase)));
							foreach (var toRemove in foldersToRemove)
							{
								library.Folders.Remove(toRemove);
								updated = true;
							}
							var foldersToAdd =
								folders.Distinct(StringComparer.OrdinalIgnoreCase)
									.Where(folderPath => !library.Folders.Any(f => string.Equals(folderPath, f.FileSystemPath, StringComparison.OrdinalIgnoreCase)))
									.Select(ShellItem.Open);

							foreach (var toAdd in foldersToAdd)
							{
								library.Folders.Add(toAdd);
								updated = true;
							}

							foreach (var toAdd in foldersToAdd)
								toAdd.Dispose();
						}
					}

					if (defaultSaveFolder is not null)
					{
						library.DefaultSaveFolder = ShellItem.Open(defaultSaveFolder);
						updated = true;
					}

					if (isPinned is not null)
					{
						library.PinnedToNavigationPane = isPinned == true;
						updated = true;
					}

					if (updated)
					{
						library.Commit();
						library.Reload();

						return Task.FromResult(ShellFolderExtensions.GetShellLibraryItem(library, libraryPath));
					}
				}
				catch (Exception e)
				{
					App.Logger.LogWarning(e, null);
				}

				return Task.FromResult<ShellLibraryItem>(null);
			});

			var newLib = item is not null ? new LibraryLocationItem(item) : null;
			if (newLib is not null)
			{
				var libItem = Libraries.FirstOrDefault(l => string.Equals(l.Path, libraryPath, StringComparison.OrdinalIgnoreCase));
				if (libItem is not null)
				{
					lock (_Libraries)
						_Libraries[_Libraries.IndexOf(libItem)] = newLib;

					DataChanged?.Invoke(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newLib, libItem));
				}

				return newLib;
			}

			return null;
		}

		/// <inheritdoc/>
		public bool TryGetLibrary(string path, out LibraryLocationItem library)
		{
			if (string.IsNullOrWhiteSpace(path) ||
				!path.EndsWith(ShellLibraryItem.EXTENSION, StringComparison.OrdinalIgnoreCase))
			{
				library = null;

				return false;
			}

			library = Libraries.FirstOrDefault(l => string.Equals(path, l.Path, StringComparison.OrdinalIgnoreCase));

			return library is not null;
		}

		/// <inheritdoc/>
		public async Task<bool> CreateNewLibrary(string name)
		{
			if (string.IsNullOrWhiteSpace(name) || !CanCreateLibrary(name).result)
				return false;

			var newLib = new LibraryLocationItem(await Win32Helper.StartSTATask(() =>
			{
				try
				{
					using var library = new ShellLibraryEx(name, Shell32.KNOWNFOLDERID.FOLDERID_Libraries, false);
					library.Folders.Add(ShellItem.Open(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))); // Add default folder so it's not empty
					library.Commit();
					library.Reload();
					return Task.FromResult(ShellFolderExtensions.GetShellLibraryItem(library, library.GetDisplayName(ShellItemDisplayString.DesktopAbsoluteParsing)));
				}
				catch (Exception e)
				{
					App.Logger.LogWarning(e, null);
				}

				return Task.FromResult<ShellLibraryItem>(null);
			}));

			if (newLib is not null)
			{
				lock (_Libraries)
					_Libraries.Add(newLib);

				DataChanged?.Invoke(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newLib));

				return true;
			}

			return false;
		}

		/// <inheritdoc/>
		public (bool result, string reason) CanCreateLibrary(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				return (false, "ErrorInputEmpty".GetLocalizedResource());

			if (FilesystemHelpers.ContainsRestrictedCharacters(name))
				return (false, "ErrorNameInputRestrictedCharacters".GetLocalizedResource());

			if (FilesystemHelpers.ContainsRestrictedFileName(name))
				return (false, "ErrorNameInputRestricted".GetLocalizedResource());

			if (Libraries.Any((item) => string.Equals(name, item.Text, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(name, Path.GetFileNameWithoutExtension(item.Path), StringComparison.OrdinalIgnoreCase)))
				return (false, "CreateLibraryErrorAlreadyExists".GetLocalizedResource());

			return (true, string.Empty);
		}

		public bool IsLibraryPath(string path)
		{
			return !string.IsNullOrEmpty(path) && path.EndsWith(ShellLibraryItem.EXTENSION, StringComparison.OrdinalIgnoreCase);
		}

		private void OnLibraryChanged(WatcherChangeTypes changeType, string oldPath, string newPath)
		{
			if (newPath is not null && (!newPath.ToLowerInvariant().EndsWith(ShellLibraryItem.EXTENSION, StringComparison.Ordinal) || !File.Exists(newPath)))
			{
				Debug.WriteLine($"Ignored library event: {changeType}, {oldPath} -> {newPath}");
				return;
			}

			Debug.WriteLine($"Library event: {changeType}, {oldPath} -> {newPath}");

			if (!changeType.HasFlag(WatcherChangeTypes.Deleted))
			{
				var library = SafetyExtensions.IgnoreExceptions(() => new ShellLibraryEx(Shell32.ShellUtil.GetShellItemForPath(newPath), true));
				if (library is null)
				{
					App.Logger.LogWarning($"Failed to open library after {changeType}: {newPath}");
					return;
				}

				var library1 = ShellFolderExtensions.GetShellLibraryItem(library, newPath);

				string? path = oldPath;
				if (string.IsNullOrEmpty(oldPath))
					path = library1?.FullPath;

				var changedLibrary = Libraries.FirstOrDefault(l => string.Equals(l.Path, path, StringComparison.OrdinalIgnoreCase));
				if (changedLibrary is not null)
				{
					lock (_Libraries)
						_Libraries.Remove(changedLibrary);

					DataChanged?.Invoke(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, changedLibrary));
				}

				// library is null in case it was deleted
				if (library is not null && !Libraries.Any(x => x.Path == library1?.FullPath))
				{
					var libItem = new LibraryLocationItem(library1);
					lock (_Libraries)
						_Libraries.Add(libItem);

					DataChanged?.Invoke(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, libItem));
				}

				library?.Dispose();
			}
		}

		private void OnLibraryChanged(object sender, FileSystemEventArgs e)
		{
			switch (e.ChangeType)
			{
				case WatcherChangeTypes.Created:
				case WatcherChangeTypes.Changed:
					OnLibraryChanged(e.ChangeType, e.FullPath, e.FullPath);
					break;
				case WatcherChangeTypes.Deleted:
					OnLibraryChanged(e.ChangeType, e.FullPath, null);
					break;
			}
		}

		private void OnLibraryRenamed(object sender, RenamedEventArgs e)
		{
			OnLibraryChanged(e.ChangeType, e.OldFullPath, e.FullPath);
		}

		public void Dispose()
		{
			_librariesWatcher?.Dispose();
		}
	}
}
