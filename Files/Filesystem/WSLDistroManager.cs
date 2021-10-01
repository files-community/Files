using Files.DataModels.NavigationControlItems;
using Files.UserControls;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;

namespace Files.Filesystem
{
    public class WSLDistroManager
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
                await SidebarControl.SideBarItemsSemaphore.WaitAsync();
                try
                {
                    var distroFolder = await StorageFolder.GetFolderFromPathAsync(@"\\wsl$\");
                    if ((await distroFolder.GetFoldersAsync()).Count != 0)
                    {
                        var section = SidebarControl.SideBarItems.FirstOrDefault(x => x.Text == "WSL".GetLocalized()) as LocationItem;
                        if (App.AppSettings.ShowWslSection && section == null)
                        {
                            section = new LocationItem()
                            {
                                Text = "WSL".GetLocalized(),
                                Section = SectionType.WSL,
                                SelectsOnInvoked = false,
                                Icon = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/WSL/genericpng.png")),
                                ChildItems = new ObservableCollection<INavigationControlItem>()
                            };
                            var index = (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Favorites) ? 1 : 0) +
                                        (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Library) ? 1 : 0) +
                                        (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Drives) ? 1 : 0) +
                                        (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.CloudDrives) ? 1 : 0) +
                                        (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Network) ? 1 : 0); // After network section
                            SidebarControl.SideBarItems.BeginBulkOperation();
                            SidebarControl.SideBarItems.Insert(Math.Min(index, SidebarControl.SideBarItems.Count), section);
                            SidebarControl.SideBarItems.EndBulkOperation();
                        }

                        if (section != null)
                        {
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

                                if (!section.ChildItems.Any(x => x.Path == folder.Path))
                                {
                                    section.ChildItems.Add(new WslDistroItem()
                                    {
                                        Text = folder.DisplayName,
                                        Path = folder.Path,
                                        Logo = logoURI
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // WSL Not Supported/Enabled
                }
                finally
                {
                    SidebarControl.SideBarItemsSemaphore.Release();
                }
            });
        }

        private void RemoveWslSideBarSection()
        {
            try
            {
                var item = (from n in SidebarControl.SideBarItems where n.Text.Equals("WSL".GetLocalized()) select n).FirstOrDefault();
                if (!App.AppSettings.ShowWslSection && item != null)
                {
                    SidebarControl.SideBarItems.Remove(item);
                }
            }
            catch (Exception)
            { }
        }

        public async void UpdateWslSectionVisibility()
        {
            if (App.AppSettings.ShowWslSection)
            {
                await EnumerateDrivesAsync();
            }
            else
            {
                RemoveWslSideBarSection();
            }
        }
    }
}