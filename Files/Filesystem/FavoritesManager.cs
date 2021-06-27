using Files.Common;
using Files.DataModels;
using Files.DataModels.NavigationControlItems;
using Files.Extensions;
using Files.Helpers;
using Files.UserControls;
using Files.UserControls.Widgets;
using Files.ViewModels;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Files.Filesystem
{
    public class FavoritesManager : ObservableObject, IDisposable
    {
        public MainViewModel MainViewModel => App.MainViewModel;
        private readonly List<FavoritesLocationItem> drivesList = new List<FavoritesLocationItem>();
        private LocationItem favoritesSection;

        public BulkConcurrentObservableCollection<FavoritesLocationItem> Favorites { get; } = new BulkConcurrentObservableCollection<FavoritesLocationItem>();

        public FavoritesManager()
        {
            Favorites.CollectionChanged += Favorites_CollectionChanged;
        }

        private async void Favorites_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!App.AppSettings.ShowFavoritesSection)
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
                            foreach (var lib in e.OldItems.Cast<FavoritesLocationItem>())
                            {
                                favoritesSection.ChildItems.Remove(lib);
                            }
                            if (e.Action == NotifyCollectionChangedAction.Replace)
                            {
                                goto case NotifyCollectionChangedAction.Add;
                            }
                            break;

                        case NotifyCollectionChangedAction.Reset:
                            foreach (var lib in Favorites.Where(IsFavoritesOnSidebar))
                            {
                                if (!favoritesSection.ChildItems.Any(x => x.Path == lib.Path))
                                {
                                    if (await lib.CheckDefaultSaveFolderAccess())
                                    {
                                        lib.Font = MainViewModel.FontName;
                                        favoritesSection.ChildItems.AddSorted(lib);
                                        this.LoadFavoritesIcon(lib);
                                    }
                                }
                            }
                            foreach (var lib in favoritesSection.ChildItems.ToList())
                            {
                                if (!Favorites.Any(x => x.Path == lib.Path))
                                {
                                    favoritesSection.ChildItems.Remove(lib);
                                }
                            }
                            break;

                        case NotifyCollectionChangedAction.Add:
                            foreach (var lib in e.NewItems.Cast<FavoritesLocationItem>().Where(IsFavoritesOnSidebar))
                            {
                                if (await lib.CheckDefaultSaveFolderAccess())
                                {
                                    lib.Font = MainViewModel.FontName;
                                    favoritesSection.ChildItems.AddSorted(lib);
                                    this.LoadFavoritesIcon(lib);
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
        
        private static bool IsFavoritesOnSidebar(FavoritesLocationItem item) => item != null && !item.IsEmpty && item.IsDefaultLocation;

        private async void LoadFavoritesIcon(FavoritesLocationItem lib)
        {
            lib.IconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(lib.Path, 24u);
            if (lib.IconData != null)
            {
                lib.Icon = await lib.IconData.ToBitmapAsync();
            }
        }

        public void Dispose()
        {
            Favorites.CollectionChanged -= Favorites_CollectionChanged;
        }

        public async Task EnumerateFavoritesAsync()
        {
            await RefreshUI();
        }

        private async Task RefreshUI()
        {
            try
            {
                await SyncFavoritesSideBarItemsUI();
            }
            catch (Exception) // UI Thread not ready yet, so we defer the pervious operation until it is.
            {
                System.Diagnostics.Debug.WriteLine($"RefreshUI Exception");
                // Defer because UI-thread is not ready yet (and DriveItem requires it?)
                CoreApplication.MainView.Activated += RefreshUI;
            }
        }

        private async void RefreshUI(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            await SyncFavoritesSideBarItemsUI();
            CoreApplication.MainView.Activated -= RefreshUI;
        }

        public async Task FavoritesEnumeratorAsync()
        {
            try
            {
                await SyncFavoritesSideBarItemsUI();
            }
            catch (Exception) // UI Thread not ready yet, so we defer the pervious operation until it is.
            {
                System.Diagnostics.Debug.WriteLine($"RefreshUI Exception");
                // Defer because UI-thread is not ready yet (and DriveItem requires it?)
                CoreApplication.MainView.Activated += EnumerateFavoritesAsync;
            }
        }

        public void RemoveFavoritesSideBarSection()
        {
            try
            {
                RemoveFavoriteSideBarItemsUI();
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshUI Exception");
                // Defer because UI-thread is not ready yet (and DriveItem requires it?)
                CoreApplication.MainView.Activated += RemoveFavoritesItems;
            }
        }

        public async void UpdateFavoritesSectionVisibility()
        {
            if (App.AppSettings.ShowFavoritesSection)
            {
                await FavoritesEnumeratorAsync();
            }
            else
            {
                RemoveFavoritesSideBarSection();
            }
        }

        private async void EnumerateFavoritesAsync(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            await SyncFavoritesSideBarItemsUI();
            CoreApplication.MainView.Activated -= EnumerateFavoritesAsync;
        }

        public void UnpinFavoritesSideBarSection()
        {
            var item = (from n in SidebarControl.SideBarItems where n.Text.Equals("SidebarFavorites".GetLocalized()) select n).FirstOrDefault();
            if (SidebarControl.SideBarItems.Contains(item) && item != null)
            {
                SidebarControl.SideBarItems.Remove(item);
                App.AppSettings.ShowFavoritesSection = false;
            }
        }

        private void RemoveFavoritesItems(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            RemoveFavoriteSideBarItemsUI();
            CoreApplication.MainView.Activated -= RemoveFavoritesItems;
        }

        public void RemoveFavoriteSideBarItemsUI()
        {
            SidebarControl.SideBarItems.BeginBulkOperation();

            try
            {
                var item = (from n in SidebarControl.SideBarItems where n.Text.Equals("SidebarFavorites".GetLocalized()) select n).FirstOrDefault();
                if (!App.AppSettings.ShowFavoritesSection && item != null)
                {
                    SidebarControl.SideBarItems.Remove(item);
                }
            }
            catch (Exception)
            { }

            SidebarControl.SideBarItems.EndBulkOperation();
        }

        //public async Task HandleWin32LibraryEvent(ShellLibraryItem library, string oldPath)
        //{
        //    string path = oldPath;
        //    if (string.IsNullOrEmpty(oldPath))
        //    {
        //        path = library?.FullPath;
        //    }
        //    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        //    {
        //        var changedLibrary = Favorites.FirstOrDefault(l => string.Equals(l.Path, path, StringComparison.OrdinalIgnoreCase));
        //        if (changedLibrary != null)
        //        {
        //            Favorites.Remove(changedLibrary);
        //        }
        //        // library is null in case it was deleted
        //        if (library != null)
        //        {
        //            Favorites.AddSorted(new FavoritesLocationItem(library));
        //        }
        //    });
        //}

        private async Task SyncFavoritesSideBarItemsUI()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, (DispatchedHandler)(async () =>
            {
                if (App.AppSettings.ShowFavoritesSection && !SidebarControl.SideBarItems.Contains(favoritesSection))
                {
                    await SidebarControl.SideBarItemsSemaphore.WaitAsync();
                    try
                    {
                        SidebarControl.SideBarItems.BeginBulkOperation();
                        favoritesSection = SidebarControl.SideBarItems.FirstOrDefault(x => x.Text == "SidebarFavorites".GetLocalized()) as FavoritesLocationItem;

                        if (favoritesSection == null)
                        {
                            favoritesSection = new LocationItem
                            {
                                Text = "SidebarFavorites".GetLocalized(),
                                Section = SectionType.Favorites,
                                SelectsOnInvoked = false,
                                Icon = UIHelpers.GetImageForIconOrNull(SidebarPinnedModel.IconResources?.FirstOrDefault(x => x.Index == Constants.ImageRes.Folder).Image),
                                ChildItems = new ObservableCollection<INavigationControlItem>()
                            };
                            SidebarControl.SideBarItems.Insert(SidebarControl.SideBarItems.Count.Equals(0) ? 0 : 1, favoritesSection);
                        }
                        
                        if (favoritesSection != null)
                        {
                            await EnumerateFavoritesAsync();

                            foreach (FavoritesLocationItem drive in Favorites.ToList())
                            {
                                if (!favoritesSection.ChildItems.Contains(drive))
                                {
                                    favoritesSection.ChildItems.Add(drive);

                                    if (drive.ItemType != NavigationControlItemType.Drive)
                                    {
                                        DrivesWidget.ItemsAdded.Add(drive);
                                    }
                                }
                            }

                            foreach (FavoritesLocationItem drive in favoritesSection.ChildItems.ToList())
                            {
                                if (!Favorites.Contains(drive))
                                {
                                    favoritesSection.ChildItems.Remove(drive);
                                    DrivesWidget.ItemsAdded.Remove(drive);
                                }
                            }
                        }
                        SidebarControl.SideBarItems.EndBulkOperation();
                    }
                    finally
                    {
                        SidebarControl.SideBarItemsSemaphore.Release();
                    }
                }
            }));
        }

        public bool TryGetFavorite(string path, out FavoritesLocationItem favorites)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.ToLower().EndsWith(ShellLibraryItem.EXTENSION))
            {
                favorites = null;
                return false;
            }
            favorites = Favorites.FirstOrDefault(l => string.Equals(path, l.Path, StringComparison.OrdinalIgnoreCase));
            return favorites != null;
        }

        public bool IsFavoritePath(string path) => TryGetFavorite(path, out _);

        public (bool result, string reason) CanCreateFavorites(string name)
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
            if (Favorites.Any((item) => string.Equals(name, item.Text, StringComparison.OrdinalIgnoreCase) || string.Equals(name, Path.GetFileNameWithoutExtension(item.Path), StringComparison.OrdinalIgnoreCase)))
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