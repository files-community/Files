using Files.Helpers;
using Files.ViewModels;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
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
        public InteractionViewModel InteractionViewModel => App.InteractionViewModel;

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
                        if (App.AppSettings.ShowLibrarySection && !MainPage.SideBarItems.Contains(librarySection))
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

                            libraryItems.Clear();
                            libraryItems.Add(AppSettings.DocumentsPath);
                            libraryItems.Add(AppSettings.PicturesPath);
                            libraryItems.Add(AppSettings.MusicPath);
                            libraryItems.Add(AppSettings.VideosPath);

                            for (int i = 0; i < libraryItems.Count(); i++)
                            {
                                string path = libraryItems[i];

                                var item = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(path));
                                var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, item));

                                if (res || (FilesystemResult)FolderHelpers.CheckFolderAccessWithWin32(path))
                                {
                                    var locationItem = new LocationItem
                                    {
                                        Path = path,
                                        Section = SectionType.Library,
                                        Glyph = GlyphHelper.GetItemIcon(path),
                                        Font = InteractionViewModel.FontName,
                                        IsDefaultLocation = true,
                                        Text = res.Result?.DisplayName ?? Path.GetFileName(path.TrimEnd('\\'))
                                    };

                                    librarySection.ChildItems.Insert(i, locationItem);
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
    }
}
