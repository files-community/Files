using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using Files.App.ServicesImplementation.Settings;
using Files.App.Shell;
using Files.Backend.Services.Settings;
using Files.Shared.Extensions;
using Microsoft.Win32;
using SevenZip;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

namespace Files.App.ViewModels.SettingsViewModels
{
	public class AdvancedViewModel : ObservableObject
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private IBundlesSettingsService BundlesSettingsService { get; } = Ioc.Default.GetRequiredService<IBundlesSettingsService>();
		private IFileTagsSettingsService FileTagsSettingsService { get; } = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();


		public ICommand EditFileTagsCommand { get; }

		public ICommand SetAsDefaultExplorerCommand { get; }

		public ICommand SetAsOpenFileDialogCommand { get; }

		public ICommand ExportSettingsCommand { get; }

		public ICommand ImportSettingsCommand { get; }

		public ICommand OpenSettingsJsonCommand { get; }


		public AdvancedViewModel()
		{
			IsSetAsDefaultFileManager = DetectIsSetAsDefaultFileManager();
			IsSetAsOpenFileDialog = DetectIsSetAsOpenFileDialog();

			EditFileTagsCommand = new AsyncRelayCommand(LaunchFileTagsConfigFile);

			SetAsDefaultExplorerCommand = new AsyncRelayCommand(SetAsDefaultExplorer);
			SetAsOpenFileDialogCommand = new AsyncRelayCommand(SetAsOpenFileDialog);

			ExportSettingsCommand = new AsyncRelayCommand(ExportSettings);
			ImportSettingsCommand = new AsyncRelayCommand(ImportSettings);
			OpenSettingsJsonCommand = new AsyncRelayCommand(OpenSettingsJson);
		}
		
		private async Task OpenSettingsJson()
		{
			var settingsJsonPath = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/settings/user_settings.json"));
			
			if (!await Launcher.LaunchFileAsync(settingsJsonPath))
			{
				await ContextMenu.InvokeVerb("open", settingsJsonPath.Path);
			}
		}

		private async Task LaunchFileTagsConfigFile()
		{
			var configFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/settings/filetags.json"));
			
			if (!await Launcher.LaunchFileAsync(configFile))
			{
				await ContextMenu.InvokeVerb("open", configFile.Path);
			}
		}

		private async Task SetAsDefaultExplorer()
		{
			await Task.Yield(); // Make sure IsSetAsDefaultFileManager is updated
			if (IsSetAsDefaultFileManager == DetectIsSetAsDefaultFileManager())
			{
				return;
			}

			var destFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "FilesOpenDialog");
			Directory.CreateDirectory(destFolder);
			foreach (var file in Directory.GetFiles(Path.Combine(Package.Current.InstalledLocation.Path, "Files.App", "Assets", "FilesOpenDialog")))
			{
				if (!SafetyExtensions.IgnoreExceptions(() => File.Copy(file, Path.Combine(destFolder, Path.GetFileName(file)), true), App.Logger))
				{
					// Error copying files
					goto DetectResult;
				}
			}

			var dataPath = Environment.ExpandEnvironmentVariables("%LocalAppData%\\Files");
			if (IsSetAsDefaultFileManager)
			{
				if (!Win32API.RunPowershellCommand($"-command \"New-Item -Force -Path '{dataPath}' -ItemType Directory; Copy-Item -Filter *.* -Path '{destFolder}\\*' -Recurse -Force -Destination '{dataPath}'\"", false))
				{
					// Error copying files
					goto DetectResult;
				}
			}
			else
			{
				Win32API.RunPowershellCommand($"-command \"Remove-Item -Path '{dataPath}' -Recurse -Force\"", false);
			}

			try
			{
				using var regProcess = Process.Start(new ProcessStartInfo("regedit.exe", @$"/s ""{Path.Combine(destFolder, IsSetAsDefaultFileManager ? "SetFilesAsDefault.reg" : "UnsetFilesAsDefault.reg")}""") { UseShellExecute = true, Verb = "runas" });
				await regProcess.WaitForExitAsync();
			}
			catch
			{
				// Canceled UAC
			}

