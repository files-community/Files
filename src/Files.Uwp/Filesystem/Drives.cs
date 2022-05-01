using Files.Shared;
using Files.Uwp.DataModels.NavigationControlItems;
using Files.Shared.Enums;
using Files.Backend.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Devices.Portable;
using Windows.Storage;
using Windows.Storage.FileProperties;
using DriveType = Files.Uwp.DataModels.NavigationControlItems.DriveType;
using System.Collections.Specialized;

namespace Files.Uwp.Filesystem
{
    public class DrivesManager : ObservableObject
    {
        private IFolderSizeProvider FolderSizeProvider { get; } = Ioc.Default.GetService<IFolderSizeProvider>();

        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private static readonly ILogger Logger = App.Logger;
        private readonly List<DriveItem> drivesList = new List<DriveItem>();

        public EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

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

        private bool showUserConsentOnInit = false;

        public bool ShowUserConsentOnInit
        {
            get => showUserConsentOnInit;
            set => SetProperty(ref showUserConsentOnInit, value);
        }

        private DeviceWatcher deviceWatcher;
        private bool driveEnumInProgress;

        public DrivesManager()
        {
            SetupDeviceWatcher();
        }

        public async Task EnumerateDrivesAsync()
        {
            driveEnumInProgress = true;

            if (await GetDrivesAsync())
            {
                if (!Drives.Any(d => d.Type != DriveType.Removable && d.Path == "C:\\"))
                {
                    // Show consent dialog if the exception is UnauthorizedAccessException
                    // and the C: drive could not be accessed
                    ShowUserConsentOnInit = true;
                }
            }

            StartDeviceWatcher();

            driveEnumInProgress = false;
        }

        private void SetupDeviceWatcher()
        {
            deviceWatcher = DeviceInformation.CreateWatcher(StorageDevice.GetDeviceSelector());
            deviceWatcher.Added += DeviceAdded;
            deviceWatcher.Removed += DeviceRemoved;
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
        }

        private void StartDeviceWatcher()
        {
            if (deviceWatcher.Status == DeviceWatcherStatus.Created
                || deviceWatcher.Status == DeviceWatcherStatus.Stopped
                || deviceWatcher.Status == DeviceWatcherStatus.Aborted)
            {
                deviceWatcher?.Start();
            }
            else
            {
                DeviceWatcher_EnumerationCompleted();
            }
        }

        private async void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender = null, object args = null)
        {
            System.Diagnostics.Debug.WriteLine("DeviceWatcher_EnumerationCompleted");
            await FolderSizeProvider.CleanCacheAsync();
        }

        private async void DeviceAdded(DeviceWatcher sender, DeviceInformation args)
        {
            var deviceId = args.Id;
            StorageFolder root = null;
            try
            {
                root = StorageDevice.FromId(deviceId);
            }
            catch (Exception ex) when (
                ex is UnauthorizedAccessException
                || ex is ArgumentException)
            {
                Logger.Warn($"{ex.GetType()}: Attempting to add the device, {args.Name}, failed at the StorageFolder initialization step. This device will be ignored. Device ID: {args.Id}");
                return;
            }

            DriveType type;
            try
            {
                // Check if this drive is associated with a drive letter
                var driveAdded = new DriveInfo(root.Path);
                type = GetDriveType(driveAdded);
            }
            catch (ArgumentException)
            {
                type = DriveType.Removable;
            }

            using var thumbnail = (StorageItemThumbnail)await FilesystemTasks.Wrap(() => root.GetThumbnailAsync(ThumbnailMode.SingleItem, 40, ThumbnailOptions.UseCurrentScale).AsTask());
            var driveItem = await DriveItem.CreateFromPropertiesAsync(root, deviceId, type, thumbnail);

            lock (drivesList)
            {
                // If drive already in list, skip.
                if (drivesList.Any(x => x.DeviceID == deviceId ||
                    string.IsNullOrEmpty(root.Path) ? x.Path.Contains(root.Name, StringComparison.OrdinalIgnoreCase) : x.Path == root.Path))
                {
                    return;
                }

                Logger.Info($"Drive added: {driveItem.Path}, {driveItem.Type}");

                drivesList.Add(driveItem);
            }

            DataChanged?.Invoke(SectionType.Drives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, driveItem));

