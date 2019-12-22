using Files.Enums;
using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace Files.SettingsPages
{
    public sealed partial class Personalization : Page
    {
        public Personalization()
        {
            InitializeComponent();

            //Load Theme Style
            var _themeval = Enum.GetValues(typeof(ThemeStyle)).Cast<ThemeStyle>();
            ThemeChooser.ItemsSource = _themeval.ToList();

            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values["theme"] != null)
            {
                ThemeStyle _selectedTheme = localSettings.Values["theme"].ToString().Equals("Default") ? ThemeStyle.System : Enum.Parse<ThemeStyle>(localSettings.Values["theme"].ToString());
                ThemeChooser.SelectedIndex = _themeval.ToList().IndexOf(_selectedTheme);
                ThemeChooser.Loaded += (s, e) =>
                {
                    ThemeChooser.SelectionChanged += async (s1, e1) =>
                    {
                        localSettings.Values["theme"] = e1.AddedItems[0].Equals("System") ? "Default" : e1.AddedItems[0].ToString();
                        await RestartReminder.Fade(value: 1.0f, duration: 1500, delay: 0).StartAsync();
                        await RestartReminder.Fade(value: 0.0f, duration: 1500, delay: 0).StartAsync();
                    };
                };
            }

            //Load App Time Style
            var _dateformatval = Enum.GetValues(typeof(TimeStyle)).Cast<TimeStyle>();
            DateFormatChooser.ItemsSource = _dateformatval.ToList();

            if (localSettings.Values["datetimeformat"] != null)
            {
                TimeStyle _selectedFormat = Enum.Parse<TimeStyle>(localSettings.Values["datetimeformat"].ToString());
                DateFormatChooser.SelectedIndex = _dateformatval.ToList().IndexOf(_selectedFormat);
                DateFormatChooser.Loaded += (s, e) =>
                {
                    DateFormatChooser.SelectionChanged += async (s1, e1) =>
                    {
                        localSettings.Values["datetimeformat"] = e1.AddedItems[0].ToString();
                        await TimeFormatReminder.Fade(value: 1.0f, duration: 1500, delay: 0).StartAsync();
                        await TimeFormatReminder.Fade(value: 0.0f, duration: 1500, delay: 0).StartAsync();
                    };
                };
            }

            // Acrylic Sidebar
            if (localSettings.Values["acrylicSidebar"] != null)
            {
	            var isAcrylicSidebarEnabled = bool.Parse(localSettings.Values["acrylicSidebar"].ToString());
	            AcrylicSidebarSwitch.IsOn = isAcrylicSidebarEnabled;
            }

            AcrylicSidebarSwitch.Loaded += (sender, args) =>
            {
	            AcrylicSidebarSwitch.Toggled += (o, eventArgs) =>
	            {
		            localSettings.Values["acrylicSidebar"] = ((ToggleSwitch)o).IsOn;
	            };
            };
        }

        private static ThemeValueClass tv = new ThemeValueClass();
        public static ThemeValueClass TV { get { return tv; } }

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
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
