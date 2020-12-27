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
                if (showFileOwner != value)
                {
                    showFileOwner = value;
                    App.AppSettings.ShowFileOwner = showFileOwner;
                    OnPropertyChanged(nameof(ShowFileOwner));
                }
            }
        }
    }
}
