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

        private bool loadShellContextMenuItems = App.AppSettings.LoadShellContextMenuItems;

        public bool LoadShellContextMenuItems
        {
            get
            {
                return loadShellContextMenuItems;
            }
            set
            {
                if (SetProperty(ref loadShellContextMenuItems, value))
                {
                    App.AppSettings.LoadShellContextMenuItems = value;
                }
            }
        }
    }
}