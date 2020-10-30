using ByteSizeLib;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.Extensions;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
    public class FileProperties : BaseProperties
    {
        private ProgressBar ProgressBar;

        private readonly List<string> PropertiesToGet_RO = new List<string>()
        {
            //Core
            "System.RatingText",
            "System.ItemFolderPathDisplay",
            "System.ItemTypeText",

            //Image
            "System.Image.ImageID",
            "System.Image.CompressedBitsPerPixel",
            "System.Image.BitDepth",
            "System.Image.Dimensions",
            "System.Image.HorizontalResolution",
            "System.Image.VerticalResolution",
            "System.Image.CompressionText",
            "System.Image.ResolutionUnit",
            "System.Image.HorizontalSize",
            "System.Image.VerticalSize",

            //GPS
            "System.GPS.Latitude",
            "System.GPS.LatitudeRef",
            "System.GPS.Longitude",
            "System.GPS.LongitudeRef",
            "System.GPS.Altitude",

            //Photo
            "System.Photo.ExposureTime",
            "System.Photo.FocalLength",
            "System.Photo.Aperture",
            "System.Photo.DateTaken",

            //Audio
            "System.Audio.ChannelCount",
            "System.Audio.EncodingBitrate",
            "System.Audio.Compression",
            "System.Audio.Format",
            "System.Audio.SampleRate",

            //Music
            "System.Music.AlbumID",
            "System.Music.DisplayArtist",
            "System.Media.CreatorApplication",

            //Media
            "System.Media.AverageLevel",
            "System.Media.Duration",
            "System.Media.FrameCount",
            "System.Media.ProtectionType",
        };

        private readonly List<string> PropertiesToGet_RW = new List<string>()
        {
            //Core
            "System.Title",
            "System.Subject",
            "System.Comment",
            "System.Copyright",
            "System.DateCreated",
            "System.DateModified",

            //Photo
            "System.Photo.CameraManufacturer",
            "System.Photo.CameraModel",

            //Music
            "System.Music.AlbumArtist",
            "System.Music.AlbumTitle",
            "System.Music.Artist",
            "System.Music.BeatsPerMinute",
            "System.Music.Composer",
            "System.Music.Conductor",
            "System.Music.DiscNumber",
            "System.Music.Genre",
            "System.Music.TrackNumber",

            //Media
            "System.Media.AuthorUrl",
            "System.Media.ContentDistributor",
            "System.Media.DateReleased",
            "System.Media.DlnaProfileID",
            "System.Media.DVDID",
            "System.Media.EncodedBy",
            "System.Media.EncodingSettings",
            "System.Media.SeriesName",
            "System.Media.SeasonNumber",
            "System.Media.EpisodeNumber",
            "System.Media.MCDI",
            "System.Media.Producer",
            "System.Media.PromotionUrl",
            "System.Media.ProviderStyle",
            "System.Media.Publisher",
            "System.Media.ThumbnailLargePath",
            "System.Media.ThumbnailLargeUri",
            "System.Media.ThumbnailSmallPath",
            "System.Media.ThumbnailSmallUri",
            "System.Media.UniqueFileIdentifier",
            "System.Media.UserWebUrl",
            "System.Media.Writer",
            "System.Media.Year",
        };

        /// <summary>
        /// This list stores all properties to be cleared when clear personal properties is called
        /// </summary>
        private readonly List<string> PersonalProperties = new List<string>()
        {
                "System.GPS.LatitudeNumerator",
                "System.GPS.LatitudeDenominator",
                "System.GPS.LongitudeNumerator",
                "System.GPS.LongitudeDenominator",
                "System.GPS.AltitudeNumerator",
                "System.GPS.AltitudeDenominator",
                "System.GPS.AltitudeRef",
                "System.Title",
                "System.Subject",
                "System.Comment",
                "System.Copyright",
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
                + " (" + ByteSize.FromBytes(Item.FileSizeBytes).Bytes.ToString("#,##0") + " " + "ItemSizeBytes".GetLocalized() + ")";

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

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";

            ViewModel.ItemCreatedTimestamp = ListedItem.GetFriendlyDateFromFormat(file.DateCreated, returnformat);
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

            //GenerateXAMLCode();
            try
            {
                ViewModel.SystemFileProperties_RO = await file.Properties.RetrievePropertiesAsync(PropertiesToGet_RO);
                ViewModel.SystemFileProperties_RW = await file.Properties.RetrievePropertiesAsync(PropertiesToGet_RW);
                //GetPropertiesAsyncDebug(file);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            SetVisibilities();

            ViewModelProcessing();
        }

        private void SetLocationInformation()
        {
            double[] latitude = ViewModel.SystemFileProperties_RO["System.GPS.Latitude"] as double[];
            double[] longitude = ViewModel.SystemFileProperties_RO["System.GPS.Longitude"] as double[];
            ViewModel.Latitude = (latitude[0] + (latitude[1] / 60) + (latitude[2] / 3600));
            ViewModel.Longitude = longitude[0] + (longitude[1] / 60) + (longitude[2] / 3600);
            ViewModel.Latitude *= (ViewModel.SystemFileProperties_RO["System.GPS.LatitudeRef"] as string).ToLower().Equals("s") ? -1 : 1;
            ViewModel.Longitude *= (ViewModel.SystemFileProperties_RO["System.GPS.LongitudeRef"] as string).ToLower().Equals("w") ? -1 : 1;
        }

        /// <summary>
        /// Use this function to process any information for the view model
        /// </summary>
        private async void ViewModelProcessing()
        {
            if (ViewModel.DetailsSectionVisibility_Photo.Equals(Visibility.Visible))
                ViewModel.CameraNameString = string.Format("{0} {1}", ViewModel.SystemFileProperties_RW["System.Photo.CameraManufacturer"], ViewModel.SystemFileProperties_RW["System.Photo.CameraModel"]);

            if (ViewModel.DetailsSectionVisibility_GPS == Visibility.Visible)
                SetLocationInformation();

            if (ViewModel.DetailsSectionVisibility_GPS.Equals(Visibility.Visible))
            {
                MapLocationFinderResult result = null;
                try
                {
                    result = await GetAddressFromCoordinates((double)ViewModel.Latitude, (double)ViewModel.Longitude);
                    if (result != null)
                    {
                        ViewModel.Geopoint = result.Locations[0];
                        ViewModel.GeopointString = string.Format("{0}, {1}", result.Locations[0].Address.Town.ToString(), result.Locations[0].Address.Region.ToString());
                    }
                    else
                    {
                        ViewModel.GeopointString = string.Format("{0}, {1}", ViewModel.Latitude, ViewModel.Longitude);
                    }
                }
                catch
                {
                }
            }

            if (ViewModel.DetailsSectionVisibility_Photo.Equals(Visibility.Visible))
            {
                ViewModel.ShotString = string.Format("{0} sec. f/{1} {2}mm", ViewModel.SystemFileProperties_RO["System.Photo.ExposureTime"], ViewModel.SystemFileProperties_RO["System.Photo.Aperture"], ViewModel.SystemFileProperties_RO["System.Photo.FocalLength"]);
            }
        }

        private void SetVisibilities()
        {
            ViewModel.DetailsSectionVisibility_GPS = CheckVisibility("System.GPS");
            ViewModel.DetailsSectionVisibility_Photo = CheckVisibility("System.Photo");
            ViewModel.DetailsSectionVisibility_Image = CheckVisibility("System.Image");
            ViewModel.DetailsSectionVisibility_Audio = CheckVisibility("System.Audio");
            ViewModel.DetailsSectionVisibility_Music = CheckVisibility("System.Music");
            ViewModel.DetailsSectionVisibility_Media = CheckVisibility("System.Media");
        }

        private Visibility CheckVisibility(string endpoint)
        {
            return CheckVisibilityHelper(endpoint, ViewModel.SystemFileProperties_RO) || CheckVisibilityHelper(endpoint, ViewModel.SystemFileProperties_RW) ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool CheckVisibilityHelper(string endpoint, IDictionary<string, object> dict)
        {
            foreach (KeyValuePair<string, object> pair in dict)
                if (pair.Key.Contains(endpoint) && pair.Value != null)
                    return true;

            return false;
        }

        private async Task<MapLocationFinderResult> GetAddressFromCoordinates(double Lat, double Lon)
        {
            JObject obj;
            try
            {
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(@"ms-appx:///Resources/BingMapsKey.txt"));
                var lines = await FileIO.ReadTextAsync(file);
                obj = JObject.Parse(lines);
            }
            catch (Exception e)
            {
                return null;
            }
            MapService.ServiceToken = (string)obj.SelectToken("key");

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
                SavePropertiesAsync(file);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        private async void SavePropertiesAsync(StorageFile file)
        {
            foreach (KeyValuePair<string, object> valuePair in ViewModel.SystemFileProperties_RW)
            {
                var newDict = new Dictionary<string, object>();
                newDict.Add(valuePair.Key, valuePair.Value);

                try
                {
                    await file.Properties.SavePropertiesAsync(newDict);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(string.Format("{0}\n{1}", valuePair.Key, e.ToString()));
                }
            }
        }

        /// <summary>
        /// This function goes through ever read-write property saved, then syncs it
        /// </summary>
        /// <returns></returns>
        public async Task ClearPersonalInformation()
        {
            StorageFile file = null;
            try
            {
                file = await ItemViewModel.GetFileFromPathAsync(Item.ItemPath);
            }
            catch
            {
                //return;
            }
            var dict = new Dictionary<string, object>();

            foreach (string str in PersonalProperties)
                dict.Add(str, null);

            await file.Properties.SavePropertiesAsync(dict);

            GetSpecialProperties();
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