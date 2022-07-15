using Files.Uwp.DataModels;
using Files.Shared.Enums;
using Files.Uwp.Filesystem;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;
using Files.Shared.Extensions;
using System.Collections.Specialized;

namespace Files.Uwp.Controllers
{
    public class SidebarPinnedController : IJson
    {
        public SidebarPinnedModel Model { get; set; }

        public EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

        private StorageFileQueryResult query;

        private string configContent;

        public string JsonFileName { get; } = "PinnedItems.json";

        private string folderPath => Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings");

        public SidebarPinnedController()
        {
            Model = new SidebarPinnedModel();
            Model.SetController(this);
        }

        public async Task InitializeAsync()
        {
            await LoadAsync();
            await StartWatchConfigChangeAsync();
        }

        public async Task ReloadAsync()
        {
            await LoadAsync();
            Model.RemoveStaleSidebarItems();
        }

        private async Task LoadAsync()
        {
            StorageFolder Folder = await FilesystemTasks.Wrap(() => ApplicationData.Current.LocalFolder.CreateFolderAsync("settings", CreationCollisionOption.OpenIfExists).AsTask());
            if (Folder == null)
            {
                Model.AddDefaultItems();
                await Model.AddAllItemsToSidebar();
                return;
            }

            var JsonFile = await FilesystemTasks.Wrap(() => Folder.GetFileAsync(JsonFileName).AsTask());
            if (!JsonFile)
            {
                if (JsonFile == FileSystemStatusCode.NotFound)
                {
                    var oldItems = await ReadV2PinnedItemsFile() ?? await ReadV1PinnedItemsFile();
                    if (oldItems != null)
                    {
                        foreach (var item in oldItems)
                        {
                            if (!Model.FavoriteItems.Contains(item))
                            {
                                Model.FavoriteItems.Add(item);
                            }
                        }
                    }
                    else
                    {
                        Model.AddDefaultItems();
                    }

                    Model.Save();
                    await Model.AddAllItemsToSidebar();
                    return;
                }
                else
                {
                    Model.AddDefaultItems();
                    await Model.AddAllItemsToSidebar();
                    return;
                }
            }

            try
            {
                configContent = await FileIO.ReadTextAsync(JsonFile.Result);
                Model = JsonConvert.DeserializeObject<SidebarPinnedModel>(configContent);
                if (Model == null)
                {
                    throw new ArgumentException($"{JsonFileName} is empty, regenerating...");
                }
                Model.SetController(this);
            }
            catch (Exception)
            {
                Model = new SidebarPinnedModel();
                Model.SetController(this);
                Model.AddDefaultItems();
                Model.Save();
            }

            await Model.AddAllItemsToSidebar();
        }

        private async Task StartWatchConfigChangeAsync()
        {
            var queryOptions = new QueryOptions();
            queryOptions.ApplicationSearchFilter = "System.FileName:" + JsonFileName;

            var settingsFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("settings");
            query = settingsFolder.CreateFileQueryWithOptions(queryOptions);
            query.ContentsChanged += Query_ContentsChanged;
            await query.GetFilesAsync();
        }

        private async void Query_ContentsChanged(IStorageQueryResultBase sender, object args)
        {
            try
            {
                var configFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/settings/PinnedItems.json"));
                var content = await FileIO.ReadTextAsync(configFile);

                if (configContent != content)
                {
                    configContent = content;
                }
                else
                {
                    return;
                }

                // Watched file changed externally, reload the sidebar items
                await ReloadAsync();
            }
            catch
            {
                // ignored
            }
        }

        public void SaveModel()
        {
            try
            {
                using (var file = File.CreateText(Path.Combine(folderPath, JsonFileName)))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Formatting = Formatting.Indented;
                    serializer.Serialize(file, Model);

                    // update local configContent to avoid unnecessary refreshes
                    configContent = JsonConvert.SerializeObject(Model, Formatting.Indented);
                }
            }
            catch
            {
                // ignored
            }
        }

        private async Task<IEnumerable<string>> ReadV1PinnedItemsFile()
        {
            return await SafetyExtensions.IgnoreExceptions(async () =>
            {
                var oldPinnedItemsFile = await ApplicationData.Current.LocalCacheFolder.GetFileAsync("PinnedItems.txt");
                var oldPinnedItems = await FileIO.ReadLinesAsync(oldPinnedItemsFile);
                await oldPinnedItemsFile.DeleteAsync();
                return oldPinnedItems;
            });
        }

        private async Task<IEnumerable<string>> ReadV2PinnedItemsFile()
        {
            return await SafetyExtensions.IgnoreExceptions(async () =>
            {
                var oldPinnedItemsFile = await ApplicationData.Current.LocalCacheFolder.GetFileAsync("PinnedItems.json");
                var model = JsonConvert.DeserializeObject<SidebarPinnedModel>(await FileIO.ReadTextAsync(oldPinnedItemsFile));
                await oldPinnedItemsFile.DeleteAsync();
                return model.FavoriteItems;
            });
        }
    }
}