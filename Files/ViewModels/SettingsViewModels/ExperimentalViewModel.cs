using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.ViewModels.SettingsViewModels
{
    public class ExperimentalViewModel : ObservableObject
    {
        private bool showFileOwner = App.AppSettings.ShowFileOwner;

        public bool ShowFileOwner
        {
            get
            {
                return showFileOwner;
            }
            set
            {
                if (SetProperty(ref showFileOwner, value))
                {
                    App.AppSettings.ShowFileOwner = value;
                }
            }
        }

        private bool useNewDetailsView = App.AppSettings.UseNewDetailsView;

        public bool UseNewDetailsView
        {
            get
            {
                return useNewDetailsView;
            }
            set
            {
                if (SetProperty(ref useNewDetailsView, value))
                {
                    App.AppSettings.UseNewDetailsView = value;
                }
            }
        }
    }
}