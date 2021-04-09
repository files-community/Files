using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.ViewModels.SettingsViewModels
{
    public class ExperimentalViewModel : ObservableObject
    {
        private int preemptiveCacheParallelLimit = App.AppSettings.PreemptiveCacheParallelLimit;
        private bool showFileOwner = App.AppSettings.ShowFileOwner;

        private bool showMultiselectOption = App.AppSettings.ShowMultiselectOption;

        private bool useFileListCache = App.AppSettings.UseFileListCache;

        private bool usePreemptiveCache = App.AppSettings.UsePreemptiveCache;

        public int PreemptiveCacheParallelLimit
        {
            get
            {
                return preemptiveCacheParallelLimit;
            }
            set
            {
                if (SetProperty(ref preemptiveCacheParallelLimit, value))
                {
                    App.AppSettings.PreemptiveCacheParallelLimit = value;
                }
            }
        }

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

        public bool UseFileListCache
        {
            get
            {
                return useFileListCache;
            }
            set
            {
                if (SetProperty(ref useFileListCache, value))
                {
                    App.AppSettings.UseFileListCache = value;
                }
            }
        }

        public bool UsePreemptiveCache
        {
            get
            {
                return usePreemptiveCache;
            }
            set
            {
                if (SetProperty(ref usePreemptiveCache, value))
                {
                    App.AppSettings.UsePreemptiveCache = value;
                }
            }
        }
    }
}