using Files.DataModels;
using Files.Enums;
using Files.Filesystem;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Controllers
{
    public class SidebarPinnedController : IJson
    {
        private StorageFolder Folder { get; set; }

        public SidebarPinnedModel Model { get; set; } = new SidebarPinnedModel();

        public string JsonFileName { get; } = "PinnedItems.json";

        private SidebarPinnedController()
        {
        }

        public static Task<SidebarPinnedController> CreateInstance()
        {
            var instance = new SidebarPinnedController();
            return instance.InitializeAsync();
        }

        private async Task<SidebarPinnedController> InitializeAsync()
        {
            await LoadAsync();
            return this;
        }

        private async Task LoadAsync()
        {
            Folder = ApplicationData.Current.LocalCacheFolder;
            var JsonFile = await FilesystemTasks.Wrap(() => Folder.GetFileAsync(JsonFileName).AsTask());

            if (JsonFile == FileSystemStatusCode.NotFound)
            {
                var oldPinnedItemsContents = await FilesystemTasks.Wrap(() => Folder.GetFileAsync("PinnedItems.txt").AsTask())
                    .OnSuccess(async (oldPinnedItemsFile) =>
                    {
                        var contents = await FileIO.ReadLinesAsync(oldPinnedItemsFile);
                        await oldPinnedItemsFile.DeleteAsync();
                        return contents;
                    });
                if (oldPinnedItemsContents)
                {
                    foreach (var line in oldPinnedItemsContents.Result)
                    {
                        if (!Model.Items.Contains(line))
                        {
                            Model.Items.Add(line);
                        }
                    }
                }
                else
                {
                    Model.AddDefaultItems();
                }

                JsonFile = await FilesystemTasks.Wrap(() => Folder.CreateFileAsync(JsonFileName, CreationCollisionOption.ReplaceExisting).AsTask())
                    .OnSuccess(async (t) =>
                    {
                        await FileIO.WriteTextAsync(t, JsonConvert.SerializeObject(Model, Formatting.Indented)).AsTask();
                        return t;
                    });
            }

            try
            {
                Model = JsonConvert.DeserializeObject<SidebarPinnedModel>(await FileIO.ReadTextAsync(JsonFile.Result));
                if (Model == null)
                {
                    throw new Exception($"{JsonFileName} is empty, regenerating...");
                }
            }
            catch (Exception)
            {
                Model = new SidebarPinnedModel();
                Model.AddDefaultItems();
                Model.Save();
            }

            Model.AddAllItemsToSidebar();
        }

        public void SaveModel()
        {
            try
            {
                using (var file = File.CreateText(ApplicationData.Current.LocalCacheFolder.Path + Path.DirectorySeparatorChar + JsonFileName))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Formatting = Formatting.Indented;
                    serializer.Serialize(file, Model);
                }
            }
            catch
            {
            }
        }
    }
}