using Files.App.DataModels;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Serialization.Implementation;
using Files.Shared.Enums;
using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace Files.App.Controllers
{
	public class TerminalController : IJson
	{
		private const string defaultTerminalPath = "ms-appx:///Assets/terminal/terminal.json";

		private string configContent = string.Empty;

		public event Action<TerminalController>? ModelChanged;

		public string JsonFileName => "terminal.json";

		private TerminalFileModel model = new();
		public TerminalFileModel Model => model;

		public async Task InitializeAsync()
		{
			await LoadAsync();
			await GetInstalledTerminalsAsync(model);
			SaveModel();
			await StartWatchConfigChangeAsync();
			ModelChanged?.Invoke(this);
		}

		public void SaveModel()
		{
			try
			{
				var path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings", JsonFileName);
				using var file = File.CreateText(path);

				// update local configContent to avoid unnecessary refreshes
				configContent = JsonSerializer.Serialize(model, DefaultJsonSettingsSerializer.Options);
				file.Write(configContent);
			}
			catch
			{
			}
		}

		private async Task LoadAsync()
		{
			StorageFolder Folder = await FilesystemTasks.Wrap(() =>
				ApplicationData.Current.LocalFolder.CreateFolderAsync("settings", CreationCollisionOption.OpenIfExists)
			.AsTask());

			if (Folder is null)
			{
				model = await GetDefaultTerminalFileModel();
				return;
			}

			var jsonFile = await FilesystemTasks.Wrap(() =>
				Folder.GetFileAsync(JsonFileName)
			.AsTask());

			if (!jsonFile)
			{
				model = await GetDefaultTerminalFileModel();
				if (jsonFile.ErrorCode is FileSystemStatusCode.NotFound)
					SaveModel();
				return;
			}

			try
			{
				configContent = await FileIO.ReadTextAsync(jsonFile.Result);

				var fileModel = JsonSerializer.Deserialize<TerminalFileModel>(configContent);
				if (fileModel is null)
					throw new ArgumentException($"{JsonFileName} is empty, regenerating...");
				model = fileModel!;
			}
			catch (ArgumentException)
			{
				model = await GetDefaultTerminalFileModel();
				SaveModel();
			}
			catch (Exception)
			{
				model = await GetDefaultTerminalFileModel();
			}
		}

		private async Task StartWatchConfigChangeAsync()
		{
			var queryOptions = new QueryOptions
			{
				ApplicationSearchFilter = "System.FileName:" + JsonFileName
			};

			var settingsFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("settings");
			var query = settingsFolder.CreateFileQueryWithOptions(queryOptions);
			query.ContentsChanged += Query_ContentsChanged;
			await query.GetFilesAsync();
		}

		private async void Query_ContentsChanged(IStorageQueryResultBase sender, object args)
		{
			try
			{
				var uri = new Uri("ms-appdata:///local/settings/terminal.json");
				var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
				var content = await FileIO.ReadTextAsync(file);

				if (configContent == content)
					return;

				configContent = content;
				await LoadAsync();
				await GetInstalledTerminalsAsync(model);
				SaveModel();
				ModelChanged?.Invoke(this);
			}
			catch {}
		}

		private async Task<TerminalFileModel> GetDefaultTerminalFileModel()
		{
			try
			{
				var uri = new Uri(defaultTerminalPath);
				var defaultFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
				var defaultContent = await FileIO.ReadTextAsync(defaultFile);

				var fileModel = JsonSerializer.Deserialize<TerminalFileModel>(configContent);
				if (fileModel is null)
					throw new ArgumentException($"{JsonFileName} is empty, regenerating...");
				model = fileModel!;

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

		private static async Task GetInstalledTerminalsAsync(TerminalFileModel model)
		{
			var terminalDefs = new Dictionary<Terminal, bool>
			{
				{
					new Terminal
					{
						Name = "Windows Terminal",
						Path = "wt.exe",
						Arguments = "-d .",
						Icon = string.Empty,
					},
					await IsWindowsTerminalBuildInstalled()
				},
				{
					new Terminal
					{
						Name = "Fluent Terminal",
						Path = "flute.exe",
						Arguments = string.Empty,
						Icon = string.Empty,
					},
					await IsFluentTerminalBuildInstalled()
				},
				{
					new Terminal
					{
						Name = "CMD",
						Path = "cmd.exe",
						Arguments = string.Empty,
						Icon = string.Empty,
					},
					true // CMD will always be present (for now at least)
				}
			};

			terminalDefs.Where(x => x.Value).ForEach(x => model.AddTerminal(x.Key));
			terminalDefs.Where(x => !x.Value).ForEach(x => model.RemoveTerminal(x.Key));

			async static Task<bool> IsWindowsTerminalBuildInstalled()
				=> await PackageHelper.IsAppInstalledAsync("Microsoft.WindowsTerminal_8wekyb3d8bbwe")
				|| await PackageHelper.IsAppInstalledAsync("Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe");

			async static Task<bool> IsFluentTerminalBuildInstalled()
				=> await PackageHelper.IsAppInstalledAsync("53621FSApps.FluentTerminal_87x1pks76srcp");
		}
	}
}