		DetectResult:
			IsSetAsDefaultFileManager = DetectIsSetAsDefaultFileManager();
			if (!IsSetAsDefaultFileManager)
			{
				IsSetAsOpenFileDialog = false;
				await SetAsOpenFileDialog();
			}
		}

		private async Task SetAsOpenFileDialog()
		{
			await Task.Yield(); // Make sure IsSetAsDefaultFileManager is updated
			if (IsSetAsOpenFileDialog == DetectIsSetAsOpenFileDialog())
			{
				return;
			}

			var destFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "FilesOpenDialog");
			Directory.CreateDirectory(destFolder);
			foreach (var file in Directory.GetFiles(Path.Combine(Package.Current.InstalledLocation.Path, "Files.App", "Assets", "FilesOpenDialog")))
			{
				if (!SafetyExtensions.IgnoreExceptions(() => File.Copy(file, Path.Combine(destFolder, Path.GetFileName(file)), true), App.Logger))
				{
					// Error copying files
					goto DetectResult;
				}
			}

			try
			{
				using var regProc32 = Process.Start("regsvr32.exe", @$"/s /n {(!IsSetAsOpenFileDialog ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "CustomOpenDialog32.dll")}""");
				await regProc32.WaitForExitAsync();
				using var regProc64 = Process.Start("regsvr32.exe", @$"/s /n {(!IsSetAsOpenFileDialog ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "CustomOpenDialog64.dll")}""");
				await regProc64.WaitForExitAsync();
				using var regProcARM64 = Process.Start("regsvr32.exe", @$"/s /n {(!IsSetAsOpenFileDialog ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "CustomOpenDialogARM64.dll")}""");
				await regProcARM64.WaitForExitAsync();
			}
			catch
			{
			}

		DetectResult:
			IsSetAsOpenFileDialog = DetectIsSetAsOpenFileDialog();
		}

		private async Task ImportSettings()
		{
			FileOpenPicker filePicker = this.InitializeWithWindow(new FileOpenPicker());
			filePicker.FileTypeFilter.Add(".zip");

			StorageFile file = await filePicker.PickSingleFileAsync();
			if (file is not null)
			{
				try
				{
					var zipFolder = await ZipStorageFolder.FromStorageFileAsync(file);
					if (zipFolder is null)
					{
						return;
					}
					var localFolderPath = ApplicationData.Current.LocalFolder.Path;
					var settingsFolder = await StorageFolder.GetFolderFromPathAsync(Path.Combine(localFolderPath, Constants.LocalSettings.SettingsFolderName));
					// Import user settings
					var userSettingsFile = await zipFolder.GetFileAsync(Constants.LocalSettings.UserSettingsFileName);
					string importSettings = await userSettingsFile.ReadTextAsync();
					UserSettingsService.ImportSettings(importSettings);
					// Import bundles
					var bundles = await zipFolder.GetFileAsync(Constants.LocalSettings.BundlesSettingsFileName);
					string importBundles = await bundles.ReadTextAsync();
					BundlesSettingsService.ImportSettings(importBundles);
					// Import pinned items
					var pinnedItems = await zipFolder.GetFileAsync(App.SidebarPinnedController.JsonFileName);
					await pinnedItems.CopyAsync(settingsFolder, pinnedItems.Name, NameCollisionOption.ReplaceExisting);
					await App.SidebarPinnedController.ReloadAsync();
					// Import file tags list and DB
					var fileTagsList = await zipFolder.GetFileAsync(Constants.LocalSettings.FileTagSettingsFileName);
					string importTags = await fileTagsList.ReadTextAsync();
					FileTagsSettingsService.ImportSettings(importTags);
					var fileTagsDB = await zipFolder.GetFileAsync(Path.GetFileName(FileTagsHelper.FileTagsDbPath));
					string importTagsDB = await fileTagsDB.ReadTextAsync();
					var tagDbInstance = FileTagsHelper.GetDbInstance();
					tagDbInstance.Import(importTagsDB);
					// Import layout preferences and DB
					var layoutPrefsDB = await zipFolder.GetFileAsync(Path.GetFileName(FolderSettingsViewModel.LayoutSettingsDbPath));
					string importPrefsDB = await layoutPrefsDB.ReadTextAsync();
					var layoutDbInstance = FolderSettingsViewModel.GetDbInstance();
					layoutDbInstance.Import(importPrefsDB);
				}
				catch (Exception ex)
				{
					App.Logger.Warn(ex, "Error importing settings");
					UIHelpers.CloseAllDialogs();
					await DialogDisplayHelper.ShowDialogAsync("SettingsImportErrorTitle".GetLocalizedResource(), "SettingsImportErrorDescription".GetLocalizedResource());
				}
			}
		}

		private async Task ExportSettings()
		{
			FileSavePicker filePicker = this.InitializeWithWindow(new FileSavePicker());
			filePicker.FileTypeChoices.Add("Zip File", new[] { ".zip" });
			filePicker.SuggestedFileName = $"Files_{App.AppVersion}";

			StorageFile file = await filePicker.PickSaveFileAsync();
			if (file is not null)
			{
				try
				{
					await ZipStorageFolder.InitArchive(file, OutArchiveFormat.Zip);
					var zipFolder = (ZipStorageFolder)await ZipStorageFolder.FromStorageFileAsync(file);
					if (zipFolder is null)
					{
						return;
					}
					var localFolderPath = ApplicationData.Current.LocalFolder.Path;
					// Export user settings
					var exportSettings = UTF8Encoding.UTF8.GetBytes((string)UserSettingsService.ExportSettings());
					await zipFolder.CreateFileAsync(new MemoryStream(exportSettings), Constants.LocalSettings.UserSettingsFileName, CreationCollisionOption.ReplaceExisting);
					// Export bundles
					var exportBundles = UTF8Encoding.UTF8.GetBytes((string)BundlesSettingsService.ExportSettings());
					await zipFolder.CreateFileAsync(new MemoryStream(exportBundles), Constants.LocalSettings.BundlesSettingsFileName, CreationCollisionOption.ReplaceExisting);
					// Export pinned items
					var pinnedItems = await BaseStorageFile.GetFileFromPathAsync(Path.Combine(localFolderPath, Constants.LocalSettings.SettingsFolderName, App.SidebarPinnedController.JsonFileName));
					await pinnedItems.CopyAsync(zipFolder, pinnedItems.Name, NameCollisionOption.ReplaceExisting);
					// Export file tags list and DB
					var exportTags = UTF8Encoding.UTF8.GetBytes((string)FileTagsSettingsService.ExportSettings());
					await zipFolder.CreateFileAsync(new MemoryStream(exportTags), Constants.LocalSettings.FileTagSettingsFileName, CreationCollisionOption.ReplaceExisting);
					var tagDbInstance = FileTagsHelper.GetDbInstance();
					byte[] exportTagsDB = UTF8Encoding.UTF8.GetBytes(tagDbInstance.Export());
					await zipFolder.CreateFileAsync(new MemoryStream(exportTagsDB), Path.GetFileName(FileTagsHelper.FileTagsDbPath), CreationCollisionOption.ReplaceExisting);
					// Export layout preferences DB
					var layoutDbInstance = FolderSettingsViewModel.GetDbInstance();
					byte[] exportPrefsDB = UTF8Encoding.UTF8.GetBytes(layoutDbInstance.Export());
					await zipFolder.CreateFileAsync(new MemoryStream(exportPrefsDB), Path.GetFileName(FolderSettingsViewModel.LayoutSettingsDbPath), CreationCollisionOption.ReplaceExisting);
				}
				catch (Exception ex)
				{
					App.Logger.Warn(ex, "Error exporting settings");
				}
			}
		}

		private bool DetectIsSetAsDefaultFileManager()
		{
			using var subkey = Registry.ClassesRoot.OpenSubKey(@"Folder\shell\open\command");
			var command = (string?)subkey?.GetValue(string.Empty);
			return !string.IsNullOrEmpty(command) && command.Contains("FilesLauncher.exe");
		}

		private bool DetectIsSetAsOpenFileDialog()
		{
			using var subkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\CLSID\{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}");
			return subkey?.GetValue(string.Empty) as string == "FilesOpenDialog class";
		}

		private bool isSetAsDefaultFileManager;

		public bool IsSetAsDefaultFileManager
		{
			get => isSetAsDefaultFileManager;
			set => SetProperty(ref isSetAsDefaultFileManager, value);
		}

		private bool isSetAsOpenFileDialog;

		public bool IsSetAsOpenFileDialog
		{
			get => isSetAsOpenFileDialog;
			set => SetProperty(ref isSetAsOpenFileDialog, value);
		}

		private FileSavePicker InitializeWithWindow(FileSavePicker obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
			return obj;
		}

		private FileOpenPicker InitializeWithWindow(FileOpenPicker obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
			return obj;
		}
	}
}
