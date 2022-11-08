using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using Files.Backend.Services.Settings;
using Files.Shared.Extensions;
using Microsoft.UI.Xaml.Controls;
using SevenZip;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

namespace Files.App.ViewModels.SettingsViewModels
{
	public class AboutViewModel : ObservableObject
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private IBundlesSettingsService BundlesSettingsService { get; } = Ioc.Default.GetRequiredService<IBundlesSettingsService>();
		protected IFileTagsSettingsService FileTagsSettingsService { get; } = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();

		public ICommand OpenLogLocationCommand { get; }
		public ICommand CopyVersionInfoCommand { get; }
		public ICommand SupportUsCommand { get; }

		public ICommand ExportSettingsCommand { get; }
		public ICommand ImportSettingsCommand { get; }

		public ICommand ClickAboutFeedbackItemCommand { get; }

		public AboutViewModel()
		{
			OpenLogLocationCommand = new AsyncRelayCommand(OpenLogLocation);
			CopyVersionInfoCommand = new RelayCommand(CopyVersionInfo);
			SupportUsCommand = new RelayCommand(SupportUs);

			ExportSettingsCommand = new AsyncRelayCommand(ExportSettings);
			ImportSettingsCommand = new AsyncRelayCommand(ImportSettings);

			ClickAboutFeedbackItemCommand = new AsyncRelayCommand<ItemClickEventArgs>(ClickAboutFeedbackItem);
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
					byte[] exportTagsDB;
					using (var tagDbInstance = FileTagsHelper.GetDbInstance())
					{
						exportTagsDB = UTF8Encoding.UTF8.GetBytes(tagDbInstance.Export());
					}
					await zipFolder.CreateFileAsync(new MemoryStream(exportTagsDB), Path.GetFileName(FileTagsHelper.FileTagsDbPath), CreationCollisionOption.ReplaceExisting);
					// Export layout preferences DB
					byte[] exportPrefsDB;
					using (var layoutDbInstance = FolderSettingsViewModel.GetDbInstance())
					{
						exportPrefsDB = UTF8Encoding.UTF8.GetBytes(layoutDbInstance.Export());
					}
					await zipFolder.CreateFileAsync(new MemoryStream(exportPrefsDB), Path.GetFileName(FolderSettingsViewModel.LayoutSettingsDbPath), CreationCollisionOption.ReplaceExisting);
				}
				catch (Exception ex)
				{
					App.Logger.Warn(ex, "Error exporting settings");
				}
			}
		}

		// WINUI3
		private FileSavePicker InitializeWithWindow(FileSavePicker obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
			return obj;
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
					using (var tagDbInstance = FileTagsHelper.GetDbInstance())
					{
						tagDbInstance.Import(importTagsDB);
					}
					// Import layout preferences and DB
					var layoutPrefsDB = await zipFolder.GetFileAsync(Path.GetFileName(FolderSettingsViewModel.LayoutSettingsDbPath));
					string importPrefsDB = await layoutPrefsDB.ReadTextAsync();
					using (var layoutDbInstance = FolderSettingsViewModel.GetDbInstance())
					{
						layoutDbInstance.Import(importPrefsDB);
					}
				}
				catch (Exception ex)
				{
					App.Logger.Warn(ex, "Error importing settings");
					UIHelpers.CloseAllDialogs();
					await DialogDisplayHelper.ShowDialogAsync("SettingsImportErrorTitle".GetLocalizedResource(), "SettingsImportErrorDescription".GetLocalizedResource());
				}
			}
		}

		// WINUI3
		private FileOpenPicker InitializeWithWindow(FileOpenPicker obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);
			return obj;
		}

		public void CopyVersionInfo()
		{
			SafetyExtensions.IgnoreExceptions(() =>
			{
				DataPackage dataPackage = new DataPackage();
				dataPackage.RequestedOperation = DataPackageOperation.Copy;
				dataPackage.SetText(Version + "\nOS Version: " + SystemInformation.Instance.OperatingSystemVersion);
				Clipboard.SetContent(dataPackage);
			});
		}

		public async void SupportUs()
		{
			await Launcher.LaunchUriAsync(new Uri(Constants.GitHub.SupportUsUrl));
		}

		public static Task OpenLogLocation() => Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder).AsTask();

		public string Version
		{
			get
			{
				var version = Package.Current.Id.Version;
				return string.Format($"{"SettingsAboutVersionTitle".GetLocalizedResource()} {version.Major}.{version.Minor}.{version.Build}.{version.Revision}");
			}
		}

		public string AppName => Package.Current.DisplayName;

		private Task ClickAboutFeedbackItem(ItemClickEventArgs e)
		{
			var clickedItem = (StackPanel)e.ClickedItem;
			var uri = clickedItem.Tag switch
			{
				"Contributors" => Constants.GitHub.ContributorsUrl,
				"Documentation" => Constants.GitHub.DocumentationUrl,
				"Feedback" => Constants.GitHub.FeedbackUrl,
				"PrivacyPolicy" => Constants.GitHub.PrivacyPolicyUrl,
				"ReleaseNotes" => Constants.GitHub.ReleaseNotesUrl,
				_ => null,
			};
			if (uri is not null)
			{
				return Launcher.LaunchUriAsync(new Uri(uri)).AsTask();
			}

			return Task.CompletedTask;
		}
	}
}
