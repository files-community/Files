using Files.Extensions;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Geolocation;
using Windows.Foundation.Collections;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Services.Maps;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Files.ViewModels.Properties
{
    public class FileProperties : BaseProperties
    {
        public ListedItem Item { get; }

        private IProgress<float> hashProgress;

        public FileProperties(SelectedItemsPropertiesViewModel viewModel, CancellationTokenSource tokenSource, CoreDispatcher coreDispatcher, IProgress<float> hashProgress, ListedItem item, IShellPage instance)
        {
            ViewModel = viewModel;
            TokenSource = tokenSource;
            Dispatcher = coreDispatcher;
            Item = item;
            AppInstance = instance;
            this.hashProgress = hashProgress;

            GetBaseProperties();

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        public override void GetBaseProperties()
        {
            if (Item != null)
            {
                ViewModel.ItemName = Item.ItemName;
                ViewModel.OriginalItemName = Item.ItemName;
                ViewModel.ItemType = Item.ItemType;
                ViewModel.ItemPath = (Item as RecycleBinItem)?.ItemOriginalFolder ??
                    (Path.IsPathRooted(Item.ItemPath) ? Path.GetDirectoryName(Item.ItemPath) : Item.ItemPath);
                ViewModel.ItemModifiedTimestamp = Item.ItemDateModified;
                ViewModel.ItemCreatedTimestamp = Item.ItemDateCreated;
                ViewModel.LoadCustomIcon = Item.LoadCustomIcon;
                ViewModel.CustomIconSource = Item.CustomIconSource;
                ViewModel.LoadFileIcon = Item.LoadFileIcon;

                if (Item.IsShortcutItem)
                {
                    var shortcutItem = (ShortcutItem)Item;

                    var isApplication = !string.IsNullOrWhiteSpace(shortcutItem.TargetPath) &&
                        (shortcutItem.TargetPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                            || shortcutItem.TargetPath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase)
                            || shortcutItem.TargetPath.EndsWith(".bat", StringComparison.OrdinalIgnoreCase));

                    ViewModel.ShortcutItemType = isApplication ? "Application".GetLocalized() :
                        Item.IsLinkItem ? "PropertiesShortcutTypeLink".GetLocalized() : "PropertiesShortcutTypeFile".GetLocalized();
                    ViewModel.ShortcutItemPath = shortcutItem.TargetPath;
                    ViewModel.IsShortcutItemPathReadOnly = shortcutItem.IsSymLink;
                    ViewModel.ShortcutItemWorkingDir = shortcutItem.WorkingDirectory;
                    ViewModel.ShortcutItemWorkingDirVisibility = Item.IsLinkItem || shortcutItem.IsSymLink ? false : true;
                    ViewModel.ShortcutItemArguments = shortcutItem.Arguments;
                    ViewModel.ShortcutItemArgumentsVisibility = Item.IsLinkItem || shortcutItem.IsSymLink ? false : true;
                    ViewModel.IsSelectedItemShortcut = ".lnk".Equals(Item.FileExtension, StringComparison.OrdinalIgnoreCase);
                    ViewModel.ShortcutItemOpenLinkCommand = new RelayCommand(async () =>
                    {
                        if (Item.IsLinkItem)
                        {
                            var tmpItem = (ShortcutItem)Item;
                            await Win32Helpers.InvokeWin32ComponentAsync(ViewModel.ShortcutItemPath, AppInstance, ViewModel.ShortcutItemArguments, tmpItem.RunAsAdmin, ViewModel.ShortcutItemWorkingDir);
                        }
                        else
                        {
                            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(
                                () => NavigationHelpers.OpenPathInNewTab(Path.GetDirectoryName(ViewModel.ShortcutItemPath)));
                        }
                    }, () =>
                    {
                        return !string.IsNullOrWhiteSpace(ViewModel.ShortcutItemPath);
                    });
                }
            }
        }

        public override async void GetSpecialProperties()
        {
            ViewModel.IsReadOnly = NativeFileOperationsHelper.HasFileAttribute(
                Item.ItemPath, System.IO.FileAttributes.ReadOnly);
            ViewModel.IsHidden = NativeFileOperationsHelper.HasFileAttribute(
                Item.ItemPath, System.IO.FileAttributes.Hidden);

            ViewModel.ItemSizeVisibility = true;
            ViewModel.ItemSize = Item.FileSizeBytes.ToLongSizeString();

            var fileIconData = await FileThumbnailHelper.LoadIconFromPathAsync(Item.ItemPath, 80, Windows.Storage.FileProperties.ThumbnailMode.SingleItem);
            if (fileIconData != null)
            {
                ViewModel.IconData = fileIconData;
                ViewModel.LoadUnknownTypeGlyph = false;
                ViewModel.LoadFileIcon = true;
            }

            if (Item.IsShortcutItem)
            {
                ViewModel.ItemCreatedTimestamp = Item.ItemDateCreated;
                ViewModel.ItemAccessedTimestamp = Item.ItemDateAccessed;
                ViewModel.LoadLinkIcon = Item.LoadWebShortcutGlyph;
                if (Item.IsLinkItem || string.IsNullOrWhiteSpace(((ShortcutItem)Item).TargetPath))
                {
                    // Can't show any other property
                    return;
                }
            }

            BaseStorageFile file = await AppInstance.FilesystemViewModel.GetFileFromPathAsync((Item as ShortcutItem)?.TargetPath ?? Item.ItemPath);
            if (file == null)
            {
                // Could not access file, can't show any other property
                return;
            }

            if (Item.IsShortcutItem)
            {
                // Can't show any other property
                return;
            }

            if (file.Properties != null)
            {
                GetOtherProperties(file.Properties);
            }

            // Get file MD5 hash
            var hashAlgTypeName = HashAlgorithmNames.Md5;
            ViewModel.ItemMD5HashProgressVisibility = Visibility.Visible;
            ViewModel.ItemMD5HashVisibility = Visibility.Visible;
            try
            {
                ViewModel.ItemMD5Hash = await GetHashForFileAsync(Item, hashAlgTypeName, TokenSource.Token, hashProgress, AppInstance);
            }
            catch (Exception ex)
            {
                App.Logger.Warn(ex, ex.Message);
                ViewModel.ItemMD5HashCalcError = true;
            }
        }

        public async void GetSystemFileProperties()
        {
            BaseStorageFile file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(Item.ItemPath));
            if (file == null)
            {
                // Could not access file, can't show any other property
                return;
            }

            var list = await FileProperty.RetrieveAndInitializePropertiesAsync(file);

            list.Find(x => x.ID == "address").Value = await GetAddressFromCoordinatesAsync((double?)list.Find(x => x.Property == "System.GPS.LatitudeDecimal").Value,
                                                                                           (double?)list.Find(x => x.Property == "System.GPS.LongitudeDecimal").Value);

            var query = list
                .Where(fileProp => !(fileProp.Value == null && fileProp.IsReadOnly))
                .GroupBy(fileProp => fileProp.SectionResource)
                .Select(group => new FilePropertySection(group) { Key = group.Key })
                .Where(section => !section.All(fileProp => fileProp.Value == null))
                .OrderBy(group => group.Priority);
            ViewModel.PropertySections = new ObservableCollection<FilePropertySection>(query);
            ViewModel.FileProperties = new ObservableCollection<FileProperty>(list.Where(i => i.Value != null));
        }

        public static async Task<string> GetAddressFromCoordinatesAsync(double? Lat, double? Lon)
        {
            if (!Lat.HasValue || !Lon.HasValue)
            {
                return null;
            }

            JObject obj;
            try
            {
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(@"ms-appx:///Resources/BingMapsKey.txt"));
                var lines = await FileIO.ReadTextAsync(file);
                obj = JObject.Parse(lines);
            }
            catch (Exception)
            {
                return null;
            }

            MapService.ServiceToken = (string)obj.SelectToken("key");

            BasicGeoposition location = new BasicGeoposition();
            location.Latitude = Lat.Value;
            location.Longitude = Lon.Value;
            Geopoint pointToReverseGeocode = new Geopoint(location);

            // Reverse geocode the specified geographic location.

            var result = await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode);
            return result?.Locations?.FirstOrDefault()?.DisplayName;
        }

        public async Task SyncPropertyChangesAsync()
        {
            BaseStorageFile file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(Item.ItemPath));
            if (file == null)
            {
                // Could not access file, can't save properties
                return;
            }

            var failedProperties = "";
            foreach (var group in ViewModel.PropertySections)
            {
                foreach (FileProperty prop in group)
                {
                    if (!prop.IsReadOnly && prop.Modified)
                    {
                        var newDict = new Dictionary<string, object>();
                        newDict.Add(prop.Property, prop.Value);

                        try
                        {
                            if (file.Properties != null)
                            {
                                await file.Properties.SavePropertiesAsync(newDict);
                            }
                        }
                        catch
                        {
                            failedProperties += $"{prop.Name}\n";
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(failedProperties))
            {
                throw new Exception($"The following properties failed to save: {failedProperties}");
            }
        }

        /// <summary>
        /// This function goes through ever read-write property saved, then syncs it
        /// </summary>
        /// <returns></returns>
        public async Task ClearPropertiesAsync()
        {
            var failedProperties = new List<string>();
            BaseStorageFile file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(Item.ItemPath));
            if (file == null)
            {
                return;
            }

            foreach (var group in ViewModel.PropertySections)
            {
                foreach (FileProperty prop in group)
                {
                    if (!prop.IsReadOnly)
                    {
                        var newDict = new Dictionary<string, object>();
                        newDict.Add(prop.Property, null);

                        try
                        {
                            if (file.Properties != null)
                            {
                                await file.Properties.SavePropertiesAsync(newDict);
                            }
                        }
                        catch
                        {
                            failedProperties.Add(prop.Name);
                        }
                    }
                }
            }

            GetSystemFileProperties();
        }

        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsReadOnly":
                    if (ViewModel.IsReadOnly)
                    {
                        NativeFileOperationsHelper.SetFileAttribute(
                            Item.ItemPath, System.IO.FileAttributes.ReadOnly);
                    }
                    else
                    {
                        NativeFileOperationsHelper.UnsetFileAttribute(
                            Item.ItemPath, System.IO.FileAttributes.ReadOnly);
                    }
                    break;

                case "IsHidden":
                    if (ViewModel.IsHidden)
                    {
                        NativeFileOperationsHelper.SetFileAttribute(
                            Item.ItemPath, System.IO.FileAttributes.Hidden);
                    }
                    else
                    {
                        NativeFileOperationsHelper.UnsetFileAttribute(
                            Item.ItemPath, System.IO.FileAttributes.Hidden);
                    }
                    break;

                case "ShortcutItemPath":
                case "ShortcutItemWorkingDir":
                case "ShortcutItemArguments":
                    var tmpItem = (ShortcutItem)Item;
                    if (string.IsNullOrWhiteSpace(ViewModel.ShortcutItemPath))
                        return;

                    var connection = await AppServiceConnectionHelper.Instance;
                    if (connection != null)
                    {
                        var value = new ValueSet()
                        {
                            { "Arguments", "FileOperation" },
                            { "fileop", "UpdateLink" },
                            { "filepath", Item.ItemPath },
                            { "targetpath", ViewModel.ShortcutItemPath },
                            { "arguments", ViewModel.ShortcutItemArguments },
                            { "workingdir", ViewModel.ShortcutItemWorkingDir },
                            { "runasadmin", tmpItem.RunAsAdmin },
                        };
                        await connection.SendMessageAsync(value);
                    }
                    break;
            }
        }

        private async Task<string> GetHashForFileAsync(ListedItem fileItem, string nameOfAlg, CancellationToken token, IProgress<float> progress, IShellPage associatedInstance)
        {
            HashAlgorithmProvider algorithmProvider = HashAlgorithmProvider.OpenAlgorithm(nameOfAlg);
            BaseStorageFile file = await StorageHelpers.ToStorageItem<BaseStorageFile>((fileItem as ShortcutItem)?.TargetPath ?? fileItem.ItemPath, associatedInstance);
            if (file == null)
            {
                return "";
            }

            Stream stream = await FilesystemTasks.Wrap(() => file.OpenStreamForReadAsync());
            if (stream == null)
            {
                return "";
            }

            uint capacity;
            var inputStream = stream.AsInputStream();
            bool isProgressSupported = false;

            try
            {
                var cap = (long)(0.5 * stream.Length) / 100;
                if (cap >= uint.MaxValue)
                {
                    capacity = uint.MaxValue;
                }
                else
                {
                    capacity = Convert.ToUInt32(cap);
                }
                isProgressSupported = true;
            }
            catch (NotSupportedException)
            {
                capacity = 64 * 1024;
            }

            Windows.Storage.Streams.Buffer buffer = new Windows.Storage.Streams.Buffer(capacity);
            var hash = algorithmProvider.CreateHash();
            while (!token.IsCancellationRequested)
            {
                await inputStream.ReadAsync(buffer, capacity, InputStreamOptions.None);
                if (buffer.Length > 0)
                {
                    hash.Append(buffer);
                }
                else
                {
                    break;
                }
                if (stream.Length > 0)
                {
                    progress?.Report(isProgressSupported ? (float)stream.Position / stream.Length * 100.0f : 20);
                }
            }
            inputStream.Dispose();
            stream.Dispose();
            if (token.IsCancellationRequested)
            {
                return "";
            }
            return CryptographicBuffer.EncodeToHexString(hash.GetValueAndReset()).ToLowerInvariant();
        }
    }
}