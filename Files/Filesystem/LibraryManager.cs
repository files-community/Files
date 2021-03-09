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
                await SyncSideBarItemsUI();
            }
            catch (Exception) // UI Thread not ready yet, so we defer the pervious operation until it is.
            {
                System.Diagnostics.Debug.WriteLine($"RefreshUI Exception");
                // Defer because UI-thread is not ready yet (and DriveItem requires it?)
                CoreApplication.MainView.Activated += EnumerateDrivesAsync;
            }
        }

        private async void EnumerateDrivesAsync(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            await SyncSideBarItemsUI();
            CoreApplication.MainView.Activated -= EnumerateDrivesAsync;
        }

        private LocationItem librarySection;
        private List<string> libraryItems { get; set; } = new List<string>();
        public SettingsViewModel AppSettings => App.AppSettings;

        private async Task SyncSideBarItemsUI()
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
                                    Path = path,
                                    Glyph = GlyphHelper.GetItemIcon(path),
                                    IsDefaultLocation = true,
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