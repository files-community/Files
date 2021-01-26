using Files.DataModels;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
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

        private TerminalController()
        {
        }

        public static Task<TerminalController> CreateInstance()
        {
            var instance = new TerminalController();
            return instance.InitializeAsync();
        }

        private async Task<TerminalController> InitializeAsync()
        {
            await LoadAsync();
            await GetInstalledTerminalsAsync();
            return this;
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
                if (JsonFile == FileSystemStatusCode.NotFound)
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

            bool isWindowsTerminalInstalled = await PackageHelper.IsAppInstalledAsync("Microsoft.WindowsTerminal_8wekyb3d8bbwe");
            bool isWindowsTerminalPreviewInstalled = await PackageHelper.IsAppInstalledAsync("Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe");
            bool isFluentTerminalInstalled = await PackageHelper.IsAppInstalledAsync("53621FSApps.FluentTerminal_87x1pks76srcp");

            if (isWindowsTerminalInstalled || isWindowsTerminalPreviewInstalled)
            {
                Model.AddTerminal(windowsTerminal);
            }
            else
            {
                Model.RemoveTerminal(windowsTerminal);
            }

            if (isFluentTerminalInstalled)
            {
                Model.AddTerminal(fluentTerminal);
            }
            else
            {
                Model.RemoveTerminal(fluentTerminal);
            }

            SaveModel();
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