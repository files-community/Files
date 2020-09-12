using ByteSizeLib;
using Files.Filesystem;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Microsoft.UI.Xaml.Controls;
using SQLitePCL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Security.Cryptography.Core;
using Windows.Services.Maps;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.View_Models.Properties
{
    internal class FileProperties : BaseProperties
    {
        private ProgressBar ProgressBar;

        //Read-only Properties
        private readonly List<string> PropertiesToGet_RO = new List<string>()
        {
            //Description
            "System.Rating",

            //Image
            "System.Image.BitDepth",
            "System.Image.Dimensions",
            "System.Image.HorizontalResolution",
            "System.Image.VerticalResolution",
            "System.Image.Compression",
            "System.Image.ResolutionUnit",
            "System.Image.HorizontalSize",
            "System.Image.VerticalSize",

            //Photo
            "System.Photo.ExposureTime",
            "System.Photo.FocalLength",
            "System.Photo.Aperture",
            "System.Photo.DateTaken"

            //GPS
            //Dunno why, but everything breaks when these are used, so an alternative is in place

            //"System.GPS.LatitudeDecimal",
            //"System.GPS.LongitudeDecimal",
            //"System.GPS.Altitude",

        };

        //Read and write properties
        private readonly List<string> PropertiesToGet_RW = new List<string>()
        {
            //Description
            "System.Title",
            "System.Subject",
            "System.Comment",
            "System.Copyright",


            //Photo
            "System.Photo.CameraManufacturer",
            "System.Photo.CameraModel",

        };
        public ListedItem Item { get; }

        public FileProperties(SelectedItemsPropertiesViewModel viewModel, CancellationTokenSource tokenSource, CoreDispatcher coreDispatcher, ProgressBar progressBar, ListedItem item)
        {
            ViewModel = viewModel;
            TokenSource = tokenSource;
            ProgressBar = progressBar;
            Dispatcher = coreDispatcher;
            Item = item;

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
                ViewModel.ItemPath = Path.IsPathRooted(Item.ItemPath) ? Path.GetDirectoryName(Item.ItemPath) : Item.ItemPath;
                ViewModel.ItemModifiedTimestamp = Item.ItemDateModified;
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

                    ViewModel.ShortcutItemType = isApplication ? ResourceController.GetTranslation("PropertiesShortcutTypeApplication") :
                        Item.IsLinkItem ? ResourceController.GetTranslation("PropertiesShortcutTypeLink") : ResourceController.GetTranslation("PropertiesShortcutTypeFile");
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
                            await Interacts.Interaction.InvokeWin32Component(ViewModel.ShortcutItemPath, ViewModel.ShortcutItemArguments, tmpItem.RunAsAdmin, ViewModel.ShortcutItemWorkingDir);
                        }
                        else
                        {
                            var folderUri = new Uri("files-uwp:" + "?folder=" + Path.GetDirectoryName(ViewModel.ShortcutItemPath));
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
            ViewModel.ItemSizeVisibility = Visibility.Visible;
            ViewModel.ItemSize = ByteSize.FromBytes(Item.FileSizeBytes).ToBinaryString().ConvertSizeAbbreviation()
                + " (" + ByteSize.FromBytes(Item.FileSizeBytes).Bytes.ToString("#,##0") + " " + ResourceController.GetTranslation("ItemSizeBytes") + ")";

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

            StorageFile file = null;
            try
            {
                file = await ItemViewModel.GetFileFromPathAsync((Item as ShortcutItem)?.TargetPath ?? Item.ItemPath);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
                // Could not access file, can't show any other property
                return;
            }

            using (var Thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, 80, ThumbnailOptions.UseCurrentScale))
            {
                BitmapImage icon = new BitmapImage();
                if (Thumbnail != null)
                {
                    ViewModel.FileIconSource = icon;
                    await icon.SetSourceAsync(Thumbnail);
                    ViewModel.LoadUnknownTypeGlyph = false;
                    ViewModel.LoadFileIcon = true;
                }
            }

            if (Item.IsShortcutItem)
            {
                // Can't show any other property
                return;
            }

            ViewModel.ItemCreatedTimestamp = ListedItem.GetFriendlyDate(file.DateCreated);
            GetOtherProperties(file.Properties);

            // Get file MD5 hash
            var hashAlgTypeName = HashAlgorithmNames.Md5;
            ViewModel.ItemMD5HashProgressVisibility = Visibility.Visible;
            ViewModel.ItemMD5HashVisibility = Visibility.Visible;
            try
            {
                ViewModel.ItemMD5Hash = await App.CurrentInstance.InteractionOperations
                    .GetHashForFile(Item, hashAlgTypeName, TokenSource.Token, ProgressBar);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
                ViewModel.ItemMD5HashCalcError = true;
            }

            GetSystemFileProperties();
        }

        public async void GetSystemFileProperties()
        {

            StorageFile file = null;
            try
            {
                file = await ItemViewModel.GetFileFromPathAsync((Item as ShortcutItem)?.TargetPath ?? Item.ItemPath);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
                // Could not access file, can't show any other property
                return;
            }

            //IDictionary<string, object> ViewModel.SystemFileProperties;

            try
            {
                // Get the specified properties through StorageFile.Properties
                ViewModel.SystemFileProperties_RO = await file.Properties.RetrievePropertiesAsync(PropertiesToGet_RO);
                ViewModel.SystemFileProperties_RW = await file.Properties.RetrievePropertiesAsync(PropertiesToGet_RW);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
                // Well this blew up
                return;
            }

            ViewModel.CameraNameString = string.Format("{0} {1}", ViewModel.SystemFileProperties_RW["System.Photo.CameraManufacturer"], ViewModel.SystemFileProperties_RW["System.Photo.CameraModel"]);

            MapLocationFinderResult result = null;
            try
            {
                //This code is temporary since the normal GPS property code doesn't work
                var imgprops = await file.Properties.GetImagePropertiesAsync();
                result = await GetAddressFromCoordinates((double)imgprops.Latitude, (double)imgprops.Longitude);
                //result = await GetAddressFromCoordinates((double)ViewModel.SystemFileProperties_RO["System.GPS.LatitudeDecimal"], (double)ViewModel.SystemFileProperties_RO["System.GPS.LongitudeDecimal"]);
            }
            catch { }

            if (result != null && result.Locations.Count > 0 && result.Status == MapLocationFinderStatus.Success)
            {
                ViewModel.Geopoint = result.Locations[0];
                ViewModel.GeopointString = string.Format("{0}, {1}", result.Locations[0].Address.Town.ToString(), result.Locations[0].Address.Region.ToString());
            }
            else
            {
                ViewModel.ShowGeotag = Visibility.Collapsed;
            }


            //var propValue = ViewModel.SystemFileProperties["System.Image.BitDepth"];
            //if (propValue != null)
            //{
            //    ViewModel.BitDepth = Convert.ToInt32(propValue);
            //}

            if (ViewModel.SystemFileProperties_RO["System.Photo.ExposureTime"] != null && ViewModel.SystemFileProperties_RO["System.Photo.FocalLength"] != null && ViewModel.SystemFileProperties_RO["System.Photo.Aperture"] != null)
            {
                ViewModel.ShotString = string.Format("{0} sec. f/{1} {2}mm", ViewModel.SystemFileProperties_RO["System.Photo.ExposureTime"], ViewModel.SystemFileProperties_RO["System.Photo.FocalLength"], ViewModel.SystemFileProperties_RO["System.Photo.Aperture"]);
            }

            var list = ViewModel.SystemFileProperties_RW;
            Debug.WriteLine(list.ToString());
        }

        public async Task<MapLocationFinderResult> GetAddressFromCoordinates(double Lat, double Lon)
        {
            //lol please don't steal this
            MapService.ServiceToken = "S7IF7M4Zxe9of0hbatDv~byc7WbHGg1rNYUqk4bL8Zw~Ar_Ap1WxoB_qnXme_hErpFhs74E8qKzCOXugSrankFJgJe9_D4l09O3TNj3WN2f2";
            // The location to reverse geocode.
            BasicGeoposition location = new BasicGeoposition();
            location.Latitude = Lat;
            location.Longitude = Lon;
            Geopoint pointToReverseGeocode = new Geopoint(location);

            // Reverse geocode the specified geographic location.
            return await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode);

            // If the query returns results, display the name of the town
            // contained in the address of the first result.
        }

        public async void SyncPropertyChanges()
        {
            StorageFile file = null;
            try
            {
                file = await ItemViewModel.GetFileFromPathAsync(Item.ItemPath);
            }
            catch
            {
                return;
            }
            //Dictionary<string, object> keyValues = new Dictionary<string, object>();
            //foreach(KeyValuePair<string, object> o in ViewModel.SystemFileProperties_RW)
            //    keyValues.Add(o.Key, o.Value);

            //IEnumerable<KeyValuePair<string, object>> param = ViewModel.SystemFileProperties_RW;
            //IEnumerable<KeyValuePair<string, object>> param = keyValues;
            try
            {
                await file.Properties.SavePropertiesAsync(ViewModel.SystemFileProperties_RW);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ShortcutItemPath":
                case "ShortcutItemWorkingDir":
                case "ShortcutItemArguments":
                    var tmpItem = (ShortcutItem)Item;
                    if (string.IsNullOrWhiteSpace(ViewModel.ShortcutItemPath))
                        return;
                    if (App.Connection != null)
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
                        await App.Connection.SendMessageAsync(value);
                    }
                    break;
            }
        }
    }
}