using Files.DataModels;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Controllers
{
    public class TerminalController : IJson
    {
        private string defaultTerminalPath = "ms-appx:///Assets/terminal/terminal.json";

        private StorageFile JsonFile { get; set; }

        private StorageFolder Folder { get; set; }

        public TerminalFileModel Model { get; set; }

        public string JsonFileName { get; } = "terminal.json";

        public TerminalController()
        {
            Init();
        }

        private async Task Load()
        {
            Folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("settings", CreationCollisionOption.OpenIfExists);
            try
            {
                JsonFile = await Folder.GetFileAsync(JsonFileName);
            }
            catch (FileNotFoundException)
            {
                var defaultFile = StorageFile.GetFileFromApplicationUriAsync(new Uri(defaultTerminalPath));

                JsonFile = await Folder.CreateFileAsync(JsonFileName);
                await FileIO.WriteBufferAsync(JsonFile, await FileIO.ReadBufferAsync(await defaultFile));
            }

            var content = await FileIO.ReadTextAsync(JsonFile);

            try
            {
                Model = JsonConvert.DeserializeObject<TerminalFileModel>(content);
                if (Model == null)
                {
                    Model = new TerminalFileModel();
                    throw new JsonParsingNullException(JsonFileName);
                }
            }
            catch (JsonParsingNullException)
            {
                var defaultFile = StorageFile.GetFileFromApplicationUriAsync(new Uri(defaultTerminalPath));

                JsonFile = await Folder.CreateFileAsync(JsonFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteBufferAsync(JsonFile, await FileIO.ReadBufferAsync(await defaultFile));
                var defaultContent = await FileIO.ReadTextAsync(JsonFile);
                Model = JsonConvert.DeserializeObject<TerminalFileModel>(defaultContent);
            }
            catch (Exception)
            {
                var defaultFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(defaultTerminalPath));
                JsonFile = null;
                var defaultContent = await FileIO.ReadTextAsync(defaultFile);
                Model = JsonConvert.DeserializeObject<TerminalFileModel>(defaultContent);
            }
        }

        public async void Init()
        {
            await Load();
            await GetInstalledTerminals();
        }

        public async Task GetInstalledTerminals()
        {
            var windowsTerminal = new Terminal()
            {
                Name = "Windows Terminal",
                Path = "wt.exe",
                Arguments = "-d .",
                Icon = ""
            };

            var fluentTerminal = new Terminal()
            {
                Name = "Fluent Terminal",
                Path = "flute.exe",
                Arguments = "",
                Icon = ""
            };

            bool isWindowsTerminalAddedOrRemoved = await Model.AddOrRemoveTerminal(windowsTerminal, "Microsoft.WindowsTerminal_8wekyb3d8bbwe");
            bool isFluentTerminalAddedOrRemoved = await Model.AddOrRemoveTerminal(fluentTerminal, "53621FSApps.FluentTerminal_87x1pks76srcp");
            if (isWindowsTerminalAddedOrRemoved || isFluentTerminalAddedOrRemoved)
            {
                SaveModel();
            }
        }

        public void SaveModel()
        {
            if (JsonFile == null) return;

            using (var file = File.CreateText(Folder.Path + Path.DirectorySeparatorChar + JsonFileName))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, Model);
            }
        }
    }

    public class JsonParsingNullException : Exception
    {
        public JsonParsingNullException(string jsonFileName) : base($"{jsonFileName} is empty, regenerating...")
        {
        }
    }
}