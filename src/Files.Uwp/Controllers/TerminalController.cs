using Files.Uwp.DataModels;
using Files.Uwp.Filesystem;
using Files.Uwp.Helpers;
using Files.Shared.Enums;
using Files.Shared.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace Files.Uwp.Controllers
{
    public class TerminalController : IJson
    {
        private string defaultTerminalPath = "ms-appx:///Assets/terminal/terminal.json";

        private string folderPath => Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings");

        private StorageFileQueryResult query;

        private string configContent;

        public TerminalFileModel Model { get; set; }

        public event Action<TerminalController> ModelChanged;

        public string JsonFileName { get; } = "terminal.json";

        public TerminalController()
        {
            Model = new TerminalFileModel();
        }

        public async Task InitializeAsync()
        {
            await LoadAsync();
            await GetInstalledTerminalsAsync();
            await StartWatchConfigChangeAsync();
            ModelChanged?.Invoke(this);
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
                configContent = await FileIO.ReadTextAsync(JsonFile.Result);
                Model = JsonConvert.DeserializeObject<TerminalFileModel>(configContent);
                if (Model == null)
                {
                    throw new ArgumentException($"{JsonFileName} is empty, regenerating...");
                }
            }
            catch (ArgumentException)
            {
                Model = await GetDefaultTerminalFileModel();
                SaveModel();
            }
            catch (Exception)
            {
                Model = await GetDefaultTerminalFileModel();
            }
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
                var configFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/settings/terminal.json"));
                var content = await FileIO.ReadTextAsync(configFile);

                if (configContent != content)
                {
                    configContent = content;
                }
                else
                {
                    return;
                }

                await LoadAsync();
                await GetInstalledTerminalsAsync();
                ModelChanged?.Invoke(this);
            }
            catch
            {
                // ignored
            }
        }

        private async Task<TerminalFileModel> GetDefaultTerminalFileModel()
        {
            try
            {
                StorageFile defaultFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(defaultTerminalPath));
                var defaultContent = await FileIO.ReadTextAsync(defaultFile);
                var model = JsonConvert.DeserializeObject<TerminalFileModel>(defaultContent);
                await GetInstalledTerminalsAsync(model);
                model.ResetToDefaultTerminal();
                return model;
            }
            catch
            {
                var model = new TerminalFileModel();
                await GetInstalledTerminalsAsync(model);
                model.ResetToDefaultTerminal();
                return model;
            }
        }

        private async Task GetInstalledTerminalsAsync()
        {
            await GetInstalledTerminalsAsync(Model);
            SaveModel();
        }

        private async Task GetInstalledTerminalsAsync(TerminalFileModel model)
        {
            var terminalDefs = new Dictionary<Terminal, bool>();

            terminalDefs.Add(new Terminal()
            {
                Name = "Windows Terminal",
                Path = "wt.exe",
                Arguments = "-d .",
                Icon = ""
            }, await IsWindowsTerminalBuildInstalled());

            terminalDefs.Add(new Terminal()
            {
                Name = "Fluent Terminal",
                Path = "flute.exe",
                Arguments = "",
                Icon = ""
            }, await PackageHelper.IsAppInstalledAsync("53621FSApps.FluentTerminal_87x1pks76srcp"));

            terminalDefs.Add(new Terminal()
            {
                Name = "CMD",
                Path = "cmd.exe",
                Arguments = "",
                Icon = ""
            }, true);    // CMD will always be present (for now at least)

            terminalDefs.Where(x => x.Value).ForEach(x => model.AddTerminal(x.Key));
            terminalDefs.Where(x => !x.Value).ForEach(x => model.RemoveTerminal(x.Key));
        }

        public async static Task<bool> IsWindowsTerminalBuildInstalled()
        {
            bool isWindowsTerminalInstalled = await PackageHelper.IsAppInstalledAsync("Microsoft.WindowsTerminal_8wekyb3d8bbwe");
            bool isWindowsTerminalPreviewInstalled = await PackageHelper.IsAppInstalledAsync("Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe");

            return isWindowsTerminalPreviewInstalled || isWindowsTerminalInstalled;
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
    }
}