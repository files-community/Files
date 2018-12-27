using System.ComponentModel;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace Files.SettingsPages
{
    
    public sealed partial class Personalization : Page
    {


        public Personalization()
        {
            this.InitializeComponent();
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values["theme"] != null)
            {
                if (localSettings.Values["theme"].ToString() == "Light")
                {
                    ThemeChooser.SelectedIndex = 1;
                }
                else if (localSettings.Values["theme"].ToString() == "Dark")
                {
                    ThemeChooser.SelectedIndex = 2;
                }
                else
                {
                    ThemeChooser.SelectedIndex = 0;
                }
            }
        }

        private static ThemeValueClass tv = new ThemeValueClass();
        public static ThemeValueClass TV { get { return tv; } }

        private void ThemeChooser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (((ComboBoxItem) e.AddedItems[0]).Content.Equals("Light"))
            {
                localSettings.Values["theme"] = "Light";
                Debug.WriteLine("Light Mode Enabled");
            }
            else if (((ComboBoxItem)e.AddedItems[0]).Content.Equals("Dark"))
            {
                localSettings.Values["theme"] = "Dark";
                Debug.WriteLine("Dark Mode Enabled");
            }
            else
            {
                localSettings.Values["theme"] = "Default";
                Debug.WriteLine("Default Mode Enabled");
            }
            RestartReminder.Visibility = Visibility.Visible;
        }
    }

    public class ThemeValueClass : INotifyPropertyChanged
    {
        public ApplicationTheme _ThemeValue;
        public ApplicationTheme ThemeValue
        {
            get
            {
                return _ThemeValue;
            }
            set
            {
                if(value != _ThemeValue)
                {
                    _ThemeValue = value;
                    NotifyPropertyChanged("ThemeValue");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}
