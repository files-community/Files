using Files.Common;
using Files.Helpers;
using Files.ViewModels;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;

namespace Files.Filesystem
{
    public class LibraryManager : ObservableObject
    {
        public InteractionViewModel InteractionViewModel => App.InteractionViewModel;

        public LibraryManager()
        {
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

        public void RemoveLibrariesSideBarSection()
        {
            try
            {
                RemoveLibrarySideBarItemsUI();
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshUI Exception");
                // Defer because UI-thread is not ready yet (and DriveItem requires it?)
                CoreApplication.MainView.Activated += RemoveLibraryItems;
            }
        }

        private async void EnumerateLibrariesAsync(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            await SyncLibrarySideBarItemsUI();
            CoreApplication.MainView.Activated -= EnumerateLibrariesAsync;
        }

        private void RemoveLibraryItems(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            RemoveLibrarySideBarItemsUI();
            CoreApplication.MainView.Activated -= RemoveLibraryItems;
        }

        private LocationItem librarySection;

        public BulkConcurrentObservableCollection<LibraryLocationItem> Libraries { get; } = new BulkConcurrentObservableCollection<LibraryLocationItem>();

        public SettingsViewModel AppSettings => App.AppSettings;

        public void RemoveLibrarySideBarItemsUI()
        {
            MainPage.SideBarItems.BeginBulkOperation();

            try
            {
                var item = (from n in MainPage.SideBarItems where n.Text.Equals("SidebarLibraries".GetLocalized()) select n).FirstOrDefault();
                if (!App.AppSettings.ShowLibrarySection && item != null)
                {
                    MainPage.SideBarItems.Remove(item);
                }
            }
            catch (Exception)
            { }

            MainPage.SideBarItems.EndBulkOperation();
        }

        private async Task SyncLibrarySideBarItemsUI()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await MainPage.SideBarItemsSemaphore.WaitAsync();
                try
                {
                    MainPage.SideBarItems.BeginBulkOperation();

                    try
                    {
                        if (App.AppSettings.ShowLibrarySection)
                        {
                            if (librarySection == null || !MainPage.SideBarItems.Contains(librarySection))
                            {
                                librarySection = new LocationItem()
                                {
                                    Text = "SidebarLibraries".GetLocalized(),
                                    Section = SectionType.Library,
                                    Font = App.Current.Resources["OldFluentUIGlyphs"] as FontFamily,
                                    Glyph = "\uEC13",
                                    SelectsOnInvoked = false,
                                    ChildItems = new ObservableCollection<INavigationControlItem>()
                                };
                                MainPage.SideBarItems.Insert(1, librarySection);
                            }
                            else
                            {
                                librarySection.ChildItems.Clear();
                            }

                            Libraries.BeginBulkOperation();
                            Libraries.Clear();
                            var libs = await LibraryHelper.ListUserLibraries();
                            if (libs != null)
                            {
                                foreach (var lib in libs)
                                {
                                    Libraries.Add(lib);
                                }
                            }
                            Libraries.EndBulkOperation();

                            var librariesOnSidebar = Libraries.Where(l => !l.IsEmpty && l.IsDefaultLocation).ToList();
                            for (int i = 0; i < librariesOnSidebar.Count; i++)
                            {
                                var lib = librariesOnSidebar[i];
                                if (await lib.CheckDefaultSaveFolderAccess())
                                {
                                    lib.Font = InteractionViewModel.FontName;
                                    librarySection.ChildItems.Insert(i, lib);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }

                    MainPage.SideBarItems.EndBulkOperation();
                }
                finally
                {
                    MainPage.SideBarItemsSemaphore.Release();
                }
            });
        }

        public bool TryGetLibrary(string path, out LibraryLocationItem library)
        {
            if (string.IsNullOrWhiteSpace(path) || Path.GetExtension(path) != ShellLibraryItem.EXTENSION)
            {
                library = null;
                return false;
            }
            library = Libraries.FirstOrDefault(l => string.Equals(path, l.Path, StringComparison.OrdinalIgnoreCase));
            return library != null;
        }

        public bool IsLibraryPath(string path) => TryGetLibrary(path, out _);
    }
}
