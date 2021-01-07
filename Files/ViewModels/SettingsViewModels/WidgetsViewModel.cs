using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.ViewModels.SettingsViewModels
{
    public class WidgetsViewModel : ObservableObject
    {
        private bool showLibraryCardsWidget = App.AppSettings.ShowLibraryCardsWidget;
        private bool showDrivesWidget = App.AppSettings.ShowDrivesWidget;
        private bool showBundlesWidget = App.AppSettings.ShowBundlesWidget;
        private bool showRecentFilesWidget = App.AppSettings.ShowRecentFilesWidget;

        public bool ShowLibraryCardsWidget
        {
            get
            {
                return showLibraryCardsWidget;
            }
            set
            {
                if (SetProperty(ref showLibraryCardsWidget, value))
                {
                    App.AppSettings.ShowLibraryCardsWidget = value;
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
                if (SetProperty(ref showDrivesWidget, value))
                {
                    App.AppSettings.ShowDrivesWidget = value;
                }
            }
        }

        public bool ShowBundlesWidget
        {
            get
            {
                return showBundlesWidget;
            }
            set
            {
                if (SetProperty(ref showBundlesWidget, value))
                {
                    App.AppSettings.ShowBundlesWidget = value;
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
                if (SetProperty(ref showRecentFilesWidget, value))
                {
                    App.AppSettings.ShowRecentFilesWidget = value;
                }
            }
        }
    }
}