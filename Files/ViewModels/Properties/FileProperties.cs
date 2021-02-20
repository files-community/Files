using ByteSizeLib;
using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.Extensions;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation.Collections;
using Windows.Security.Cryptography.Core;
using Windows.Services.Maps;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Files.ViewModels.Properties
{
    public class FileProperties : BaseProperties
    {
        private ProgressBar ProgressBar;

        public ListedItem Item { get; }

        public FileProperties(SelectedItemsPropertiesViewModel viewModel, CancellationTokenSource tokenSource, CoreDispatcher coreDispatcher, ProgressBar progressBar, ListedItem item, IShellPage instance)
        {
            ViewModel = viewModel;
            TokenSource = tokenSource;
            ProgressBar = progressBar;
            Dispatcher = coreDispatcher;
            Item = item;
            AppInstance = instance;

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
                //ViewModel.FileIconSource = Item.FileImage;
                ViewModel.LoadFolderGlyph = Item.LoadFolderGlyph;
                ViewModel.LoadUnknownTypeGlyph = Item.LoadUnknownTypeGlyph;
                ViewModel.LoadFileIcon = Item.LoadFileIcon;

                if (Item.IsShortcutItem)
                {
                    var shortcutItem = (ShortcutItem)Item;

                    var isApplication = !string.IsNullOrWhiteSpace(shortcutItem.TargetPath) &&
                        (shortcutItem.TargetPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                            || shortcutItem.TargetPath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase)
                            || shortcutItem.TargetPath.EndsWith(".bat", StringComparison.OrdinalIgnoreCase));

                    ViewModel.ShortcutItemType = isApplication ? "PropertiesShortcutTypeApplication".GetLocalized() :
                        Item.IsLinkItem ? "PropertiesShortcutTypeLink".GetLocalized() : "PropertiesShortcutTypeFile".GetLocalized();
                    ViewModel.ShortcutItemPath = shortcutItem.TargetPath;
                    ViewModel.ShortcutItemWorkingDir = shortcutItem.WorkingDirectory;
                    ViewModel.ShortcutItemWorkingDirVisibility = Item.IsLinkItem ? Visibility.Collapsed : Visibility.Visible;
                    ViewModel.ShortcutItemArguments = shortcutItem.Arguments;
                    ViewModel.ShortcutItemArgumentsVisibility = Item.IsLinkItem ? Visibility.Collapsed : Visibility.Visible;
                    ViewModel.IsSelectedItemShortcut = Item.FileExtension.Equals(".lnk", StringComparison.OrdinalIgnoreCase);
                    ViewModel.ShortcutItemOpenLinkCommand = new RelayCommand(async () =>
                    {
                        if (Item.IsLinkItem)
                        {
                            var tmpItem = (ShortcutItem)Item;
                            await AppInstance.InteractionOperations.InvokeWin32ComponentAsync(ViewModel.ShortcutItemPath, ViewModel.ShortcutItemArguments, tmpItem.RunAsAdmin, ViewModel.ShortcutItemWorkingDir);
                        }
                        else
                        {
                            var folderUri = new Uri($"files-uwp:?folder={Path.GetDirectoryName(ViewModel.ShortcutItemPath)}");
                            await Windows.System.Launcher.LaunchUriAsync(folderUri);
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

            ViewModel.ItemSizeVisibility = Visibility.Visible;
            ViewModel.ItemSize = $"{ByteSize.FromBytes(Item.FileSizeBytes).ToBinaryString().ConvertSizeAbbreviation()} ({ByteSize.FromBytes(Item.FileSizeBytes).Bytes:#,##0} {"ItemSizeBytes".GetLocalized()})";

            var fileIconInfo = await AppInstance.FilesystemViewModel.LoadIconOverlayAsync(Item.ItemPath, 80);
            if (fileIconInfo.IconData != null && !Item.IsLinkItem)
            {
                ViewModel.FileIconSource = await fileIconInfo.IconData.ToBitmapAsync();
            }

            if (Item.IsShortcutItem)
            {
                ViewModel.ItemCreatedTimestamp = Item.ItemDateCreated;
                ViewModel.ItemAccessedTimestamp = Item.ItemDateAccessed;
                ViewModel.LoadLinkIcon = Item.IsLinkItem;
                if (Item.IsLinkItem || string.IsNullOrWhiteSpace(((ShortcutItem)Item).TargetPath))
                {
                    // Can't show any other property
                    return;
                }
            }

            StorageFile file = await AppInstance.FilesystemViewModel.GetFileFromPathAsync((Item as ShortcutItem)?.TargetPath ?? Item.ItemPath);
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

            GetOtherProperties(file.Properties);

            // Get file MD5 hash
            var hashAlgTypeName = HashAlgorithmNames.Md5;
            ViewModel.ItemMD5HashProgressVisibility = Visibility.Visible;
            ViewModel.ItemMD5HashVisibility = Visibility.Visible;
            try
            {
                ViewModel.ItemMD5Hash = await AppInstance.InteractionOperations
                    .GetHashForFileAsync(Item, hashAlgTypeName, TokenSource.Token, ProgressBar);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
                ViewModel.ItemMD5HashCalcError = true;
            }
        }

        public async void GetSystemFileProperties()
        {
            StorageFile file = await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(Item.ItemPath).AsTask());
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
                .OrderBy(group => group.Priority)
                .Where(section => !section.All(fileProp => fileProp.Value == null));
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
            return result != null ? result.Locations[0].DisplayName : null;
        }

        public async Task SyncPropertyChangesAsync()
        {
            StorageFile file = await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(Item.ItemPath).AsTask());
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
                            await file.Properties.SavePropertiesAsync(newDict);
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
            StorageFile file = await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(Item.ItemPath).AsTask());
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
                            await file.Properties.SavePropertiesAsync(newDict);
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
                    if (AppInstance.FilesystemViewModel.Connection != null)
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
                        await AppInstance.FilesystemViewModel.Connection.SendMessageAsync(value);
                    }
                    break;
            }
        }
    }
}