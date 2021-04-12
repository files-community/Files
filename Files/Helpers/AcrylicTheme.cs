using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.Xaml;

namespace Files.Helpers
{
    public class AcrylicTheme : INotifyPropertyChanged
    {
        private Color fallbackColor;
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
            FallbackColor = (Color)App.Current.Resources["SolidBackgroundFillColorBase"];
            TintOpacity = 0.15;
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
            FallbackColor = (Color)App.Current.Resources["SolidBackgroundFillColorBase"];
            TintOpacity = 0.0;
        }

        public void SetDarkTheme()
        {
            FallbackColor = (Color)App.Current.Resources["SolidBackgroundFillColorBase"];
            TintOpacity = 0.15;
        }
    }
}