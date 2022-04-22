using Files.Extensions;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Files.Uwp.Extensions;
using Files.Uwp.Filesystem;
using Files.Uwp.Filesystem.StorageItems;
using Files.Uwp.Helpers;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;
using Windows.Storage;
using Windows.UI.Core;

namespace Files.Uwp.ViewModels.Properties
{
    internal class CombinedProperties : BaseProperties
    {
        public List<ListedItem> List { get; }

        public CombinedProperties(SelectedItemsPropertiesViewModel viewModel, CancellationTokenSource tokenSource,
            CoreDispatcher coreDispatcher, List<ListedItem> listedItems, IShellPage instance)
        {
            ViewModel = viewModel;
            TokenSource = tokenSource;
            Dispatcher = coreDispatcher;
            List = listedItems;
            AppInstance = instance;
            GetBaseProperties();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        public override void GetBaseProperties()
        {
            if (List != null)
            {
                ViewModel.LoadCombinedItemsGlyph = true;
                if (List.All(x => x.ItemType.Equals(List.First().ItemType)))
                {
                    ViewModel.ItemType = string.Format("PropertiesDriveItemTypesEquals".GetLocalized(), List.First().ItemType);
                }
                else
                {
                    ViewModel.ItemType = "PropertiesDriveItemTypeDifferent".GetLocalized();
                }
                var itemsPath = List.Select(Item => (Item as RecycleBinItem)?.ItemOriginalFolder ??
                    (Path.IsPathRooted(Item.ItemPath) ? Path.GetDirectoryName(Item.ItemPath) : Item.ItemPath));
                if (itemsPath.Distinct().Count() == 1)
                {
                    ViewModel.ItemPath = string.Format("PropertiesCombinedItemPath".GetLocalized(), itemsPath.First());
                }
            }
        }

        public override async void GetSpecialProperties()
        {
            if (List.All(x => x.PrimaryItemAttribute == StorageItemTypes.File))
            {
                ViewModel.IsReadOnly = List.All(x => NativeFileOperationsHelper.HasFileAttribute(x.ItemPath, System.IO.FileAttributes.ReadOnly));
            }
            ViewModel.IsHidden = List.All(x => NativeFileOperationsHelper.HasFileAttribute(x.ItemPath, System.IO.FileAttributes.Hidden));

            ViewModel.LastSeparatorVisibility = false;
            ViewModel.ItemSizeVisibility = true;

            ViewModel.FilesCount += List.Where(x => x.PrimaryItemAttribute == StorageItemTypes.File).ToList().Count;
            ViewModel.FoldersCount += List.Where(x => x.PrimaryItemAttribute == StorageItemTypes.Folder).ToList().Count;

            long totalSize = 0;
            long filesSize = List.Where(x => x.PrimaryItemAttribute == StorageItemTypes.File).Sum(x => x.FileSizeBytes);
            long foldersSize = 0;

            ViewModel.ItemSizeProgressVisibility = true;
            foreach (var item in List)
            {
                if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    var fileSizeTask = Task.Run(async () =>
                    {
                        var size = await CalculateFolderSizeAsync(item.ItemPath, TokenSource.Token);
                        return size;
                    });
                    try
                    {
                        foldersSize += await fileSizeTask;
                    }
                    catch (Exception ex)
                    {
                        App.Logger.Warn(ex, ex.Message);
                    }
                }
            }
            ViewModel.ItemSizeProgressVisibility = false;

            totalSize = filesSize + foldersSize;
            ViewModel.ItemSize = totalSize.ToLongSizeString();
            SetItemsCountString();
        }

        public async void GetSystemFileProperties()
        {
            List<BaseStorageFile> files = new();
            foreach (ListedItem Item in List)
            {
                BaseStorageFile file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(Item.ItemPath));
                if (file == null)
                {
                    // Could not access file, can't show any other property
                    continue;
                }
                files.Add(file);
            }

