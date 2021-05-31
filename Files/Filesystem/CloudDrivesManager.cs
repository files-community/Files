using Files.Common;
using Files.DataModels.NavigationControlItems;
using Files.Filesystem.Cloud;
using Files.Helpers;
using Files.UserControls;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Files.Filesystem
{
    public class CloudDrivesManager : ObservableObject
    {
        private static readonly Logger Logger = App.Logger;
        private readonly List<DriveItem> drivesList = new List<DriveItem>();

        public IReadOnlyList<DriveItem> Drives
        {
            get
            {
                lock (drivesList)
                {
                    return drivesList.ToList().AsReadOnly();
                }
            }
        }

        public CloudDrivesManager()
        {
        }

        public async Task EnumerateDrivesAsync()
        {
            var cloudProviderController = new CloudProviderController();
            var cloudProviders = await cloudProviderController.DetectInstalledCloudProvidersAsync();

            foreach (var provider in cloudProviders)
            {
                Logger.Info($"Adding cloud provider \"{provider.Name}\" mapped to {provider.SyncFolder}");
                var cloudProviderItem = new DriveItem()
                {
                    Text = provider.Name,
                    Path = provider.SyncFolder,
                    Type = DriveType.CloudDrive,
                };

                await CoreApplication.MainView.CoreWindow.DispatcherQueue.EnqueueAsync(async () =>
                {
                    cloudProviderItem.Icon = await (await FileThumbnailHelper.LoadIconWithoutOverlayAsync(provider.SyncFolder, 24)).ToBitmapAsync();
                });

                lock (drivesList)
                {
                    if (!drivesList.Any(x => x.Path == cloudProviderItem.Path))
                    {
                        drivesList.Add(cloudProviderItem);
                    }
                }
            }

            await RefreshUI();
        }

        private async Task RefreshUI()
        {
            try
            {
                await SyncSideBarItemsUI();
            }
            catch (Exception ex) // UI Thread not ready yet, so we defer the previous operation until it is.
            {
                Logger.Warn(ex, "UI thread not ready yet");
                System.Diagnostics.Debug.WriteLine($"RefreshUI Exception");
                // Defer because UI-thread is not ready yet (and DriveItem requires it?)
                CoreApplication.MainView.Activated += RefreshUI;
            }
        }

        private async void RefreshUI(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            CoreApplication.MainView.Activated -= RefreshUI;
            await SyncSideBarItemsUI();
        }

        private async Task SyncSideBarItemsUI()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await SidebarControl.SideBarItemsSemaphore.WaitAsync();
                try
                {
                    SidebarControl.SideBarItems.BeginBulkOperation();

                    var section = SidebarControl.SideBarItems.FirstOrDefault(x => x.Text == "SidebarCloudDrives".GetLocalized()) as LocationItem;
                    if (section == null)
                    {
                        section = new LocationItem()
                        {
                            Text = "SidebarCloudDrives".GetLocalized(),
                            Section = SectionType.CloudDrives,
                            SelectsOnInvoked = false,
                            Icon = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/FluentIcons/CloudDrive.png")),
                            ChildItems = new ObservableCollection<INavigationControlItem>()
                        };
                        SidebarControl.SideBarItems.Add(section);
                    }

                    foreach (DriveItem drive in Drives.ToList())
                    {
                        if (!section.ChildItems.Contains(drive))
                        {
                            section.ChildItems.Add(drive);
                        }
                    }

                    SidebarControl.SideBarItems.EndBulkOperation();
                }
                finally
                {
                    SidebarControl.SideBarItemsSemaphore.Release();
                }
            });
        }
    }
}