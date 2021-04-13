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

        private bool useFileListCache = App.AppSettings.UseFileListCache;

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

        private bool usePreemptiveCache = App.AppSettings.UsePreemptiveCache;

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

        private int preemptiveCacheParallelLimit = App.AppSettings.PreemptiveCacheParallelLimit;

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