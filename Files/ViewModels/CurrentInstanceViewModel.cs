using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.ViewModels
{
    public class CurrentInstanceViewModel : ObservableObject
    {
        /*
         * TODO:
         * In the future, we should consolidate these public variables into
         * a single enum property providing simplified customization of the
         * values being manipulated inside the setter blocks.
         */

        public FolderSettingsViewModel FolderSettings { get; }

        public CurrentInstanceViewModel(IShellPage associatedInstance)
        {
            FolderSettings = new FolderSettingsViewModel(associatedInstance);
        }

        private bool isPageTypeSearchResults = false;

        public bool IsPageTypeSearchResults
        {
            get => isPageTypeSearchResults;
            set
            {
                SetProperty(ref isPageTypeSearchResults, value);
                OnPropertyChanged(nameof(IsCreateButtonEnabledInPage));
                OnPropertyChanged(nameof(CanCreateFileInPage));
                OnPropertyChanged(nameof(CanPasteInPage));
                OnPropertyChanged(nameof(CanOpenTerminalInPage));
                OnPropertyChanged(nameof(CanCopyPathInPage));
            }
        }

        private bool isPageTypeNotHome = false;

        public bool IsPageTypeNotHome
        {
            get => isPageTypeNotHome;
            set
            {
                SetProperty(ref isPageTypeNotHome, value);
                OnPropertyChanged(nameof(IsCreateButtonEnabledInPage));
                OnPropertyChanged(nameof(CanCreateFileInPage));
                OnPropertyChanged(nameof(CanPasteInPage));
                OnPropertyChanged(nameof(CanOpenTerminalInPage));
                OnPropertyChanged(nameof(CanCopyPathInPage));
            }
        }

        private bool isPageTypeMtpDevice = false;

        public bool IsPageTypeMtpDevice
        {
            get => isPageTypeMtpDevice;
            set
            {
                SetProperty(ref isPageTypeMtpDevice, value);
                OnPropertyChanged(nameof(IsCreateButtonEnabledInPage));
                OnPropertyChanged(nameof(CanCreateFileInPage));
                OnPropertyChanged(nameof(CanPasteInPage));
                OnPropertyChanged(nameof(CanOpenTerminalInPage));
                OnPropertyChanged(nameof(CanCopyPathInPage));
            }
        }

        private bool isPageTypeRecycleBin = false;

        public bool IsPageTypeRecycleBin
        {
            get => isPageTypeRecycleBin;
            set
            {
                SetProperty(ref isPageTypeRecycleBin, value);
                OnPropertyChanged(nameof(IsCreateButtonEnabledInPage));
                OnPropertyChanged(nameof(CanCreateFileInPage));
                OnPropertyChanged(nameof(CanPasteInPage));
                OnPropertyChanged(nameof(CanOpenTerminalInPage));
                OnPropertyChanged(nameof(CanCopyPathInPage));
            }
        }

        public bool IsCreateButtonEnabledInPage
        {
            get => !isPageTypeRecycleBin && isPageTypeNotHome && !isPageTypeSearchResults;
        }

        public bool CanCopyPathInPage
        {
            get => !isPageTypeMtpDevice && !isPageTypeRecycleBin && isPageTypeNotHome && !isPageTypeSearchResults;
        }

        public bool CanCreateFileInPage
        {
            get => !isPageTypeMtpDevice && !isPageTypeRecycleBin && isPageTypeNotHome && !isPageTypeSearchResults;
        }

        public bool CanOpenTerminalInPage
        {
            get => !isPageTypeMtpDevice && !isPageTypeRecycleBin && isPageTypeNotHome && !isPageTypeSearchResults;
        }

        public bool CanPasteInPage
        {
            get => !isPageTypeRecycleBin && isPageTypeNotHome && !isPageTypeSearchResults;
        }

        private bool isPageTypeCloudDrive = false;

        public bool IsPageTypeCloudDrive
        {
            get => isPageTypeCloudDrive;
            set => SetProperty(ref isPageTypeCloudDrive, value);
        }
    }
}