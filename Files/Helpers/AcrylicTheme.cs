using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.Xaml;

namespace Files.Helpers
{
    public class AcrylicTheme : INotifyPropertyChanged
    {
        private Color fallbackColor;
        private Color tintColor;
        private double tintOpacity;

        public Color FallbackColor
        {
            get { return fallbackColor; }
            set
            {
                fallbackColor = value;
                NotifyPropertyChanged(nameof(FallbackColor));
            }
        }

        public Color TintColor
        {
            get { return tintColor; }
            set
            {
                tintColor = value;
                NotifyPropertyChanged(nameof(TintColor));
            }
        }

        public double TintOpacity
        {
            get { return tintOpacity; }
            set
            {
                tintOpacity = value;
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