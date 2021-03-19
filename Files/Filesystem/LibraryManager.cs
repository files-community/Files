using Files.Helpers;
using Files.ViewModels;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private List<LibraryLocationItem> libraryItems { get; set; } = new List<LibraryLocationItem>();

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
                            var libs = await LibraryHelper.Instance.ListUserLibraries(false);
                            libraryItems.AddRange(libs.Where(l => l.IsDefaultLocation));

                            for (int i = 0; i < libraryItems.Count; i++)
                            {
                                var lib = libraryItems[i];
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
    }
}