            // Update the collection on the ui-thread.
            DeviceWatcher_EnumerationCompleted();
        }

        private void DeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Logger.Info($"Drive removed: {args.Id}");
            lock (drivesList)
            {
                drivesList.RemoveAll(x => x.DeviceID == args.Id);
            }
            
            DataChanged?.Invoke(SectionType.Drives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            // Update the collection on the ui-thread.
            DeviceWatcher_EnumerationCompleted();
        }

        private async Task<bool> GetDrivesAsync()
        {
            // Flag set if any drive throws UnauthorizedAccessException
            bool unauthorizedAccessDetected = false;

            var drives = DriveInfo.GetDrives();

            foreach (var drive in drives)
            {
                var res = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(drive.Name).AsTask());
                if (res == FileSystemStatusCode.Unauthorized)
                {
                    unauthorizedAccessDetected = true;
                    Logger.Warn($"{res.ErrorCode}: Attempting to add the device, {drive.Name}, failed at the StorageFolder initialization step. This device will be ignored.");
                    continue;
                }
                else if (!res)
                {
                    Logger.Warn($"{res.ErrorCode}: Attempting to add the device, {drive.Name}, failed at the StorageFolder initialization step. This device will be ignored.");
                    continue;
                }

                using var thumbnail = (StorageItemThumbnail)await FilesystemTasks.Wrap(() => res.Result.GetThumbnailAsync(ThumbnailMode.SingleItem, 40, ThumbnailOptions.UseCurrentScale).AsTask());
                var type = GetDriveType(drive);
                var driveItem = await DriveItem.CreateFromPropertiesAsync(res.Result, drive.Name.TrimEnd('\\'), type, thumbnail);

                lock (drivesList)
                {
                    // If drive already in list, skip.
                    if (drivesList.Any(x => x.Path == drive.Name))
                    {
                        continue;
                    }

                    Logger.Info($"Drive added: {driveItem.Path}, {driveItem.Type}");
                    drivesList.Add(driveItem);
                }

                DataChanged?.Invoke(SectionType.Drives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, driveItem));
            }

            return unauthorizedAccessDetected;
        }

        private DriveType GetDriveType(DriveInfo drive)
        {
            DriveType type;
            switch (drive.DriveType)
            {
                case System.IO.DriveType.CDRom:
                    type = DriveType.CDRom;
                    break;

                case System.IO.DriveType.Fixed:
                    type = DriveType.Fixed;
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
                    if (Helpers.PathNormalization.NormalizePath(drive.Name) != Helpers.PathNormalization.NormalizePath("A:") &&
                            Helpers.PathNormalization.NormalizePath(drive.Name) != Helpers.PathNormalization.NormalizePath("B:"))
                    {
                        type = DriveType.Unknown;
                    }
                    else
                    {
                        type = DriveType.FloppyDisk;
                    }
                    break;

                default:
                    type = DriveType.Unknown;
                    break;
            }

            return type;
        }

        public static async Task<StorageFolderWithPath> GetRootFromPathAsync(string devicePath)
        {
            if (!Path.IsPathRooted(devicePath))
            {
                return null;
            }
            var rootPath = Path.GetPathRoot(devicePath);
            if (devicePath.StartsWith("\\\\?\\", StringComparison.Ordinal)) // USB device
            {
                // Check among already discovered drives
                StorageFolder matchingDrive = App.DrivesManager.Drives.FirstOrDefault(x =>
                    Helpers.PathNormalization.NormalizePath(x.Path) == Helpers.PathNormalization.NormalizePath(rootPath))?.Root;
                if (matchingDrive == null)
                {
                    // Check on all removable drives
                    var remDevices = await DeviceInformation.FindAllAsync(StorageDevice.GetDeviceSelector());
                    foreach (var item in remDevices)
                    {
                        try
                        {
                            var root = StorageDevice.FromId(item.Id);
                            if (Helpers.PathNormalization.NormalizePath(rootPath).Replace("\\\\?\\", "", StringComparison.Ordinal) == root.Name.ToUpperInvariant())
                            {
                                matchingDrive = root;
                                break;
                            }
                        }
                        catch (Exception)
                        {
                            // Ignore this..
                        }
                    }
                }
                if (matchingDrive != null)
                {
                    return new StorageFolderWithPath(matchingDrive, rootPath);
                }
            }
            else if (devicePath.StartsWith("\\\\", StringComparison.Ordinal)) // Network share
            {
                rootPath = rootPath.LastIndexOf("\\", StringComparison.Ordinal) > 1 ? rootPath.Substring(0, rootPath.LastIndexOf("\\", StringComparison.Ordinal)) : rootPath; // Remove share name
                return new StorageFolderWithPath(await StorageFolder.GetFolderFromPathAsync(rootPath), rootPath);
            }
            // It's ok to return null here, on normal drives StorageFolder.GetFolderFromPathAsync works
            return null;
        }

        public async Task HandleWin32DriveEvent(DeviceEvent eventType, string deviceId)
        {
            switch (eventType)
            {
                case DeviceEvent.Added:
                    var driveAdded = new DriveInfo(deviceId);
                    var rootAdded = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(deviceId).AsTask());
                    if (!rootAdded)
                    {
                        Logger.Warn($"{rootAdded.ErrorCode}: Attempting to add the device, {deviceId}, failed at the StorageFolder initialization step. This device will be ignored.");
                        return;
                    }

                    DriveItem driveItem;

                    using (var thumbnail = (StorageItemThumbnail)await FilesystemTasks.Wrap(() => rootAdded.Result.GetThumbnailAsync(ThumbnailMode.SingleItem, 40, ThumbnailOptions.UseCurrentScale).AsTask()))
                    {
                        var type = GetDriveType(driveAdded);
                        driveItem = await DriveItem.CreateFromPropertiesAsync(rootAdded, deviceId, type, thumbnail);
                    }

                    lock (drivesList)
                    {
                        // If drive already in list, skip.
                        var matchingDrive = drivesList.FirstOrDefault(x => x.DeviceID == deviceId ||
                            string.IsNullOrEmpty(rootAdded.Result.Path) ? x.Path.Contains(rootAdded.Result.Name, StringComparison.OrdinalIgnoreCase) : x.Path == rootAdded.Result.Path);
                        if (matchingDrive != null)
                        {
                            // Update device id to match drive letter
                            matchingDrive.DeviceID = deviceId;
                            return;
                        }
                        Logger.Info($"Drive added from fulltrust process: {driveItem.Path}, {driveItem.Type}");
                        drivesList.Add(driveItem);
                    }

                    DataChanged?.Invoke(SectionType.Drives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, driveItem));

                    DeviceWatcher_EnumerationCompleted();
                    break;

                case DeviceEvent.Removed:
                    lock (drivesList)
                    {
                        drivesList.RemoveAll(x => x.DeviceID == deviceId);
                    }
                    DataChanged?.Invoke(SectionType.Drives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    DeviceWatcher_EnumerationCompleted();
                    break;

                case DeviceEvent.Inserted:
                case DeviceEvent.Ejected:
                    var rootModified = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(deviceId).AsTask());
                    DriveItem matchingDriveEjected = Drives.FirstOrDefault(x => x.DeviceID == deviceId);
                    if (rootModified && matchingDriveEjected != null)
                    {
                        _ = CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                        {
                            matchingDriveEjected.Root = rootModified.Result;
                            matchingDriveEjected.Text = rootModified.Result.DisplayName;
                            return matchingDriveEjected.UpdatePropertiesAsync();
                        });
                    }
                    break;
            }
        }

        public void Dispose()
        {
            if (deviceWatcher != null)
            {
                if (deviceWatcher.Status == DeviceWatcherStatus.Started || deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted)
                {
                    deviceWatcher.Stop();
                }
            }
        }

        public void ResumeDeviceWatcher()
        {
            if (!driveEnumInProgress)
            {
                this.StartDeviceWatcher();
            }
        }
    }
}