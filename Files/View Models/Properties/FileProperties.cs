using ByteSizeLib;
using Files.Filesystem;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Windows.Devices.Geolocation;
using Windows.Foundation.Collections;
using Windows.Security.Cryptography.Core;
using Windows.Services.Maps;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.View_Models.Properties
{
    internal class FileProperties : BaseProperties
    {
        private ProgressBar ProgressBar;

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
        }

        public async void GetAddressFromCoordinates()
        {
            //lol please don't steal this
            MapService.ServiceToken = "S7IF7M4Zxe9of0hbatDv~byc7WbHGg1rNYUqk4bL8Zw~Ar_Ap1WxoB_qnXme_hErpFhs74E8qKzCOXugSrankFJgJe9_D4l09O3TNj3WN2f2";
            // The location to reverse geocode.
            BasicGeoposition location = new BasicGeoposition();
            location.Latitude = (double)ViewModel.Latitude;
            location.Longitude = (double)ViewModel.Longitude;
            Geopoint pointToReverseGeocode = new Geopoint(location);

            // Reverse geocode the specified geographic location.
            MapLocationFinderResult result =
                    await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode);

            // If the query returns results, display the name of the town
            // contained in the address of the first result.
            if (result.Status == MapLocationFinderStatus.Success)
            {
                ViewModel.Geopoint = result.Locations[0];
                ViewModel.GeopointString = string.Format("{0}, {1}", result.Locations[0].Address.Town.ToString(), result.Locations[0].Address.Region.ToString());
            }
            else
            {
                ViewModel.GeopointString = string.Format("{0:g}, {1:g}", Math.Truncate((decimal)ViewModel.Latitude * 10000000) / 10000000, Math.Truncate((decimal)ViewModel.Longitude * 10000000) / 10000000);
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

    internal class ImageFileProperties : FileProperties
    {
        private ProgressBar ProgressBar;

        public ImageFileProperties(SelectedItemsPropertiesViewModel viewModel, CancellationTokenSource tokenSource, CoreDispatcher coreDispatcher, ProgressBar progressBar, ListedItem item) : base(viewModel, tokenSource, coreDispatcher, progressBar, item)
        {
            ProgressBar = progressBar;

            GetBaseProperties();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        public override async void GetSpecialProperties()
        {
            base.GetSpecialProperties();

            StorageFile file = null;
            ImageProperties imageProperties;
            try
            {
                file = await ItemViewModel.GetFileFromPathAsync((Item as ShortcutItem)?.TargetPath ?? Item.ItemPath);
                imageProperties = await file.Properties.GetImagePropertiesAsync();
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
                // Could not access file, can't show any other property
                return;
            }

            //List of properties to retrieve
            List<string> moreProperties = new List<string>() {
                    "System.Image.BitDepth",
                    "System.Photo.ExposureTime",
                    "System.Photo.FocalLength",
                    "System.Photo.Aperture",
            };

            IDictionary<string, object> extraProperties;
            IDictionary<string, object> extraProperties2;
            List<PropertiesData> propertiesDatas = new List<PropertiesData>();

            try
            {
                // Get the specified properties through StorageFile.Properties
                extraProperties = await file.Properties.RetrievePropertiesAsync(moreProperties);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
                // Well this blew up
                return;
            }

            ViewModel.DateTaken = imageProperties.DateTaken;
            ViewModel.CameraModel = imageProperties.CameraModel;
            ViewModel.CameraManufacturer = imageProperties.CameraManufacturer;
            ViewModel.ImageWidth = (int)imageProperties.Width;
            ViewModel.ImageHeight = (int)imageProperties.Height;
            ViewModel.DimensionString = ViewModel.ImageWidth + "x" + ViewModel.ImageHeight;
            ViewModel.DimensionsTooltip = "Width: " + ViewModel.ImageWidth + "px\nHeight: " + ViewModel.ImageHeight + "px";
            ViewModel.ImageTitle = imageProperties.Title;
            ViewModel.Longitude = imageProperties.Longitude;
            ViewModel.Latitude = imageProperties.Latitude;
            ViewModel.ImageKeywords = imageProperties.Keywords;
            ViewModel.Rating = (int)imageProperties.Rating;
            ViewModel.ImageOrientation = imageProperties.Orientation;
            ViewModel.CameraNameString = string.Format("{0} {1}", ViewModel.CameraManufacturer, ViewModel.CameraModel);

            foreach (string str in ViewModel.ImageKeywords)
                ViewModel.Tags += str + "; ";

            ViewModel.ShowTitle = ViewModel.ImageTitle.Equals("") ? Visibility.Collapsed : Visibility.Visible;

            ViewModel.RatingReal = ViewModel.Rating / 20.00 == 0.0 ? -1 : ViewModel.Rating / 20.00;

            //ViewModel.People = (System.Collections.Generic.IList<string>) imageProperties.PeopleNames;

            if (ViewModel.Longitude != null && ViewModel.Latitude != null)
            {
                MapService.ServiceToken = "S7IF7M4Zxe9of0hbatDv~byc7WbHGg1rNYUqk4bL8Zw~Ar_Ap1WxoB_qnXme_hErpFhs74E8qKzCOXugSrankFJgJe9_D4l09O3TNj3WN2f2";
                // The location to reverse geocode.
                BasicGeoposition location = new BasicGeoposition();
                location.Latitude = (double)ViewModel.Latitude;
                location.Longitude = (double)ViewModel.Longitude;
                Geopoint pointToReverseGeocode = new Geopoint(location);

                // Reverse geocode the specified geographic location.
                MapLocationFinderResult result =
                      await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode);

                // If the query returns results, display the name of the town
                // contained in the address of the first result.
                if (result.Status == MapLocationFinderStatus.Success)
                {
                    ViewModel.Geopoint = result.Locations[0];
                    ViewModel.GeopointString = string.Format("{0}, {1}", result.Locations[0].Address.Town.ToString(), result.Locations[0].Address.Region.ToString());
                }
                else
                {
                    ViewModel.GeopointString = string.Format("{0:g}, {1:g}", Math.Truncate((decimal)ViewModel.Latitude * 10000000) / 10000000, Math.Truncate((decimal)ViewModel.Longitude * 10000000) / 10000000);
                }
            }
            else
            {
                ViewModel.ShowGeotag = Visibility.Collapsed;
            }


            var propValue = extraProperties[moreProperties[0]];
            if (propValue != null)
            {
                ViewModel.BitDepth = Convert.ToInt32(propValue);
            }

            if (extraProperties[moreProperties[1]] != null && extraProperties[moreProperties[2]] != null && extraProperties[moreProperties[3]] != null)
            {
                ViewModel.ShotString = string.Format("{0} sec. f/{1} {2}mm", extraProperties[moreProperties[1]], extraProperties[moreProperties[2]], extraProperties[moreProperties[3]]);
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