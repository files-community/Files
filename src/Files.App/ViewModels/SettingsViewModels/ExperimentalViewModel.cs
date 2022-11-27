using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.App.Shell;
using Files.Backend.Services.Settings;
using Files.Shared.Extensions;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.System;

namespace Files.App.ViewModels.SettingsViewModels
{
	public class ExperimentalViewModel : ObservableObject
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public ICommand EditFileTagsCommand { get; }

		public ICommand SetAsDefaultExplorerCommand { get; }

		public ICommand SetAsOpenFileDialogCommand { get; }

		public ExperimentalViewModel()
		{
			IsSetAsDefaultFileManager = DetectIsSetAsDefaultFileManager();
			IsSetAsOpenFileDialog = DetectIsSetAsOpenFileDialog();

			EditFileTagsCommand = new AsyncRelayCommand(LaunchFileTagsConfigFile);
			SetAsDefaultExplorerCommand = new AsyncRelayCommand(SetAsDefaultExplorer);
			SetAsOpenFileDialogCommand = new AsyncRelayCommand(SetAsOpenFileDialog);
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
	}
}
