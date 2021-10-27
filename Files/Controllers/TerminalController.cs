using Files.DataModels;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Search;

namespace Files.Controllers
{
    public class TerminalController : IJson
    {
        private string defaultTerminalPath = "ms-appx:///Assets/terminal/terminal.json";

        private string folderPath => Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings");

        private StorageFileQueryResult query;

        private bool suppressChangeEvent;

        private string configContent;

        public TerminalFileModel Model { get; set; }

        public event Action<TerminalController> ModelChanged;

        public string JsonFileName { get; } = "terminal.json";

        public TerminalController()
        {
        }

        public async Task InitializeAsync()
        {
            await LoadAsync();
            await GetInstalledTerminalsAsync();
            await StartWatchConfigChangeAsync();
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
                configContent = content;
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

        private async Task StartWatchConfigChangeAsync()
        {
            var configFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/settings/terminal.json"));
            var folder = await configFile.GetParentAsync();
            query = folder.CreateFileQuery();
            query.ContentsChanged += Query_ContentsChanged;
            await query.GetFilesAsync();
        }

        private async void Query_ContentsChanged(IStorageQueryResultBase sender, object args)
        {
            if (suppressChangeEvent)
            {
                suppressChangeEvent = false;
                return;
            }

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
                CoreApplication.MainView.DispatcherQueue.TryEnqueue(() =>
                {
                    ModelChanged?.Invoke(this);
                });
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
                return JsonConvert.DeserializeObject<TerminalFileModel>(defaultContent);
            }
            catch
            {
                var model = new TerminalFileModel();

                if (await IsWindowsTerminalBuildInstalled())
                {
                    model.Terminals.Add(new Terminal()
                    {
                        Name = "Windows Terminal",
                        Path = "wt.exe",
                        Arguments = "-d .",
                        Icon = ""
                    });
                }
                else
                {
                    model.Terminals.Add(new Terminal()
                    {
                        Name = "CMD",
                        Path = "cmd.exe",
                        Arguments = "",
                        Icon = ""
                    });
                }
                
                model.ResetToDefaultTerminal();
                return model;
            }
        }

        public async Task GetInstalledTerminalsAsync()
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
            }, true);   // CMD will always be present (for now at least)

            foreach(KeyValuePair<Terminal, bool> terminalItem in terminalDefs)
            {
                if (terminalItem.Value)
                {
                    Model.AddTerminal(terminalItem.Key);
                }
                else
                {
                    Model.RemoveTerminal(terminalItem.Key);
                }
            }

            SaveModel();
        }

        public async static Task<bool> IsWindowsTerminalBuildInstalled()
        {
            bool isWindowsTerminalInstalled = await PackageHelper.IsAppInstalledAsync("Microsoft.WindowsTerminal_8wekyb3d8bbwe");
            bool isWindowsTerminalPreviewInstalled = await PackageHelper.IsAppInstalledAsync("Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe");

            return isWindowsTerminalPreviewInstalled || isWindowsTerminalInstalled;
        }

        public void SaveModel()
        {
            suppressChangeEvent = true;
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