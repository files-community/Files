using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Files.Interacts
{
    public class State : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class PasteState : State
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
    }

    public class AlwaysPresentCommandsState : State
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
    }
}