using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.View_Models
{
    public class CurrentInstanceViewModel : ObservableObject
    {
        private bool _IsPageTypeNotHome = false;

        public bool IsPageTypeNotHome
        {
            get => _IsPageTypeNotHome;
            set => SetProperty(ref _IsPageTypeNotHome, value);
        }

        private bool _IsPageTypeMtpDevice = false;

        public bool IsPageTypeMtpDevice
        {
            get => _IsPageTypeMtpDevice;
            set => SetProperty(ref _IsPageTypeMtpDevice, value);
        }

        private bool _IsPageTypeRecycleBin = false;

        public bool IsPageTypeRecycleBin
        {
            get => _IsPageTypeRecycleBin;
            set => SetProperty(ref _IsPageTypeRecycleBin, value);
        }

        public bool IsCreateButtonEnabledInPage
        {
            get => !_IsPageTypeRecycleBin && IsPageTypeNotHome;
        }

        public bool CanCreateFileInPage
        {
            get => !_IsPageTypeMtpDevice && !_IsPageTypeRecycleBin && IsPageTypeNotHome;
        }

        public bool CanOpenTerminalInPage
        {
            get => !_IsPageTypeMtpDevice && !_IsPageTypeRecycleBin && IsPageTypeNotHome;
        }

        private bool _IsPageTypeCloudDrive = false;

        public bool IsPageTypeCloudDrive
        {
            get => _IsPageTypeCloudDrive;
            set => SetProperty(ref _IsPageTypeCloudDrive, value);
        }
    }
}