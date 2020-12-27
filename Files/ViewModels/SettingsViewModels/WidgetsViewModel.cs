using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.ViewModels.SettingsViewModels
{
    public class WidgetsViewModel : ObservableObject
    {
        private bool showLibraryCardsWidget = App.AppSettings.ShowLibraryCardsWidget;
        private bool showDrivesWidget = App.AppSettings.ShowDrivesWidget;
        private bool showRecentFilesWidget = App.AppSettings.ShowRecentFilesWidget;

        public bool ShowLibraryCardsWidget
        {
            get
            {
                return showLibraryCardsWidget;
            }
            set
            {
                if (showLibraryCardsWidget != value)
                {
                    showLibraryCardsWidget = value;
                    App.AppSettings.ShowLibraryCardsWidget = showLibraryCardsWidget;
                    OnPropertyChanged(nameof(ShowLibraryCardsWidget));
                }
            }
        }

        public bool ShowDrivesWidget
        {
            get
            {
                return showDrivesWidget;
            }
            set
            {
                if (showDrivesWidget != value)
                {
                    showDrivesWidget = value;
                    App.AppSettings.ShowDrivesWidget = showDrivesWidget;
                    OnPropertyChanged(nameof(ShowDrivesWidget));
                }
            }
        }

        public bool ShowRecentFilesWidget
        {
            get
            {
                return showRecentFilesWidget;
            }
            set
            {
                if (showRecentFilesWidget != value)
                {
                    showRecentFilesWidget = value;
                    App.AppSettings.ShowRecentFilesWidget = showRecentFilesWidget;
                    OnPropertyChanged(nameof(ShowRecentFilesWidget));
                }
            }
        }
    }
}
