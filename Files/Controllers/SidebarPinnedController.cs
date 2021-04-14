using Files.DataModels;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Controllers
{
    public class SidebarPinnedController : IJson
    {
        private StorageFolder Folder { get; set; }
        private StorageFile JsonFile { get; set; }

        public SidebarPinnedModel Model { get; set; }
        public string JsonFileName { get; } = "PinnedItems.json";

        private SidebarPinnedController()
        {
            Model = new SidebarPinnedModel();
            Model.SetController(this);
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

            try
            {
                JsonFile = await Folder.GetFileAsync(JsonFileName);
            }
            catch (FileNotFoundException)
            {
                try
                {
                    var oldPinnedItemsFile = await Folder.GetFileAsync("PinnedItems.txt");
                    var oldPinnedItems = await FileIO.ReadLinesAsync(oldPinnedItemsFile);
                    await oldPinnedItemsFile.DeleteAsync();

                    foreach (var line in oldPinnedItems)
                    {
                        if (!Model.FavoriteItems.Contains(line))
                        {
                            Model.FavoriteItems.Add(line);
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    Model.AddDefaultItems();
                }

                JsonFile = await Folder.CreateFileAsync(JsonFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(JsonFile, JsonConvert.SerializeObject(Model, Formatting.Indented));
            }

            try
            {
                Model = JsonConvert.DeserializeObject<SidebarPinnedModel>(await FileIO.ReadTextAsync(JsonFile));
                if (Model == null)
                {
                    throw new Exception($"{JsonFileName} is empty, regenerating...");
                }
                Model.SetController(this);
            }
            catch (Exception)
            {
                await JsonFile.DeleteAsync();
                Model = new SidebarPinnedModel();
                Model.SetController(this);
                Model.AddDefaultItems();
                Model.Save();
            }

            await Model.AddAllItemsToSidebar();
        }

        public void SaveModel()
        {
            using (var file = File.CreateText(ApplicationData.Current.LocalCacheFolder.Path + Path.DirectorySeparatorChar + JsonFileName))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, Model);
            }
        }
    }
}