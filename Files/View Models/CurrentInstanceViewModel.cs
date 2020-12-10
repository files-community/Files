using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.View_Models
{
    public class CurrentInstanceViewModel : ObservableObject
    {
        /* 
         * TODO:
         * In the future, we should consolidate these public variables into
         * a single enum property providing simplified customization of the
         * values being manipulated inside the setter blocks. 
         */

        private bool _IsPageTypeSearchResults = false;

        public bool IsPageTypeSearchResults
        {
            get => _IsPageTypeSearchResults;
            set
            {
                SetProperty(ref _IsPageTypeSearchResults, value);
                OnPropertyChanged(nameof(IsCreateButtonEnabledInPage));
                OnPropertyChanged(nameof(CanCreateFileInPage));
                OnPropertyChanged(nameof(CanOpenTerminalInPage));
            }
        }

        private bool _IsPageTypeNotHome = false;

        public bool IsPageTypeNotHome
        {
            get => _IsPageTypeNotHome;
            set
            {
                SetProperty(ref _IsPageTypeNotHome, value);
                OnPropertyChanged(nameof(IsCreateButtonEnabledInPage));
                OnPropertyChanged(nameof(CanCreateFileInPage));
                OnPropertyChanged(nameof(CanOpenTerminalInPage));
            }
        }

        private bool _IsPageTypeMtpDevice = false;

        public bool IsPageTypeMtpDevice
        {
            get => _IsPageTypeMtpDevice;
            set
            {
                SetProperty(ref _IsPageTypeMtpDevice, value);
                OnPropertyChanged(nameof(IsCreateButtonEnabledInPage));
                OnPropertyChanged(nameof(CanCreateFileInPage));
                OnPropertyChanged(nameof(CanOpenTerminalInPage));
            }
        }

        private bool _IsPageTypeRecycleBin = false;

        public bool IsPageTypeRecycleBin
        {
            get => _IsPageTypeRecycleBin;
            set
            {
                SetProperty(ref _IsPageTypeRecycleBin, value);
                OnPropertyChanged(nameof(IsCreateButtonEnabledInPage));
                OnPropertyChanged(nameof(CanCreateFileInPage));
                OnPropertyChanged(nameof(CanOpenTerminalInPage));
            }
        }

        public bool IsCreateButtonEnabledInPage
        {
            get => !_IsPageTypeRecycleBin && IsPageTypeNotHome && !_IsPageTypeSearchResults;
        }

        public bool CanCreateFileInPage
        {
            get => !_IsPageTypeMtpDevice && !_IsPageTypeRecycleBin && IsPageTypeNotHome && !_IsPageTypeSearchResults;
        }

        public bool CanOpenTerminalInPage
        {
            get => !_IsPageTypeMtpDevice && !_IsPageTypeRecycleBin && IsPageTypeNotHome && !_IsPageTypeSearchResults;
        }

        private bool _IsPageTypeCloudDrive = false;

        public bool IsPageTypeCloudDrive
        {
            get => _IsPageTypeCloudDrive;
            set => SetProperty(ref _IsPageTypeCloudDrive, value);
        }
    }
}