using Files.DataModels;
using Files.Helpers;
using Files.ViewModels;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;

namespace Files.Filesystem
{
    public class LibraryManager : ObservableObject
    {
        public LibraryManager()
        {

        }

        public async Task EnumerateDrivesAsync()
        {
            try
            {
                await SyncLibrarySideBarItemsUI();
            }
            catch (Exception) // UI Thread not ready yet, so we defer the pervious operation until it is.
            {
                System.Diagnostics.Debug.WriteLine($"RefreshUI Exception");
                // Defer because UI-thread is not ready yet (and DriveItem requires it?)
                CoreApplication.MainView.Activated += EnumerateDrivesAsync;
            }
        }

        public async Task RemoveEnumerateDrivesAsync()
        {
            try
            {
                await RemoveLibrarySideBarItemsUI();
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshUI Exception");
                // Defer because UI-thread is not ready yet (and DriveItem requires it?)
                CoreApplication.MainView.Activated += RemoveEnumerateDrivesAsync;
            }
        }

        private async void EnumerateDrivesAsync(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            await SyncLibrarySideBarItemsUI();
            CoreApplication.MainView.Activated -= EnumerateDrivesAsync;
        }

        private async void RemoveEnumerateDrivesAsync(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            await RemoveLibrarySideBarItemsUI();
            CoreApplication.MainView.Activated -= RemoveEnumerateDrivesAsync;
        }

        private LocationItem librarySection;
        private List<string> libraryItems { get; set; } = new List<string>();
        public SettingsViewModel AppSettings => App.AppSettings;

        public async Task RemoveLibrarySideBarItemsUI()
        {
            MainPage.SideBarItems.BeginBulkOperation();

            try
            {
                var item = (from n in MainPage.SideBarItems where n.Text.Equals("SidebarLibraries".GetLocalized()) select n).FirstOrDefault();
                if (!App.AppSettings.ShowLibrarySection && MainPage.SideBarItems.Contains(librarySection))
                {
                    MainPage.SideBarItems.Remove(item);
                }
            }
            catch (Exception)
            {

            }

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
                        if (App.AppSettings.ShowLibrarySection && !MainPage.SideBarItems.Contains(librarySection))
                        {
                            librarySection = new LocationItem()
                            {
                                Text = "SidebarLibraries".GetLocalized(),
                                Font = App.Current.Resources["OldFluentUIGlyphs"] as FontFamily,
                                Glyph = "\uEC13",
                                ChildItems = new ObservableCollection<INavigationControlItem>()
                            };
                            MainPage.SideBarItems.Add(librarySection);

                            libraryItems.Add(AppSettings.DesktopPath);
                            libraryItems.Add(AppSettings.DownloadsPath);
                            libraryItems.Add(AppSettings.DocumentsPath);
                            libraryItems.Add(AppSettings.PicturesPath);
                            libraryItems.Add(AppSettings.MusicPath);
                            libraryItems.Add(AppSettings.VideosPath);

                            for (int i = 0; i < libraryItems.Count(); i++)
                            {
                                string path = libraryItems[i];

                                var locationItem = new LocationItem
                                {
                                    Font = App.Current.Resources["FluentGlyphs"] as FontFamily,
                                    Path = path,
                                    Glyph = GlyphHelper.GetItemIcon(path),
                                    IsDefaultLocation = false,
                                    Text = Path.GetFileName(path.TrimEnd('\\'))
                                };

                                librarySection.ChildItems.Insert(i, locationItem);
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
    }
}