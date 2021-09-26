using Files.Common;
using Files.Extensions;
using Files.Helpers;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Filesystem
{
    public class LibraryManager
    {
        public BulkConcurrentObservableCollection<LibraryLocationItem> Libraries { get; } = new BulkConcurrentObservableCollection<LibraryLocationItem>();

        public event EventHandler<IReadOnlyList<LibraryLocationItem>> RefreshCompleted;
        public event EventHandler<SectionType> RemoveLibrariesSidebarSection;

        public LibraryManager()
        {
        }

        public static bool IsLibraryOnSidebar(LibraryLocationItem item) => item != null && !item.IsEmpty && item.IsDefaultLocation;

        public void HandleWin32LibraryEvent(ShellLibraryItem library, string oldPath)
        {
            string path = oldPath;
            if (string.IsNullOrEmpty(oldPath))
            {
                path = library?.FullPath;
            }

            var changedLibrary = Libraries.FirstOrDefault(l => string.Equals(l.Path, path, StringComparison.OrdinalIgnoreCase));
            if (changedLibrary != null)
            {
                Libraries.Remove(changedLibrary);
            }
            // library is null in case it was deleted
            if (library != null)
            {
                Libraries.AddSorted(new LibraryLocationItem(library));
            }
        }

        public async Task EnumerateLibrariesAsync()
        {
            if (!App.AppSettings.ShowLibrarySection)
            {
                return;
            }

            Libraries.BeginBulkOperation();
            Libraries.Clear();
            var libs = await LibraryHelper.ListUserLibraries();
            if (libs != null)
            {
                libs.Sort();
                Libraries.AddRange(libs);
            }
            Libraries.EndBulkOperation();

            RefreshCompleted?.Invoke(this, Libraries.ToList());
        }

        public async void UpdateLibrariesSectionVisibility()
        {
            if (App.AppSettings.ShowLibrarySection)
            {
                await EnumerateLibrariesAsync();
            }
            else
            {
                RemoveLibrariesSidebarSection?.Invoke(this, SectionType.Library);
            }
        }

        public bool TryGetLibrary(string path, out LibraryLocationItem library)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.ToLower().EndsWith(ShellLibraryItem.EXTENSION))
            {
                library = null;
                return false;
            }
            library = Libraries.FirstOrDefault(l => string.Equals(path, l.Path, StringComparison.OrdinalIgnoreCase));
            return library != null;
        }

        public bool IsLibraryPath(string path) => TryGetLibrary(path, out _);

        public async Task<bool> CreateNewLibrary(string name)
        {
            if (!CanCreateLibrary(name).result)
            {
                return false;
            }
            var newLib = await LibraryHelper.CreateLibrary(name);
            if (newLib != null)
            {
                Libraries.AddSorted(newLib);
                return true;
            }
            return false;
        }

        public async Task<LibraryLocationItem> UpdateLibrary(string libraryPath, string defaultSaveFolder = null, string[] folders = null, bool? isPinned = null)
        {
            var newLib = await LibraryHelper.UpdateLibrary(libraryPath, defaultSaveFolder, folders, isPinned);
            if (newLib != null)
            {
                var libItem = Libraries.FirstOrDefault(l => string.Equals(l.Path, libraryPath, StringComparison.OrdinalIgnoreCase));
                if (libItem != null)
                {
                    Libraries[Libraries.IndexOf(libItem)] = libItem;
                }
                return newLib;
            }
            return null;
        }

        public (bool result, string reason) CanCreateLibrary(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return (false, "CreateLibraryErrorInputEmpty".GetLocalized());
            }
            if (FilesystemHelpers.ContainsRestrictedCharacters(name))
            {
                return (false, "ErrorNameInputRestrictedCharacters".GetLocalized());
            }
            if (FilesystemHelpers.ContainsRestrictedFileName(name))
            {
                return (false, "ErrorNameInputRestricted".GetLocalized());
            }
            if (Libraries.Any((item) => string.Equals(name, item.Text, StringComparison.OrdinalIgnoreCase) || string.Equals(name, Path.GetFileNameWithoutExtension(item.Path), StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "CreateLibraryErrorAlreadyExists".GetLocalized());
            }
            else
            {
                return (true, string.Empty);
            }
        }
    }
}