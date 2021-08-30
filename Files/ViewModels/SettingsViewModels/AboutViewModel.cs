using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml.Controls;

namespace Files.ViewModels.SettingsViewModels
{
    public class AboutViewModel : ObservableObject
    {
        public RelayCommand OpenLogLocationCommand => new RelayCommand(() => SettingsViewModel.OpenLogLocation());
        public RelayCommand CopyVersionInfoCommand => new RelayCommand(() => CopyVersionInfo());

        public RelayCommand<ItemClickEventArgs> ClickAboutFeedbackItemCommand =>
            new RelayCommand<ItemClickEventArgs>(ClickAboutFeedbackItem);

        public void CopyVersionInfo()
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetText(Version + "\nOS Version: " + SystemInformation.Instance.OperatingSystemVersion);
            Clipboard.SetContent(dataPackage);
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