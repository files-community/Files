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
                RaisePropertyChanged("CanCreateFileInPage");
            }
        }

        private bool _IsPageTypeMtpDevice = false;

        public bool IsPageTypeMtpDevice
        {
            get => _IsPageTypeMtpDevice;
            set
            {
                Set(ref _IsPageTypeMtpDevice, value);
                RaisePropertyChanged("CanCreateFileInPage");
            }
        }

        private bool _IsPageTypeRecycleBin = false;

        public bool IsPageTypeRecycleBin
        {
            get => _IsPageTypeRecycleBin;
            set
            {
                Set(ref _IsPageTypeRecycleBin, value);
                RaisePropertyChanged("CanCreateFileInPage");
            }
        }

        public bool CanCreateFileInPage
        {
            get => !_IsPageTypeMtpDevice && !_IsPageTypeRecycleBin && IsPageTypeNotHome;
        }
    }
}