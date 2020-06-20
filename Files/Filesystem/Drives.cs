using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Devices.Portable;
using Windows.Storage;
using Windows.UI.Core;

namespace Files.Filesystem
{
    public class DrivesManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public IList<DriveItem> Drives { get; } = new List<DriveItem>();
        public bool ShowUserConsentOnInit { get; set; } = false;
        private DeviceWatcher _deviceWatcher;

        public DrivesManager()
        {
            Task findDrivesTask = null;
            try
            {
                findDrivesTask = GetDrives(Drives);
            }
            catch (AggregateException)
            {
                ShowUserConsentOnInit = true;
            }

            findDrivesTask.ContinueWith((x) =>
            {
                GetVirtualDrivesList(Drives);

                _deviceWatcher = DeviceInformation.CreateWatcher(StorageDevice.GetDeviceSelector());
                _deviceWatcher.Added += DeviceAdded;
                _deviceWatcher.Removed += DeviceRemoved;
                _deviceWatcher.Updated += DeviceUpdated;
                _deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
                _deviceWatcher.Start();
            });
        }

        private async void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            try
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    if (App.sideBarItems.FirstOrDefault(x => x is HeaderTextItem && x.Text == ResourceController.GetTranslation("SidebarDrives")) == null)
                    {
                        App.sideBarItems.Add(new HeaderTextItem() { Text = ResourceController.GetTranslation("SidebarDrives") });
                    }
                    foreach (DriveItem drive in Drives)
                    {
                        if (!App.sideBarItems.Contains(drive))
                        {
                            App.sideBarItems.Add(drive);
                        }
                    }
                    foreach (INavigationControlItem item in App.sideBarItems.ToList())
                    {
                        if (item is DriveItem && !Drives.Contains(item))
                        {
                            App.sideBarItems.Remove(item);
                        }
                    }
                });
            }
            catch (Exception)       // UI Thread not ready yet, so we defer the pervious operation until it is.
            {
                // Defer because UI-thread is not ready yet (and DriveItem requires it?)
                CoreApplication.MainView.Activated += MainView_Activated;
            }
        }

        private async void MainView_Activated(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                if (App.sideBarItems.FirstOrDefault(x => x is HeaderTextItem && x.Text == ResourceController.GetTranslation("SidebarDrives")) == null)
                {
                    App.sideBarItems.Add(new HeaderTextItem() { Text = ResourceController.GetTranslation("SidebarDrives") });
                }
                foreach (DriveItem drive in Drives)
                {
                    if (!App.sideBarItems.Contains(drive))
                    {
                        App.sideBarItems.Add(drive);
                    }
                }
                foreach (INavigationControlItem item in App.sideBarItems.ToList())
                {
                    if (item is DriveItem && !Drives.Contains(item))
                    {
                        App.sideBarItems.Remove(item);
                    }
                }
            });
            CoreApplication.MainView.Activated -= MainView_Activated;
        }

        private async void DeviceAdded(DeviceWatcher sender, DeviceInformation args)
        {
            var deviceId = args.Id;
            StorageFolder root = null;
            try
            {
                root = StorageDevice.FromId(deviceId);
            }
            catch (UnauthorizedAccessException)
            {
                Logger.Warn($"UnauthorizedAccessException: Attemting to add the device, {args.Name}, failed at the StorageFolder initialization step. This device will be ignored. Device ID: {args.Id}");
                return;
            }

            // If drive already in list, skip.
            if (Drives.Any(x => x.Path == root.Name))
            {
                return;
            }

            var driveItem = new DriveItem(root, DriveType.Removable);

            Logger.Info($"Drive added: {driveItem.Path}, {driveItem.Type}");

            // Update the collection on the ui-thread.
            try
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    Drives.Add(driveItem);
                    DeviceWatcher_EnumerationCompleted(null, null);
                });
            }
            catch (Exception)
            {
                // Ui-Thread not yet created.
                Drives.Add(driveItem);
            }
        }

        private async void DeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            var drives = DriveInfo.GetDrives().Select(x => x.Name);

            foreach (var drive in Drives)
            {
                if (drive.Type == DriveType.VirtualDrive || drives.Contains(drive.Path))
                {
                    continue;
                }

                Logger.Info($"Drive removed: {drive.Path}");

                // Update the collection on the ui-thread.
                try
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                    {
                        Drives.Remove(drive);
                        DeviceWatcher_EnumerationCompleted(null, null);
                    });
                }
                catch (Exception)
                {
                    // Ui-Thread not yet created.
                    Drives.Remove(drive);
                }
                return;
            }
        }

        private void DeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Debug.WriteLine("Devices updated");
        }

        private async Task GetDrives(IList<DriveItem> list)
        {
            var drives = DriveInfo.GetDrives().ToList();

            var remDevices = await DeviceInformation.FindAllAsync(StorageDevice.GetDeviceSelector());
            List<string> supportedDevicesNames = new List<string>();
            foreach (var item in remDevices)
            {
                try
                {
                    supportedDevicesNames.Add(StorageDevice.FromId(item.Id).Name);
                }
                catch (Exception e)
                {
                    Logger.Warn("Can't get storage device name: " + e.Message + ", skipping...");
                }
            }

            foreach (DriveInfo driveInfo in drives.ToList())
            {
                if (!supportedDevicesNames.Contains(driveInfo.Name) && driveInfo.DriveType == System.IO.DriveType.Removable)
                {
                    drives.Remove(driveInfo);
                }
            }

            foreach (var drive in drives)
            {
                // If drive already in list, skip.
                if (list.Any(x => x.Path == drive.Name))
                {
                    continue;
                }

                var folder = Task.Run(async () => await StorageFolder.GetFolderFromPathAsync(drive.Name)).Result;

                DriveType type = DriveType.Unknown;

                switch (drive.DriveType)
                {
                    case System.IO.DriveType.CDRom:
                        type = DriveType.CDRom;
                        break;

                    case System.IO.DriveType.Fixed:
                        if (InstanceTabsView.NormalizePath(drive.Name) != InstanceTabsView.NormalizePath("A:")
                            && InstanceTabsView.NormalizePath(drive.Name) !=
                            InstanceTabsView.NormalizePath("B:"))
                        {
                            type = DriveType.Fixed;
                        }
                        else
                        {
                            type = DriveType.FloppyDisk;
                        }
                        break;

                    case System.IO.DriveType.Network:
                        type = DriveType.Network;
                        break;

                    case System.IO.DriveType.NoRootDirectory:
                        type = DriveType.NoRootDirectory;
                        break;

                    case System.IO.DriveType.Ram:
                        type = DriveType.Ram;
                        break;

                    case System.IO.DriveType.Removable:
                        type = DriveType.Removable;
                        break;

                    case System.IO.DriveType.Unknown:
                        type = DriveType.Unknown;
                        break;

                    default:
                        type = DriveType.Unknown;
                        break;
                }

                var driveItem = new DriveItem(folder, type);

                Logger.Info($"Drive added: {driveItem.Path}, {driveItem.Type}");

                list.Add(driveItem);
            }
        }

        private void GetVirtualDrivesList(IList<DriveItem> list)
        {
            var oneDriveItem = new DriveItem()
            {
                Text = "OneDrive",
                Path = App.AppSettings.OneDrivePath,
                Type = DriveType.VirtualDrive,
            };

            var setting = ApplicationData.Current.LocalSettings.Values["PinOneDrive"];
            if (setting == null || (bool)setting == true)
            {
                list.Add(oneDriveItem);
            }
        }

        public void Dispose()
        {
            if (_deviceWatcher.Status == DeviceWatcherStatus.Started || _deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted)
            {
                _deviceWatcher.Stop();
            }
        }
    }
}