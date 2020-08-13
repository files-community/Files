using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.Xaml;

namespace Files.Helpers
{
    public class AcrylicTheme : INotifyPropertyChanged
    {
        private Color _FallbackColor;
        private Color _TintColor;
        private double _TintOpacity;

        public Color FallbackColor
        {
            get { return _FallbackColor; }
            set
            {
                _FallbackColor = value;
                NotifyPropertyChanged(nameof(FallbackColor));
            }
        }

        public Color TintColor
        {
            get { return _TintColor; }
            set
            {
                _TintColor = value;
                NotifyPropertyChanged(nameof(TintColor));
            }
        }

        public double TintOpacity
        {
            get { return _TintOpacity; }
            set
            {
                _TintOpacity = value;
                NotifyPropertyChanged(nameof(TintOpacity));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AcrylicTheme()
        {
        }

        public void SetDefaultTheme()
        {
            if (Application.Current.RequestedTheme == ApplicationTheme.Light)
            {
                SetLightTheme();
            }
            else if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                SetDarkTheme();
            }
        }

        public void SetLightTheme()
        {
            FallbackColor = Color.FromArgb(255, 250, 249, 248);
            TintColor = Colors.White;
            TintOpacity = 0.9;
        }

        public void SetDarkTheme()
        {
            FallbackColor = Color.FromArgb(255, 50, 49, 48);
            TintColor = Colors.Black;
            TintOpacity = 0.7;
        }
    }
}