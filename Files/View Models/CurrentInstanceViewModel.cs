using GalaSoft.MvvmLight;

namespace Files.View_Models
{
    public class CurrentInstanceViewModel : ViewModelBase
    {
        private bool _IsPageTypeNotHome = false;

        public bool IsPageTypeNotHome
        {
            get => _IsPageTypeNotHome;
            set
            {
                Set(ref _IsPageTypeNotHome, value);
                RaisePropertyChanged("IsCreateButtonEnabledInPage");
                RaisePropertyChanged("CanCreateFileInPage");
                RaisePropertyChanged("CanOpenTerminalInPage");
            }
        }

        private bool _IsPageTypeMtpDevice = false;

        public bool IsPageTypeMtpDevice
        {
            get => _IsPageTypeMtpDevice;
            set
            {
                Set(ref _IsPageTypeMtpDevice, value);
                RaisePropertyChanged("IsCreateButtonEnabledInPage");
                RaisePropertyChanged("CanCreateFileInPage");
                RaisePropertyChanged("CanOpenTerminalInPage");
            }
        }

        private bool _IsPageTypeRecycleBin = false;

        public bool IsPageTypeRecycleBin
        {
            get => _IsPageTypeRecycleBin;
            set
            {
                Set(ref _IsPageTypeRecycleBin, value);
                RaisePropertyChanged("IsCreateButtonEnabledInPage");
                RaisePropertyChanged("CanCreateFileInPage");
                RaisePropertyChanged("CanOpenTerminalInPage");
            }
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

        private bool _IsPageTypeOnedrive = false;

        public bool IsPageTypeOnedrive
        {
            get => _IsPageTypeOnedrive;
            set => Set(ref _IsPageTypeOnedrive, value);
        }
    }
}