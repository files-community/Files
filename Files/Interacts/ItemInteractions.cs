using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;

namespace Files.Interacts
{
    public class PasteState : INotifyPropertyChanged
    {
        private bool _isEnabled;

        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }

            set
            {
                if (value != _isEnabled)
                {
                    _isEnabled = value;
                    NotifyPropertyChanged("IsEnabled");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AlwaysPresentCommandsState : INotifyPropertyChanged
    {
        private bool isCopyPathCommandEnabled;

        public bool IsCopyPathCommandEnabled
        {
            get
            {
                return isCopyPathCommandEnabled;
            }
            set
            {
                if (value != isCopyPathCommandEnabled)
                {
                    isCopyPathCommandEnabled = value;
                    NotifyPropertyChanged("IsCopyPathCommandEnabled");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}