// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.StorageItems;
using Files.App.Shell;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SevenZip;
using System.IO;
using System.Text;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

namespace Files.App.ViewModels.Settings
{
	public class AdvancedViewModel : ObservableObject
	{
		private IUserSettingsService UserSettingsService { get; }

		private IBundlesSettingsService BundlesSettingsService { get; }
		
		private IFileTagsSettingsService FileTagsSettingsService { get; }

		public ICommand SetAsDefaultExplorerCommand { get; }
		public ICommand SetAsOpenFileDialogCommand { get; }
		public ICommand ExportSettingsCommand { get; }
		public ICommand ImportSettingsCommand { get; }
		public ICommand OpenSettingsJsonCommand { get; }

		private bool _IsSetAsDefaultFileManager;
		public bool IsSetAsDefaultFileManager
		{
			get => _IsSetAsDefaultFileManager;
			set => SetProperty(ref _IsSetAsDefaultFileManager, value);
		}

		private bool _IsSetAsOpenFileDialog;
		public bool IsSetAsOpenFileDialog
		{
			get => _IsSetAsOpenFileDialog;
			set => SetProperty(ref _IsSetAsOpenFileDialog, value);
		}

		public AdvancedViewModel()
		{
			UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			BundlesSettingsService = Ioc.Default.GetRequiredService<IBundlesSettingsService>();
			FileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();

			IsSetAsDefaultFileManager = DetectIsSetAsDefaultFileManager();
			IsSetAsOpenFileDialog = DetectIsSetAsOpenFileDialog();

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
				await ContextMenu.InvokeVerb("open", settingsJsonPath.Path);
		}

		private async Task SetAsDefaultExplorer()
		{
			// Make sure IsSetAsDefaultFileManager is updated
			await Task.Yield();

			if (IsSetAsDefaultFileManager == DetectIsSetAsDefaultFileManager())
				return;

			var destFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "FilesOpenDialog");
			Directory.CreateDirectory(destFolder);

			foreach (var file in Directory.GetFiles(Path.Combine(Package.Current.InstalledLocation.Path, "Files.App", "Assets", "FilesOpenDialog")))
			{
				if (!SafetyExtensions.IgnoreExceptions(() => File.Copy(file, Path.Combine(destFolder, Path.GetFileName(file)), true), App.Logger))
				{
					// Error copying files
					await DetectResult();

					return;
				}
			}

			var dataPath = Environment.ExpandEnvironmentVariables("%LocalAppData%\\Files");
			if (IsSetAsDefaultFileManager)
			{
				if (!Win32API.RunPowershellCommand($"-command \"New-Item -Force -Path '{dataPath}' -ItemType Directory; Copy-Item -Filter *.* -Path '{destFolder}\\*' -Recurse -Force -Destination '{dataPath}'\"", false))
				{
					// Error copying files
					await DetectResult();

					return;
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

			await DetectResult();
		}

		private async Task DetectResult()
		{
			IsSetAsDefaultFileManager = DetectIsSetAsDefaultFileManager();
			if (!IsSetAsDefaultFileManager)
			{
				IsSetAsOpenFileDialog = false;
				await SetAsOpenFileDialog();
			}
		}

		private async Task SetAsOpenFileDialog()
		{
			// Make sure IsSetAsDefaultFileManager is updated
			await Task.Yield();
			if (IsSetAsOpenFileDialog == DetectIsSetAsOpenFileDialog())
				return;

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
			FileOpenPicker filePicker = InitializeWithWindow(new FileOpenPicker());
			filePicker.FileTypeFilter.Add(".zip");

			StorageFile file = await filePicker.PickSingleFileAsync();
			if (file is not null)
			{
				try
				{
					var zipFolder = await ZipStorageFolder.FromStorageFileAsync(file);
					if (zipFolder is null)
						return;

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
					App.Logger.LogWarning(ex, "Error importing settings");
					UIHelpers.CloseAllDialogs();
					await DialogDisplayHelper.ShowDialogAsync("SettingsImportErrorTitle".GetLocalizedResource(), "SettingsImportErrorDescription".GetLocalizedResource());
				}
			}
		}

		private async Task ExportSettings()
		{
			FileSavePicker filePicker = InitializeWithWindow(new FileSavePicker());
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
						return;

					var localFolderPath = ApplicationData.Current.LocalFolder.Path;

					// Export user settings
					var exportSettings = UTF8Encoding.UTF8.GetBytes((string)UserSettingsService.ExportSettings());
					await zipFolder.CreateFileAsync(new MemoryStream(exportSettings), Constants.LocalSettings.UserSettingsFileName, CreationCollisionOption.ReplaceExisting);

					// Export bundles
					var exportBundles = UTF8Encoding.UTF8.GetBytes((string)BundlesSettingsService.ExportSettings());
					await zipFolder.CreateFileAsync(new MemoryStream(exportBundles), Constants.LocalSettings.BundlesSettingsFileName, CreationCollisionOption.ReplaceExisting);

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
					App.Logger.LogWarning(ex, "Error exporting settings");
				}
			}
		}

		private static bool DetectIsSetAsDefaultFileManager()
		{
			using var subkey = Registry.ClassesRoot.OpenSubKey(@"Folder\shell\open\command");
			var command = (string?)subkey?.GetValue(string.Empty);

			return !string.IsNullOrEmpty(command) && command.Contains("FilesLauncher.exe");
		}

		private static bool DetectIsSetAsOpenFileDialog()
		{
			using var subkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\CLSID\{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}");

			return subkey?.GetValue(string.Empty) as string == "FilesOpenDialog class";
		}

		private static FileSavePicker InitializeWithWindow(FileSavePicker obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);

			return obj;
		}

		private static FileOpenPicker InitializeWithWindow(FileOpenPicker obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);

			return obj;
		}
	}
}
