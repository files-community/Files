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

        private bool showMultiselectOption = App.AppSettings.ShowMultiselectOption;
        public bool ShowMultiselectOption
        {
            get
            {
                return showMultiselectOption;
            }
            set
            {
                if (SetProperty(ref showMultiselectOption, value))
                {
                    App.AppSettings.ShowMultiselectOption = value;
                }
            }
        }
    }
}