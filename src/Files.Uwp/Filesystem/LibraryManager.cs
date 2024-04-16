using Files.Shared;
using Files.Uwp.Helpers;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Uwp.Filesystem
{
    public class LibraryManager : IDisposable
    {
        public EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

        private readonly List<LibraryLocationItem> libraries = new();
        public IReadOnlyList<LibraryLocationItem> Libraries
        {
            get
            {
                lock (libraries)
                {
                    return libraries.ToList().AsReadOnly();
                }
            }
        }

        public async Task UpdateLibrariesAsync()
        {
            lock (libraries)
            {
                libraries.Clear();
            }
            var libs = await LibraryHelper.ListUserLibraries();
            if (libs is not null)
            {
                libs.Sort();
                lock (libraries)
                {
                    libraries.AddRange(libs);
                }
            }
            DataChanged?.Invoke(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public Task HandleWin32LibraryEvent(ShellLibraryItem library, string oldPath)
        {
            string path = oldPath;
            if (string.IsNullOrEmpty(oldPath))
            {
                path = library?.FullPath;
            }
            var changedLibrary = Libraries.FirstOrDefault(l => string.Equals(l.Path, path, StringComparison.OrdinalIgnoreCase));
            if (changedLibrary is not null)
            {
                lock (libraries)
                {
                    libraries.Remove(changedLibrary);
                }
                DataChanged?.Invoke(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, changedLibrary));
            }
            // library is null in case it was deleted
            if (library is not null && !Libraries.Any(x => x.Path == library.FullPath))
            {
                var libItem = new LibraryLocationItem(library);
                lock (libraries)
                {
                    libraries.Add(libItem);
                }
                DataChanged?.Invoke(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, libItem));
            }
            return Task.CompletedTask;
        }

        public bool TryGetLibrary(string path, out LibraryLocationItem library)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.EndsWith(ShellLibraryItem.EXTENSION, StringComparison.OrdinalIgnoreCase))
            {
                library = null;
                return false;
            }
            library = Libraries.FirstOrDefault(l => string.Equals(path, l.Path, StringComparison.OrdinalIgnoreCase));
            return library is not null;
        }

        public async Task<bool> CreateNewLibrary(string name)
        {
            if (!CanCreateLibrary(name).result)
            {
                return false;
            }
            var newLib = await LibraryHelper.CreateLibrary(name);
            if (newLib is not null)
            {
                lock (libraries)
                {
                    libraries.Add(newLib);
                }
                DataChanged?.Invoke(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newLib));
                return true;
            }
            return false;
        }

        public async Task<LibraryLocationItem> UpdateLibrary(string libraryPath, string defaultSaveFolder = null, string[] folders = null, bool? isPinned = null)
        {
            var newLib = await LibraryHelper.UpdateLibrary(libraryPath, defaultSaveFolder, folders, isPinned);
            if (newLib is not null)
            {
                var libItem = Libraries.FirstOrDefault(l => string.Equals(l.Path, libraryPath, StringComparison.OrdinalIgnoreCase));
                if (libItem is not null)
                {
                    lock (libraries)
                    {
                        libraries[libraries.IndexOf(libItem)] = newLib;
                    }
                    DataChanged?.Invoke(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newLib, libItem));
                }
                return newLib;
            }
            return null;
        }

        public (bool result, string reason) CanCreateLibrary(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return (false, "ErrorInputEmpty".GetLocalized());
            }
            if (FilesystemHelpers.ContainsRestrictedCharacters(name))
            {
                return (false, "ErrorNameInputRestrictedCharacters".GetLocalized());
            }
            if (FilesystemHelpers.ContainsRestrictedFileName(name))
            {
                return (false, "ErrorNameInputRestricted".GetLocalized());
            }
            if (Libraries.Any((item) => string.Equals(name, item.Text, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, Path.GetFileNameWithoutExtension(item.Path), StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "CreateLibraryErrorAlreadyExists".GetLocalized());
            }
            return (true, string.Empty);
        }

        public void Dispose() {}
    }
}