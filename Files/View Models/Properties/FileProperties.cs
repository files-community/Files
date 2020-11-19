using ByteSizeLib;
using Files.Converters;
using Files.Filesystem;
using Files.Helpers;
using Files.UserControls;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation.Collections;
using Windows.Security.Cryptography.Core;
using Windows.Services.Maps;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.View_Models.Properties
{
    public class FileProperties : BaseProperties
    {
        private ProgressBar ProgressBar;

        private List<FileProperty> PropertyListItemsBase = new List<FileProperty>()
        {
            new FileProperty()
            {
                Name = "Address",
                Section = "GPS",
                ID = "address",
            },
			new FileProperty() {
				Name = "Rating Text",
				Property = "System.RatingText",
				Section = "Core",
			},
			new FileProperty() {
				Name = "Item Folder Path Display",
				Property = "System.ItemFolderPathDisplay",
				Section = "Core",
			},
			new FileProperty() {
				Name = "Item Type Text",
				Property = "System.ItemTypeText",
				Section = "Core",
			},
			new FileProperty() {
				Name = "Title",
				Property = "System.Title",
				Section = "Core",
			},
			new FileProperty() {
				Name = "Subject",
				Property = "System.Subject",
				Section = "Core",
			},
			new FileProperty() {
				Name = "Comment",
				Property = "System.Comment",
				Section = "Core",
			},
			new FileProperty() {
				Name = "Copyright",
				Property = "System.Copyright",
				Section = "Core",
			},
			new FileProperty() {
				Name = "Date Created",
				Property = "System.DateCreated",
				Section = "Core",
				Converter = new DateTimeOffsetToString(),
			},
			new FileProperty() {
				Name = "Date Modified",
				Property = "System.DateModified",
				Section = "Core",
				Converter = new DateTimeOffsetToString(),
			},
			new FileProperty() {
				Name = "Image I D",
				Property = "System.Image.ImageID",
				Section = "Image",
			},
			new FileProperty() {
				Name = "Compressed Bits Per Pixel",
				Property = "System.Image.CompressedBitsPerPixel",
				Section = "Image",
				Converter = new DoubleToString(),
			},
			new FileProperty() {
				Name = "Bit Depth",
				Property = "System.Image.BitDepth",
				Section = "Image",
				Converter = new UInt32ToString(),
			},
			new FileProperty() {
				Name = "Dimensions",
				Property = "System.Image.Dimensions",
				Section = "Image",
			},
			new FileProperty() {
				Name = "Horizontal Resolution",
				Property = "System.Image.HorizontalResolution",
				Section = "Image",
				Converter = new DoubleToString(),
			},
			new FileProperty() {
				Name = "Vertical Resolution",
				Property = "System.Image.VerticalResolution",
				Section = "Image",
				Converter = new DoubleToString(),
			},
			new FileProperty() {
				Name = "Resolution Unit",
				Property = "System.Image.ResolutionUnit",
				Section = "Image",
			},
			new FileProperty() {
				Name = "Horizontal Size",
				Property = "System.Image.HorizontalSize",
				Section = "Image",
				Converter = new UInt32ToString(),
			},
			new FileProperty() {
				Name = "Vertical Size",
				Property = "System.Image.VerticalSize",
				Section = "Image",
				Converter = new UInt32ToString(),
			},
			new FileProperty() {
				Name = "Latitude",
				Property = "System.GPS.Latitude",
				Section = "GPS",
			},
			new FileProperty() {
				Name = "Latitude Decimal",
				Property = "System.GPS.LatitudeDecimal",
				Section = "GPS",
			},
			new FileProperty() {
				Name = "Latitude Ref",
				Property = "System.GPS.LatitudeRef",
				Section = "GPS",
			},
			new FileProperty() {
				Name = "Longitude",
				Property = "System.GPS.Longitude",
				Section = "GPS",
			},
			new FileProperty() {
				Name = "Longitude Decimal",
				Property = "System.GPS.LongitudeDecimal",
				Section = "GPS",
			},
			new FileProperty() {
				Name = "Longitude Ref",
				Property = "System.GPS.LongitudeRef",
				Section = "GPS",
			},
			new FileProperty() {
				Name = "Altitude",
				Property = "System.GPS.Altitude",
				Section = "GPS",
				Converter = new DoubleToString(),
			},
			new FileProperty() {
				Name = "Camera Manufacturer",
				Property = "System.Photo.CameraManufacturer",
				Section = "Photo",
			},
			new FileProperty() {
				Name = "Camera Model",
				Property = "System.Photo.CameraModel",
				Section = "Photo",
			},
			new FileProperty() {
				Name = "Exposure Time",
				Property = "System.Photo.ExposureTime",
				Section = "Photo",
				Converter = new DoubleToString(),
			},
			new FileProperty() {
				Name = "Focal Length",
				Property = "System.Photo.FocalLength",
				Section = "Photo",
				Converter = new DoubleToString(),
			},
			new FileProperty() {
				Name = "Aperture",
				Property = "System.Photo.Aperture",
				Section = "Photo",
				Converter = new DoubleToString(),
			},
			new FileProperty() {
				Name = "Date Taken",
				Property = "System.Photo.DateTaken",
				Section = "Photo",
				Converter = new DateTimeOffsetToString(),
			},
			new FileProperty() {
				Name = "Channel Count",
				Property = "System.Audio.ChannelCount",
				Section = "Audio",
				Converter = new UInt32ToString(),
			},
			new FileProperty() {
				Name = "Encoding Bitrate",
				Property = "System.Audio.EncodingBitrate",
				Section = "Audio",
				Converter = new UInt32ToString(),
			},
			new FileProperty() {
				Name = "Format",
				Property = "System.Audio.Format",
				Section = "Audio",
			},
			new FileProperty() {
				Name = "Sample Rate",
				Property = "System.Audio.SampleRate",
				Section = "Audio",
				Converter = new UInt32ToString(),
			},
			new FileProperty() {
				Name = "Album I D",
				Property = "System.Music.AlbumID",
				Section = "Music",
			},
			new FileProperty() {
				Name = "Display Artist",
				Property = "System.Music.DisplayArtist",
				Section = "Music",
			},
			new FileProperty() {
				Name = "Creator Application",
				Property = "System.Media.CreatorApplication",
				Section = "Media",
			},
			new FileProperty() {
				Name = "Album Artist",
				Property = "System.Music.AlbumArtist",
				Section = "Music",
			},
			new FileProperty() {
				Name = "Album Title",
				Property = "System.Music.AlbumTitle",
				Section = "Music",
			},
			new FileProperty() {
				Name = "Artist",
				Property = "System.Music.Artist",
				Section = "Music",
				Converter = new StringArrayToString(),
			},
			new FileProperty() {
				Name = "Beats Per Minute",
				Property = "System.Music.BeatsPerMinute",
				Section = "Music",
			},
			new FileProperty() {
				Name = "Composer",
				Property = "System.Music.Composer",
				Section = "Music",
				Converter = new StringArrayToString(),
			},
			new FileProperty() {
				Name = "Conductor",
				Property = "System.Music.Conductor",
				Section = "Music",
				Converter = new StringArrayToString(),
			},
			new FileProperty() {
				Name = "Disc Number",
				Property = "System.Music.DiscNumber",
				Section = "Music",
				Converter = new UInt32ToString(),
			},
			new FileProperty() {
				Name = "Genre",
				Property = "System.Music.Genre",
				Section = "Music",
				Converter = new StringArrayToString(),
			},
			new FileProperty() {
				Name = "Track Number",
				Property = "System.Music.TrackNumber",
				Section = "Music",
				Converter = new UInt32ToString(),
			},
			new FileProperty() {
				Name = "Average Level",
				Property = "System.Media.AverageLevel",
				Section = "Media",
				Converter = new UInt32ToString(),
			},
			new FileProperty() {
				Name = "Duration",
				Property = "System.Media.Duration",
				Section = "Media",
			},
			new FileProperty() {
				Name = "Frame Count",
				Property = "System.Media.FrameCount",
				Section = "Media",
				Converter = new UInt32ToString(),
			},
			new FileProperty() {
				Name = "Protection Type",
				Property = "System.Media.ProtectionType",
				Section = "Media",
			},
			new FileProperty() {
				Name = "Author Url",
				Property = "System.Media.AuthorUrl",
				Section = "Media",
			},
			new FileProperty() {
				Name = "Content Distributor",
				Property = "System.Media.ContentDistributor",
				Section = "Media",
			},
			new FileProperty() {
				Name = "Date Released",
				Property = "System.Media.DateReleased",
				Section = "Media",
			},
			new FileProperty() {
				Name = "Series Name",
				Property = "System.Media.SeriesName",
				Section = "Media",
			},
			new FileProperty() {
				Name = "Season Number",
				Property = "System.Media.SeasonNumber",
				Section = "Media",
				Converter = new UInt32ToString(),
			},
			new FileProperty() {
				Name = "Episode Number",
				Property = "System.Media.EpisodeNumber",
				Section = "Media",
				Converter = new UInt32ToString(),
			},
			new FileProperty() {
				Name = "Producer",
				Property = "System.Media.Producer",
				Section = "Media",
				Converter = new StringArrayToString(),
			},
			new FileProperty() {
				Name = "Promotion Url",
				Property = "System.Media.PromotionUrl",
				Section = "Media",
			},
			new FileProperty() {
				Name = "Provider Style",
				Property = "System.Media.ProviderStyle",
				Section = "Media",
			},
			new FileProperty() {
				Name = "Publisher",
				Property = "System.Media.Publisher",
				Section = "Media",
			},
			new FileProperty() {
				Name = "Thumbnail Large Path",
				Property = "System.Media.ThumbnailLargePath",
				Section = "Media",
			},
			new FileProperty() {
				Name = "Thumbnail Small Path",
				Property = "System.Media.ThumbnailSmallPath",
				Section = "Media",
			},
			new FileProperty() {
				Name = "Unique File Identifier",
				Property = "System.Media.UniqueFileIdentifier",
				Section = "Media",
			},
			new FileProperty() {
				Name = "User Web Url",
				Property = "System.Media.UserWebUrl",
				Section = "Media",
			},
			new FileProperty() {
				Name = "Writer",
				Property = "System.Media.Writer",
				Section = "Media",
				Converter = new StringArrayToString(),
			},
			new FileProperty() {
				Name = "Year",
				Property = "System.Media.Year",
				Section = "Media",
				Converter = new UInt32ToString(),
			},

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

            //ViewModelProcessing();
            var list = new List<FileProperty>();

            // Get all the properties from the base, get their values (if needed), and add them to the ViewModel list.
            foreach (var item in PropertyListItemsBase)
            {
                if (item.Property != null)
                {
                    var props = await file.Properties.RetrievePropertiesAsync(new List<string>() { item.Property });
                    item.Value = props[item.Property];
                }
                list.Add(item);
            }
            list.Find(x => x.ID == "address").Value = await GetAddressFromCoordinates((double?)list.Find(x => x.Property == "System.GPS.LatitudeDecimal").Value, (double?)list.Find(x => x.Property == "System.GPS.LongitudeDecimal").Value);

            // This code groups the properties by their "section" property. The code is derived from the XAML Controls Gallery ListView with grouped headers sample.
            var query = from item in list group item by item.Section into g orderby g.Key select new FilePropertySection(g) { Key = g.Key };
            ViewModel.PropertySections = new ObservableCollection<FilePropertySection>(query);

            SetVisibilities();
        }

        private void SetVisibilities()
        {
            var propertySections = new List<FilePropertySection>(ViewModel.PropertySections);
            foreach (var group in propertySections)
            {
                if (CheckSectionNull(group))
                    ViewModel.PropertySections.Remove(group);
            }
        }

        private bool CheckSectionNull(FilePropertySection fileProperties)
        {
            foreach (var prop in fileProperties)
            {
                if (prop.Value != null)
                    return false;
            }

            return true;
        }

        private async Task<string> GetAddressFromCoordinates(double? Lat, double? Lon)
        {
            if (!Lat.HasValue || !Lon.HasValue)
                return null;

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
            location.Latitude = Lat.Value;
            location.Longitude = Lon.Value;
            Geopoint pointToReverseGeocode = new Geopoint(location);

            // Reverse geocode the specified geographic location.

            var result = await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode);
            return result != null ? result.Locations[0].DisplayName : null;
        }


        public async Task SyncPropertyChanges()
        {
            StorageFile file = null;
            file = await ItemViewModel.GetFileFromPathAsync(Item.ItemPath);

            var failedProperties = "";
            foreach (var group in ViewModel.PropertySections)
            {
                foreach (FileProperty prop in group)
                {
                    if (!prop.IsReadOnly)
                    {
                        var newDict = new Dictionary<string, object>();
                        newDict.Add(prop.Property, prop.Value);

                        try
                        {
                            await file.Properties.SavePropertiesAsync(newDict);
                        }
                        catch (Exception e)
                        {
                            failedProperties += $"{prop.Name}\n";
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(failedProperties))
                throw new Exception($"The following properties failed to save: {failedProperties}");

        }

        /// <summary>
        /// This function goes through ever read-write property saved, then syncs it
        /// </summary>
        /// <returns></returns>
        public async Task ClearPersonalInformation()
        {
            var failedProperties = new List<string>();
            StorageFile file = null;
            try
            {
                file = await ItemViewModel.GetFileFromPathAsync(Item.ItemPath);
            }
            catch
            {
                return;
            }

            foreach (var group in ViewModel.PropertySections)
            {
                foreach (FileProperty prop in group)
                {
                    if (prop.IsPersonalProperty)
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

            //if (failedProperties.Count > 0)
            //{
            //    var text = "The following properties failed to save";

            //    foreach (var error in failedProperties)
            //    {
            //        text += $"{error}, ";
            //    }
            //    text.Remove(text.LastIndexOf(','));
            //    var toastContent = new ToastContent()
            //    {
            //        Visual = new ToastVisual()
            //        {
            //            BindingGeneric = new ToastBindingGeneric()
            //            {
            //                Children =
            //                {
            //                    new AdaptiveText()
            //                    {
            //                        Text = "Some properties failed to clear"
            //                    },
            //                    new AdaptiveText()
            //                    {
            //                        Text = text
            //                    }
            //                },
            //                AppLogoOverride = new ToastGenericAppLogo()
            //                {
            //                    Source = "ms-appx:///Assets/error.png"
            //                }
            //            }
            //        },
            //        Actions = new ToastActionsCustom()
            //        {
            //            Buttons =
            //            {
            //                new ToastButton(ResourceController.GetTranslation("ExceptionNotificationReportButton"), "report")
            //                {
            //                    ActivationType = ToastActivationType.Foreground
            //                }
            //            }
            //        }
            //    };

            //    // Create the toast notification
            //    var toastNotif = new ToastNotification(toastContent.GetXml());

            //    // And send the notification
            //    ToastNotificationManager.CreateToastNotifier().Show(toastNotif);
            //}


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