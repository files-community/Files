using Files.Common;
using Files.DataModels;
using Files.DataModels.NavigationControlItems;
using Files.Extensions;
using Files.Helpers;
using Files.Services;
using Files.UserControls;
using Files.ViewModels;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Files.Filesystem
{
    public class LibraryManager : IDisposable
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public MainViewModel MainViewModel => App.MainViewModel;

        private LocationItem librarySection;

        public BulkConcurrentObservableCollection<LibraryLocationItem> Libraries { get; } = new BulkConcurrentObservableCollection<LibraryLocationItem>();

        public LibraryManager()
        {
            Libraries.CollectionChanged += Libraries_CollectionChanged;
        }

        private async void Libraries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!UserSettingsService.AppearanceSettingsService.ShowLibrarySection)
            {
                return;
            }

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await SidebarControl.SideBarItemsSemaphore.WaitAsync();
                try
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Replace:
                        case NotifyCollectionChangedAction.Remove:
                            foreach (var lib in e.OldItems.Cast<LibraryLocationItem>())
                            {
                                librarySection.ChildItems.Remove(lib);
                            }
                            if (e.Action == NotifyCollectionChangedAction.Replace)
                            {
                                goto case NotifyCollectionChangedAction.Add;
                            }
                            break;

                        case NotifyCollectionChangedAction.Reset:
                            foreach (var lib in Libraries.Where(IsLibraryOnSidebar))
                            {
                                if (!librarySection.ChildItems.Any(x => x.Path == lib.Path))
                                {
                                    if (await lib.CheckDefaultSaveFolderAccess())
                                    {
                                        lib.Font = MainViewModel.FontName;
                                        librarySection.ChildItems.AddSorted(lib);
                                        this.LoadLibraryIcon(lib);
                                    }
                                }
                            }
                            foreach (var lib in librarySection.ChildItems.ToList())
                            {
                                if (!Libraries.Any(x => x.Path == lib.Path))
                                {
                                    librarySection.ChildItems.Remove(lib);
                                }
                            }
                            break;

                        case NotifyCollectionChangedAction.Add:
                            foreach (var lib in e.NewItems.Cast<LibraryLocationItem>().Where(IsLibraryOnSidebar))
                            {
                                if (await lib.CheckDefaultSaveFolderAccess())
                                {
                                    lib.Font = MainViewModel.FontName;
                                    librarySection.ChildItems.AddSorted(lib);
                                    this.LoadLibraryIcon(lib);
                                }
                            }
                            break;
                    }
                }
                finally
                {
                    SidebarControl.SideBarItemsSemaphore.Release();
                }
            });
        }

        private static bool IsLibraryOnSidebar(LibraryLocationItem item) => item != null && !item.IsEmpty && item.IsDefaultLocation;

        private async void LoadLibraryIcon(LibraryLocationItem lib)
        {
            lib.IconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(lib.Path, 24u);
            if (lib.IconData != null)
            {
                lib.Icon = await lib.IconData.ToBitmapAsync();
            }
        }

        public void Dispose()
        {
            Libraries.CollectionChanged -= Libraries_CollectionChanged;
        }

        public async Task EnumerateLibrariesAsync()
        {
            try
            {
                await SyncLibrarySideBarItemsUI();
            }
            catch (Exception) // UI Thread not ready yet, so we defer the pervious operation until it is.
            {
                System.Diagnostics.Debug.WriteLine($"RefreshUI Exception");
                // Defer because UI-thread is not ready yet (and DriveItem requires it?)
                CoreApplication.MainView.Activated += EnumerateLibrariesAsync;
            }
        }

        private void RemoveLibrariesSideBarSection()
        {
            try
            {
                var item = (from n in SidebarControl.SideBarItems where n.Text.Equals("SidebarLibraries".GetLocalized()) select n).FirstOrDefault();
                if (!UserSettingsService.AppearanceSettingsService.ShowLibrarySection && item != null)
                {
                    SidebarControl.SideBarItems.Remove(item);
                }
            }
            catch (Exception) { }
        }

        public async void UpdateLibrariesSectionVisibility()
        {
            if (UserSettingsService.AppearanceSettingsService.ShowLibrarySection)
            {
                await EnumerateLibrariesAsync();
            }
            else
            {
                RemoveLibrariesSideBarSection();
            }
        }

        private async void EnumerateLibrariesAsync(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            await SyncLibrarySideBarItemsUI();
            CoreApplication.MainView.Activated -= EnumerateLibrariesAsync;
        }

        public async Task HandleWin32LibraryEvent(ShellLibraryItem library, string oldPath)
        {
            string path = oldPath;
            if (string.IsNullOrEmpty(oldPath))
            {
                path = library?.FullPath;
            }
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var changedLibrary = Libraries.FirstOrDefault(l => string.Equals(l.Path, path, StringComparison.OrdinalIgnoreCase));
                if (changedLibrary != null)
                {
                    Libraries.Remove(changedLibrary);
                }
                // library is null in case it was deleted
                if (library != null && !Libraries.Any(x => x.Path == library.FullPath))
                {
                    Libraries.AddSorted(new LibraryLocationItem(library));
                }
            });
        }

        private async Task SyncLibrarySideBarItemsUI()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await SidebarControl.SideBarItemsSemaphore.WaitAsync();
                try
                {
                    var section = SidebarControl.SideBarItems.FirstOrDefault(x => x.Text == "SidebarLibraries".GetLocalized()) as LocationItem;
                    if (UserSettingsService.AppearanceSettingsService.ShowLibrarySection && section == null)
                    {
                        librarySection = new LocationItem
                        {
                            Text = "SidebarLibraries".GetLocalized(),
                            Section = SectionType.Library,
                            SelectsOnInvoked = false,
                            Icon = await UIHelpers.GetIconResource(Constants.ImageRes.Libraries),
                            ChildItems = new ObservableCollection<INavigationControlItem>()
                        };
                        var index = (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Favorites) ? 1 : 0); // After favorites section
                        SidebarControl.SideBarItems.BeginBulkOperation();
                        SidebarControl.SideBarItems.Insert(Math.Min(index, SidebarControl.SideBarItems.Count), librarySection);
                        SidebarControl.SideBarItems.EndBulkOperation();
                    }
                }
                finally
                {
                    SidebarControl.SideBarItemsSemaphore.Release();
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
            });
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
                    Libraries[Libraries.IndexOf(libItem)] = newLib;
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