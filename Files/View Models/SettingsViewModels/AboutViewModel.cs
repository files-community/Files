using Files.DataModels.SettingsModels;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.System;
using Windows.UI.Xaml.Controls;

namespace Files.ViewModels.SettingsViewModels
{
    public class AboutViewModel : ObservableObject
    {
        public AboutViewModel()
        {
            AboutFeedbackItems = new List<AboutFeedbackItem>()
            {
                new AboutFeedbackItem()
                {
                    Title = "SettingsAboutSubmitFeedback/Text".GetLocalized(),
                    Subtitle = "SettingsAboutSubmitFeedbackDescription/Text".GetLocalized(),
                    AutomationPropertiesName = "SettingsAboutSubmitFeedbackListViewItem/AutomationProperties/Name".GetLocalized(),
                    Glyph = "\ueb05",
                    Command = new RelayCommand(() =>
                    {
                        SettingsViewModel.ReportIssueOnGitHub();
                    })
                },
                new AboutFeedbackItem()
                {
                    Title = "SettingsAboutReleaseNotes/Text".GetLocalized(),
                    Subtitle = "SettingsAboutReleaseNotesDescription/Text".GetLocalized(),
                    AutomationPropertiesName = "SettingsAboutReleaseNotesListViewItem/AutomationProperties/Name".GetLocalized(),
                    Glyph = "\uEB3A",
                    Command = new RelayCommand(async () =>
                    {
                        await Launcher.LaunchUriAsync(new Uri(@"https://github.com/files-community/files-uwp/releases"));
                    })
                },
                new AboutFeedbackItem()
                {
                    Title = "SettingsAboutContributors/Text".GetLocalized(),
                    Subtitle = "SettingsAboutContributorsDescription/Text".GetLocalized(),
                    AutomationPropertiesName = "SettingsAboutContributorsListViewItem/AutomationProperties/Name".GetLocalized(),
                    Glyph = "\uEAF7",
                    Command = new RelayCommand(async () =>
                    {
                        await Launcher.LaunchUriAsync(new Uri(@"https://github.com/files-community/files-uwp/graphs/contributors"));
                    })
                },
                new AboutFeedbackItem()
                {
                    Title = "SettingsAboutSupportUs/Text".GetLocalized(),
                    Subtitle = "SettingsAboutSupportUsDescription/Text".GetLocalized(),
                    AutomationPropertiesName = "SettingsAboutSupportUsListViewItem/AutomationProperties/Name".GetLocalized(),
                    Glyph = "\uEB3B",
                    Command = new RelayCommand(async () =>
                    {
                        await Launcher.LaunchUriAsync(new Uri(@"https://paypal.me/yaichenbaum"));
                    })
                }
            };
        }

        public RelayCommand OpenLogLocationCommand => new RelayCommand(() => SettingsViewModel.OpenLogLocation());
        public RelayCommand<ItemClickEventArgs> ClickAboutFeedbackItemCommand =>
            new RelayCommand<ItemClickEventArgs>(ClickAboutFeedbackItem);

        public List<AboutFeedbackItem> AboutFeedbackItems { get; set; }

        public string Version
        {
            get
            {
                var version = Package.Current.Id.Version;
                return string.Format($"{"SettingsAboutVersionTitle".GetLocalized()} {version.Major}.{version.Minor}.{version.Build}.{version.Revision}");
            }
        }

        private void ClickAboutFeedbackItem(ItemClickEventArgs e)
        {
            var clickedItem = (AboutFeedbackItem)e.ClickedItem;
            if (clickedItem.Command.CanExecute(null))
            {
                clickedItem.Command.Execute(null);
            }
        }
    }
}
