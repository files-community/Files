using ByteSizeLib;
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
                Name = "Latitude Decimal",
                Property = "System.GPS.LatitudeDecimal",
                Section = "GPS",
                IsPersonalProperty = true,
            },
            new FileProperty() {
                Name = "Longitude Decimal",
                Property = "System.GPS.LongitudeDecimal",
                Section = "GPS",
            },
            new FileProperty() {
                Name = "Latitude",
                Property = "System.GPS.Latitude",
                Section = "GPS",
                IsPersonalProperty = true,
            },
            new FileProperty() {
                Name = "Latitude Ref",
                Property = "System.GPS.LatitudeRef",
                Section = "GPS",
                IsPersonalProperty = true,
		    },
		    new FileProperty() {
			    Name = "Longitude",
			    Property = "System.GPS.Longitude",
			    Section = "GPS",
                IsPersonalProperty = true,
            },
		    new FileProperty() {
			    Name = "Longitude Ref",
			    Property = "System.GPS.LongitudeRef",
			    Section = "GPS",
                IsPersonalProperty = true,
            },
		    new FileProperty() {
			    Name = "Altitude",
			    Property = "System.GPS.Altitude",
			    Section = "GPS",
                IsPersonalProperty = true,
            },
		    new FileProperty() {
			    Name = "Exposure Time",
			    Property = "System.Photo.ExposureTime",
			    Section = "Photo",
		    },
		    new FileProperty() {
			    Name = "Focal Length",
			    Property = "System.Photo.FocalLength",
			    Section = "Photo",
		    },
		    new FileProperty() {
			    Name = "Aperture",
			    Property = "System.Photo.Aperture",
			    Section = "Photo",
		    },
		    new FileProperty() {
			    Name = "Date Taken",
			    Property = "System.Photo.DateTaken",
			    Section = "Photo",
		    },
		    new FileProperty() {
			    Name = "Channel Count",
			    Property = "System.Audio.ChannelCount",
			    Section = "Audio",
		    },
		    new FileProperty() {
			    Name = "Encoding Bitrate",
			    Property = "System.Audio.EncodingBitrate",
			    Section = "Audio",
		    },
		    new FileProperty() {
			    Name = "Compression",
			    Property = "System.Audio.Compression",
			    Section = "Audio",
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
                if(item.Property != null)
                {
                    var props = await file.Properties.RetrievePropertiesAsync(new List<string>() { item.Property });
                    item.Value = props[item.Property];
                }
                list.Add(item);
            }

            list.Find(x => x.ID == "address").Value = await GetAddressFromCoordinates((double)list.Find(x => x.Property == "System.GPS.LatitudeDecimal").Value, (double)list.Find(x => x.Property == "System.GPS.LongitudeDecimal").Value);

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

        private async Task<string> GetAddressFromCoordinates(double Lat, double Lon)
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

            var result = await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode);
            return result != null ? result.Locations[0].DisplayName : null;
        }

        public async void SyncPropertyChanges()
        {
            StorageFile file = null;
            //banner.Progress = new Progress<uint>();
            try
            {
                file = await ItemViewModel.GetFileFromPathAsync(Item.ItemPath);
                await SavePropertiesAsync(file);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

        }

        private async Task SavePropertiesAsync(StorageFile file)
        {
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
                            Debug.WriteLine(string.Format("{0}\n{1}", prop.Property, e.ToString()));
                        }
                    }
                }
            }
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