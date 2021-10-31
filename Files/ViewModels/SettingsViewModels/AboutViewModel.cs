using Files.Extensions;
using Files.Helpers;
using Files.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Xaml.Controls;

namespace Files.ViewModels.SettingsViewModels
{
    public class AboutViewModel : ObservableObject
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public RelayCommand OpenLogLocationCommand => new RelayCommand(() => SettingsViewModel.OpenLogLocation());
        public RelayCommand CopyVersionInfoCommand => new RelayCommand(() => CopyVersionInfo());

        public ICommand ExportSettingsCommand { get; }

        public ICommand ImportSettingsCommand { get;  }

        public RelayCommand<ItemClickEventArgs> ClickAboutFeedbackItemCommand =>
            new RelayCommand<ItemClickEventArgs>(ClickAboutFeedbackItem);

        public AboutViewModel()
        {
            ExportSettingsCommand = new AsyncRelayCommand(ExportSettings);
            ImportSettingsCommand = new AsyncRelayCommand(ImportSettings);
        }

        private async Task ExportSettings()
        {
            FileSavePicker filePicker = new FileSavePicker();
            filePicker.FileTypeChoices.Add("Json File", Path.GetExtension(Constants.LocalSettings.UserSettingsFileName).CreateList());

            StorageFile file = await filePicker.PickSaveFileAsync();
            if (file != null)
            {
                string export = (string)UserSettingsService.ExportSettings();
                await FileIO.WriteTextAsync(file, export);
            }
        }

        private async Task ImportSettings()
        {
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.FileTypeFilter.Add(Path.GetExtension(Constants.LocalSettings.UserSettingsFileName));

            StorageFile file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                try
                {
                    string import = await FileIO.ReadTextAsync(file);
                    UserSettingsService.ImportSettings(import);
                }
                catch
                {
                    UIHelpers.CloseAllDialogs();
                    await DialogDisplayHelper.ShowDialogAsync("SettingsImportErrorTitle".GetLocalized(), "SettingsImportErrorDescription".GetLocalized());
                }
            }
        }

        public void CopyVersionInfo()
        {
            Common.Extensions.IgnoreExceptions(() =>
            {
                DataPackage dataPackage = new DataPackage();
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                dataPackage.SetText(Version + "\nOS Version: " + SystemInformation.Instance.OperatingSystemVersion);
                Clipboard.SetContent(dataPackage);
            });
        }

        public string Version
        {
            get
            {
                var version = Package.Current.Id.Version;
                return string.Format($"{"SettingsAboutVersionTitle".GetLocalized()} {version.Major}.{version.Minor}.{version.Build}.{version.Revision}");
            }
        }

        public string AppName
        {
            get
            {
                return Package.Current.DisplayName;
            }
        }

        private async void ClickAboutFeedbackItem(ItemClickEventArgs e)
        {
            var clickedItem = (StackPanel)e.ClickedItem;
            switch (clickedItem.Tag)
            {
                case "Feedback":
                    SettingsViewModel.ReportIssueOnGitHub();
                    break;

                case "ReleaseNotes":
                    await Launcher.LaunchUriAsync(new Uri(@"https://github.com/files-community/Files/releases"));
                    break;

                case "Documentation":
                    await Launcher.LaunchUriAsync(new Uri(@"https://files.community/docs"));
                    break;

                case "Contributors":
                    await Launcher.LaunchUriAsync(new Uri(@"https://github.com/files-community/Files/graphs/contributors"));
                    break;

                case "PrivacyPolicy":
                    await Launcher.LaunchUriAsync(new Uri(@"https://github.com/files-community/Files/blob/main/Privacy.md"));
                    break;

                case "SupportUs":
                    await Launcher.LaunchUriAsync(new Uri(@"https://paypal.me/yaichenbaum"));
                    break;

                default:
                    break;
            }
        }
    }
}