            List<List<FileProperty>> listAll = new();
            foreach (BaseStorageFile file in files)
            {
                var listItem = await FileProperty.RetrieveAndInitializePropertiesAsync(file);
                listAll.Add(listItem);
            }

            List<List<FileProperty>> precombinded = new();
            foreach (List<FileProperty> list in listAll)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (precombinded.ElementAtOrDefault(i) == null)
                    {
                        List<FileProperty> tempList = new();
                        tempList.Add(list[i]);
                        precombinded.Add(tempList);
                    }
                    else 
                    {
                        precombinded[i].Add(list[i]);
                    }
                }
            }

            List<FileProperty> finalProperties = new();
            foreach(List<FileProperty> list in precombinded)
            {
                if (list.First().Property == "System.ItemTypeText")
                {
                    if (!list.All(x => x.Value.Equals(list.First().Value)))
                    {
                        list.First().Value = string.Join("; ", list.Select(x => x.Value).Distinct().ToList());
                        finalProperties.Add(list.First());
                    }
                    else
                    {
                        finalProperties.Add(list.First());
                    }
                }
                else if (list.First().Property == "System.Media.Duration")
                {
                    if (!list.All(x => x.Value == null))
                    {
                        list.First().Value = list.Select(x => x.Value).ToList().Sum(str => Convert.ToInt64(str));
                        finalProperties.Add(list.First());
                    }
                }
                else if (list.First().Property == "System.FilePlaceholderStatus")
                {
                    finalProperties.Add(list.First());
                }
                else if (list.All(x => x.Value == null))
                {
                    finalProperties.Add(list.First());
                }
                else if (list.Where(x => x.Value != null).Count() == 1)
                {
                    var result = list.Where(x => x.Value != null);
                    if (result.Any() && result.Count() == 1)
                    {
                        finalProperties.Add(result.First());
                    }
                }
                else if (!list.All(x => x.Value.Equals(list.First().Value)))
                {
                    list.First().Value = "PropertiesFilesHasMultipleValues".GetLocalized();
                    finalProperties.Add(list.First());
                }
                else
                {
                    finalProperties.Add(list.First());
                }
                // System.ItemPathDisplay
            }

            var query = finalProperties
                .Where(fileProp => !(fileProp.Value == null && fileProp.IsReadOnly))
                .GroupBy(fileProp => fileProp.SectionResource)
                .Select(group => new FilePropertySection(group) { Key = group.Key })
                .Where(section => !section.All(fileProp => fileProp.Value == null))
                .OrderBy(group => group.Priority);
            ViewModel.PropertySections = new ObservableCollection<FilePropertySection>(query);
            ViewModel.FileProperties = new ObservableCollection<FileProperty>(finalProperties.Where(i => i.Value != null));
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
            foreach (ListedItem Item in List)
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
        }

        /// <summary>
        /// This function goes through ever read-write property saved, then syncs it
        /// </summary>
        /// <returns></returns>
        public async Task ClearPropertiesAsync()
        {
            foreach (ListedItem Item in List)
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
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsReadOnly":
                    if (ViewModel.IsReadOnly)
                    {
                        List.ForEach(x => NativeFileOperationsHelper.SetFileAttribute(
                            x.ItemPath, System.IO.FileAttributes.ReadOnly));
                    }
                    else
                    {
                        List.ForEach(x => NativeFileOperationsHelper.UnsetFileAttribute(
                            x.ItemPath, System.IO.FileAttributes.ReadOnly));
                    }
                    break;

                case "IsHidden":
                    if (ViewModel.IsHidden)
                    {
                        List.ForEach(x => NativeFileOperationsHelper.SetFileAttribute(
                            x.ItemPath, System.IO.FileAttributes.Hidden));
                    }
                    else
                    {
                        List.ForEach(x => NativeFileOperationsHelper.UnsetFileAttribute(
                            x.ItemPath, System.IO.FileAttributes.Hidden));
                    }
                    break;
            }
        }
    }
}