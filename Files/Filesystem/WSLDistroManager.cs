using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;

namespace Files.Filesystem
{
    public class WSLDistroManager : ObservableObject
    {
        public WSLDistroManager()
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
                        var distroFolder = await StorageFolder.GetFolderFromPathAsync(@"\\wsl$\");
                        if ((await distroFolder.GetFoldersAsync()).Count != 0)
                        {
                            var section = MainPage.SideBarItems.FirstOrDefault(x => x.Text == "WSL") as LocationItem;

                            section = new LocationItem()
                            {
                                Text = "WSL",
                                
                                Glyph = "\uEC7A",
                                SelectsOnInvoked = false,
                                ChildItems = new ObservableCollection<INavigationControlItem>()
                            };
                            MainPage.SideBarItems.Add(section);


                            foreach (StorageFolder folder in await distroFolder.GetFoldersAsync())
                            {
                                Uri logoURI = null;
                                if (folder.DisplayName.Contains("ubuntu", StringComparison.OrdinalIgnoreCase))
                                {
                                    logoURI = new Uri("ms-appx:///Assets/WSL/ubuntupng.png");
                                }
                                else if (folder.DisplayName.Contains("kali", StringComparison.OrdinalIgnoreCase))
                                {
                                    logoURI = new Uri("ms-appx:///Assets/WSL/kalipng.png");
                                }
                                else if (folder.DisplayName.Contains("debian", StringComparison.OrdinalIgnoreCase))
                                {
                                    logoURI = new Uri("ms-appx:///Assets/WSL/debianpng.png");
                                }
                                else if (folder.DisplayName.Contains("opensuse", StringComparison.OrdinalIgnoreCase))
                                {
                                    logoURI = new Uri("ms-appx:///Assets/WSL/opensusepng.png");
                                }
                                else if (folder.DisplayName.Contains("alpine", StringComparison.OrdinalIgnoreCase))
                                {
                                    logoURI = new Uri("ms-appx:///Assets/WSL/alpinepng.png");
                                }
                                else
                                {
                                    logoURI = new Uri("ms-appx:///Assets/WSL/genericpng.png");
                                }

                                section.ChildItems.Add(new WSLDistroItem()
                                {
                                    Text = folder.DisplayName,
                                    Path = folder.Path,
                                    Logo = logoURI
                                });
                            }

                        }
                    }
                    catch (Exception)
                    {
                        // WSL Not Supported/Enabled
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