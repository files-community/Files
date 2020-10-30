using Files.Enums;
using Files.Helpers;
using Files.View_Models;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class Appearance : Page
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public Appearance()
        {
            InitializeComponent();

            List<string> _themeval = new List<string>();
            _themeval.Add("SystemTheme".GetLocalized());
            _themeval.Add("LightTheme".GetLocalized());
            _themeval.Add("DarkTheme".GetLocalized());
            ThemeChooser.ItemsSource = _themeval;

            ThemeChooser.SelectedIndex = (int)Enum.Parse(typeof(ElementTheme), ThemeHelper.RootTheme.ToString());
            ThemeChooser.Loaded += (s, e) =>
            {
                ThemeChooser.SelectionChanged += (s1, e1) =>
                {
                    var themeComboBox = s1 as ComboBox;

                    switch (themeComboBox.SelectedIndex)
                    {
                        case 0:
                            ThemeHelper.RootTheme = ElementTheme.Default;
                            break;

                        case 1:
                            ThemeHelper.RootTheme = ElementTheme.Light;
                            break;

                        case 2:
                            ThemeHelper.RootTheme = ElementTheme.Dark;
                            break;
                    }
                };
            };

            //Load App Time Style
            List<string> _dateformatval = new List<string>();
            _dateformatval.Add("ApplicationTimeStye".GetLocalized());
            _dateformatval.Add("SystemTimeStye".GetLocalized());
            DateFormatChooser.ItemsSource = _dateformatval;

            TimeStyle _selectedFormat = AppSettings.DisplayedTimeStyle;
            DateFormatChooser.SelectedIndex = (int)Enum.Parse(typeof(TimeStyle), _selectedFormat.ToString());
            DateFormatChooser.Loaded += (s, e) =>
            {
                DateFormatChooser.SelectionChanged += (s1, e1) =>
                {
                    var timeStyleComboBox = s1 as ComboBox;

                    switch (timeStyleComboBox.SelectedIndex)
                    {
                        case 0:
                            AppSettings.DisplayedTimeStyle = TimeStyle.Application;
                            break;

                        case 1:
                            AppSettings.DisplayedTimeStyle = TimeStyle.System;
                            break;
                    }

                    //await TimeFormatReminder.Fade(value: 1.0f, duration: 1500, delay: 0).StartAsync();
                    //await TimeFormatReminder.Fade(value: 0.0f, duration: 1500, delay: 0).StartAsync();
                };
            };
        }
    }
}