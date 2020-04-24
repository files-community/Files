using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.Xaml;

namespace Files.Helpers
{
    public class AppTheme : INotifyPropertyChanged
    {
        private double? _TintLuminosityOpacity;
        private Color _FallbackColor;
        private Color _TintColor;
        private double _TintOpacity;

        public double? TintLuminosityOpacity
        {
            get { return _TintLuminosityOpacity; }
            set
            {
                _TintLuminosityOpacity = value;
                NotifyPropertyChanged("TintLuminosityOpacity");
            }
        }

        public Color FallbackColor
        {
            get { return _FallbackColor; }
            set
            {
                _FallbackColor = value;
                NotifyPropertyChanged("FallbackColor");
            }
        }

        public Color TintColor
        {
            get { return _TintColor; }
            set
            {
                _TintColor = value;
                NotifyPropertyChanged("TintColor");
            }
        }

        public double TintOpacity
        {
            get { return _TintOpacity; }
            set
            {
                _TintOpacity = value;
                NotifyPropertyChanged("TintOpacity");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AppTheme()
        {
        }

        public void SetDefaultTheme()
        {
            TintLuminosityOpacity = 0.9;
            FallbackColor = (Color)Application.Current.Resources["SystemChromeMediumLowColor"];
            TintColor = (Color)Application.Current.Resources["SystemAltHighColor"];
            TintOpacity = 0.9;
        }

        public void SetLightTheme()
        {
            TintLuminosityOpacity = 0.9;
            FallbackColor = Color.FromArgb(255, 242, 242, 242);
            TintColor = Colors.White;
            TintOpacity = 0.9;
        }

        public void SetDarkTheme()
        {
            TintLuminosityOpacity = 0.9;
            FallbackColor = Color.FromArgb(255, 43, 43, 43);
            TintColor = Colors.Black;
            TintOpacity = 0.7;
        }
    }
}