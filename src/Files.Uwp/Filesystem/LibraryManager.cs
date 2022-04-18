using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Uwp.Helpers;
using Files.Shared;
using Files.Shared.Extensions;
using Files.Uwp.ViewModels;
using Microsoft.Toolkit.Uwp;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Files.Uwp.Filesystem
{
    public class LibraryManager : IDisposable
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public MainViewModel MainViewModel => App.MainViewModel;

        private readonly List<LibraryLocationItem> librariesList = new List<LibraryLocationItem>();

        public EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

        public IReadOnlyList<LibraryLocationItem> Libraries
        {
            get
            {
                lock (librariesList)
                {
                    return librariesList.ToList().AsReadOnly();
                }
            }
        }

        public LibraryManager()
        {
        }

        private static bool IsLibraryOnSidebar(LibraryLocationItem item) => item != null && !item.IsEmpty && item.IsDefaultLocation;

        public void Dispose()
        {
        }

        public async Task EnumerateLibrariesAsync()
        {
            librariesList.Clear();
            var libs = await LibraryHelper.ListUserLibraries();
            if (libs != null)
            {
                libs.Sort();
                librariesList.AddRange(libs);
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
            if (changedLibrary != null)
            {
                librariesList.Remove(changedLibrary);
                DataChanged?.Invoke(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, changedLibrary));
            }
            // library is null in case it was deleted
            if (library != null && !Libraries.Any(x => x.Path == library.FullPath))
            {
                var index = librariesList.AddSorted(new LibraryLocationItem(library));
                DataChanged?.Invoke(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, library, index));
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
            return library != null;
        }

        public async Task<bool> CreateNewLibrary(string name)
        {
            if (!CanCreateLibrary(name).result)
            {
                return false;
            }
            var newLib = await LibraryHelper.CreateLibrary(name);
            if (newLib != null)
            {
                var index = librariesList.AddSorted(newLib);
                DataChanged?.Invoke(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newLib, index));
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
                    librariesList[librariesList.IndexOf(libItem)] = newLib;
                    DataChanged?.Invoke(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newLib));
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