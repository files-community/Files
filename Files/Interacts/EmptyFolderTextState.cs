using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;

namespace Files.Interacts
{
    public class EmptyFolderTextState : INotifyPropertyChanged
    {
        private Visibility _isVisible;

        public Visibility IsVisible
        {
            get
            {
                return _isVisible;
            }

            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    NotifyPropertyChanged("IsVisible");
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