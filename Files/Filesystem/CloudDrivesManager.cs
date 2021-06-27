using Files.Common;
using Files.DataModels;
using Files.DataModels.NavigationControlItems;
using Files.Filesystem.Cloud;
using Files.Helpers;
using Files.UserControls;
using Files.UserControls.Widgets;
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

        private LocationItem cloudDrivesSection;
        public BulkConcurrentObservableCollection<CloudDrivesLocationItem> CloudDriveItems { get; } = new BulkConcurrentObservableCollection<CloudDrivesLocationItem>();

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

                var iconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(provider.SyncFolder, 24);
                if (iconData != null)
                {
                    await CoreApplication.MainView.CoreWindow.DispatcherQueue.EnqueueAsync(async () =>
                    {
                        cloudProviderItem.Icon = await iconData.ToBitmapAsync();
                    });
                }

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

        public async Task CloudDrivesEnumeratorAsync()
        {
            try
            {
                await SyncCloudDrivesSideBarItemsUI();
            }
            catch (Exception) // UI Thread not ready yet, so we defer the pervious operation until it is.
            {
                System.Diagnostics.Debug.WriteLine($"RefreshUI Exception");
                // Defer because UI-thread is not ready yet (and DriveItem requires it?)
                CoreApplication.MainView.Activated += EnumerateCloudDrivesAsync;
            }
        }

        private async void EnumerateCloudDrivesAsync(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            await SyncCloudDrivesSideBarItemsUI();
            CoreApplication.MainView.Activated -= EnumerateCloudDrivesAsync;
        }

        private async Task SyncCloudDrivesSideBarItemsUI()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (App.AppSettings.ShowCloudDrivesSection && !SidebarControl.SideBarItems.Contains(cloudDrivesSection))
                {
                    await SidebarControl.SideBarItemsSemaphore.WaitAsync();
                    try
                    {
                        SidebarControl.SideBarItems.BeginBulkOperation();

                        cloudDrivesSection = SidebarControl.SideBarItems.FirstOrDefault(x => x.Text == "SidebarCloudDrives".GetLocalized()) as LocationItem;
                        if (cloudDrivesSection == null)
                        {
                            cloudDrivesSection = new LocationItem()
                            {
                                Text = "SidebarCloudDrives".GetLocalized(),
                                Section = SectionType.CloudDrives,
                                SelectsOnInvoked = false,
                                Icon = UIHelpers.GetImageForIconOrNull(SidebarPinnedModel.IconResources?.FirstOrDefault(x => x.Index == Constants.ImageRes.Folder).Image),
                                ChildItems = new ObservableCollection<INavigationControlItem>()
                            };
                            SidebarControl.SideBarItems.Insert(SidebarControl.SideBarItems.Count.Equals(0) ? 0 : 1, cloudDrivesSection);
                        }

                        if (cloudDrivesSection != null)
                        {
                            await EnumerateDrivesAsync();

                            foreach (DriveItem drive in Drives.ToList())
                            {
                                if (!cloudDrivesSection.ChildItems.Contains(drive))
                                {
                                    cloudDrivesSection.ChildItems.Add(drive);

                                    if (drive.Type != DriveType.VirtualDrive)
                                    {
                                        DrivesWidget.ItemsAdded.Add(drive);
                                    }
                                }
                            }

                            foreach (DriveItem drive in cloudDrivesSection.ChildItems.ToList())
                            {
                                if (!Drives.Contains(drive))
                                {
                                    cloudDrivesSection.ChildItems.Remove(drive);
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
            });
        }

        public async void UpdateCloudDrivesSectionVisibility()
        {
            if (App.AppSettings.ShowCloudDrivesSection)
            {
                await CloudDrivesEnumeratorAsync();
            }
            else
            {
                RemoveCloudDrivesSideBarSection();
            }
        }

        public void RemoveCloudDrivesSideBarSection()
        {
            try
            {
                RemoveCloudDrivesSideBarItemsUI();
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshUI Exception");
                // Defer because UI-thread is not ready yet (and DriveItem requires it?)
                CoreApplication.MainView.Activated += RemoveCloudDrivesItems;
            }
        }

        private void RemoveCloudDrivesItems(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            RemoveCloudDrivesSideBarItemsUI();
            CoreApplication.MainView.Activated -= RemoveCloudDrivesItems;
        }

        public void RemoveCloudDrivesSideBarItemsUI()
        {
            SidebarControl.SideBarItems.BeginBulkOperation();

            try
            {
                var item = (from n in SidebarControl.SideBarItems where n.Text.Equals("SidebarCloudDrives".GetLocalized()) select n).FirstOrDefault();
                if (!App.AppSettings.ShowCloudDrivesSection && item != null)
                {
                    SidebarControl.SideBarItems.Remove(item);
                }
            }
            catch (Exception)
            { }

            SidebarControl.SideBarItems.EndBulkOperation();
        }

        internal void UnpinCloudDrivesSideBarSection()
        {
            var item = (from n in SidebarControl.SideBarItems where n.Text.Equals("SidebarCloudDrives".GetLocalized()) select n).FirstOrDefault();
            if (SidebarControl.SideBarItems.Contains(item) && item != null)
            {
                SidebarControl.SideBarItems.Remove(item);
                App.AppSettings.ShowCloudDrivesSection = false;
            }
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
            await SyncSideBarItemsUI();
            CoreApplication.MainView.Activated -= RefreshUI;
        }

        private async Task SyncSideBarItemsUI()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (App.AppSettings.ShowCloudDrivesSection)
                {
                    await SidebarControl.SideBarItemsSemaphore.WaitAsync();
                    try
                    {
                        SidebarControl.SideBarItems.BeginBulkOperation();

                        var section = SidebarControl.SideBarItems.FirstOrDefault(x => x.Text == "SidebarCloudDrives".GetLocalized()) as LocationItem;
                        if (section == null && Drives.Any())
                        {
                            section = new LocationItem()
                            {
                                Text = "SidebarCloudDrives".GetLocalized(),
                                Section = SectionType.CloudDrives,
                                SelectsOnInvoked = false,
                                Icon = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/FluentIcons/CloudDrive.png")),
                                ChildItems = new ObservableCollection<INavigationControlItem>(),
                                IsExpanded=false,
                            };
                            SidebarControl.SideBarItems.Add(section);
                        }

                        if (section != null)
                        {
                            foreach (DriveItem drive in Drives.ToList())
                            {
                                if (!section.ChildItems.Contains(drive))
                                {
                                    section.ChildItems.Add(drive);
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
            });
        }
    }
}