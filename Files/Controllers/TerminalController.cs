using Files.DataModels;
using Files.Filesystem;
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

        private string folderPath => Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings");

        public TerminalFileModel Model { get; set; }

        public string JsonFileName { get; } = "terminal.json";

        public TerminalController()
        {
            Init();
        }

        private async Task LoadAsync()
        {
            StorageFolder Folder = await FilesystemTasks.Wrap(() => ApplicationData.Current.LocalFolder.CreateFolderAsync("settings", CreationCollisionOption.OpenIfExists).AsTask());
            if (Folder == null)
            {
                Model = await GetDefaultTerminalFileModel();
                return;
            }

            var JsonFile = await FilesystemTasks.Wrap(() => Folder.GetFileAsync(JsonFileName).AsTask());
            if (!JsonFile)
            {
                if (JsonFile == FilesystemErrorCode.ERROR_NOTFOUND)
                {
                    Model = await GetDefaultTerminalFileModel();
                    SaveModel();
                    return;
                }
                else
                {
                    Model = await GetDefaultTerminalFileModel();
                    return;
                }
            }

            try
            {
                var content = await FileIO.ReadTextAsync(JsonFile.Result);
                Model = JsonConvert.DeserializeObject<TerminalFileModel>(content);
                if (Model == null)
                {
                    throw new JsonParsingNullException(JsonFileName);
                }
            }
            catch (JsonParsingNullException)
            {
                Model = await GetDefaultTerminalFileModel();
                SaveModel();
            }
            catch (Exception)
            {
                Model = await GetDefaultTerminalFileModel();
            }
        }

        private async Task<TerminalFileModel> GetDefaultTerminalFileModel()
        {
            try
            {
                StorageFile defaultFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(defaultTerminalPath));
                var defaultContent = await FileIO.ReadTextAsync(defaultFile);
                return JsonConvert.DeserializeObject<TerminalFileModel>(defaultContent);
            }
            catch
            {
                var model = new TerminalFileModel();
                model.Terminals.Add(new Terminal()
                {
                    Name = "CMD",
                    Path = "cmd.exe",
                    Arguments = "",
                    Icon = ""
                });
                model.ResetToDefaultTerminal();
                return model;
            }
        }

        public async void Init()
        {
            await LoadAsync();
            await GetInstalledTerminalsAsync();
        }

        public async Task GetInstalledTerminalsAsync()
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

            bool isWindowsTerminalAddedOrRemoved = await Model.AddOrRemoveTerminalAsync(windowsTerminal, "Microsoft.WindowsTerminal_8wekyb3d8bbwe");
            bool isFluentTerminalAddedOrRemoved = await Model.AddOrRemoveTerminalAsync(fluentTerminal, "53621FSApps.FluentTerminal_87x1pks76srcp");
            if (isWindowsTerminalAddedOrRemoved || isFluentTerminalAddedOrRemoved)
            {
                SaveModel();
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
                }
            }
            catch
            {
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