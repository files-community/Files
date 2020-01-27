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
			ThemeStyle _selectedTheme = App.AppSettings.ThemeValue;

			ThemeChooser.SelectedIndex = _themeval.ToList().IndexOf(_selectedTheme);
			ThemeChooser.Loaded += (s, e) =>
			{
				ThemeChooser.SelectionChanged += async (s1, e1) =>
				{
					switch (e1.AddedItems[0].ToString())
					{
						case "System":
							App.AppSettings.ThemeValue = ThemeStyle.System;
							break;
						case "Light":
							App.AppSettings.ThemeValue = ThemeStyle.Light;
							break;
						case "Dark":
							App.AppSettings.ThemeValue = ThemeStyle.Dark;
							break;
					}
					
					//await RestartReminder.Fade(value: 1.0f, duration: 1500, delay: 0).StartAsync();
					//await RestartReminder.Fade(value: 0.0f, duration: 1500, delay: 0).StartAsync();
				};
			};

			//Load App Time Style
			var _dateformatval = Enum.GetValues(typeof(TimeStyle)).Cast<TimeStyle>();
			DateFormatChooser.ItemsSource = _dateformatval.ToList();

			TimeStyle _selectedFormat = App.AppSettings.DisplayedTimeStyle;
			DateFormatChooser.SelectedIndex = _dateformatval.ToList().IndexOf(_selectedFormat);
			DateFormatChooser.Loaded += (s, e) =>
			{
				DateFormatChooser.SelectionChanged += async (s1, e1) =>
				{
					switch (e1.AddedItems[0].ToString())
					{
						case "Application":
							App.AppSettings.DisplayedTimeStyle = TimeStyle.Application;
							break;
						case "System":
							App.AppSettings.DisplayedTimeStyle = TimeStyle.System;
							break;
					}

					//await TimeFormatReminder.Fade(value: 1.0f, duration: 1500, delay: 0).StartAsync();
					//await TimeFormatReminder.Fade(value: 0.0f, duration: 1500, delay: 0).StartAsync();
				};
			};

			
			AcrylicSidebarSwitch.IsOn = App.AppSettings.SidebarThemeMode.Equals(SidebarOpacity.Opaque) ? false : true;

			AcrylicSidebarSwitch.Loaded += (sender, args) =>
			{
				AcrylicSidebarSwitch.Toggled += (o, eventArgs) =>
				{
					if (((ToggleSwitch)o).IsOn)
					{
						App.AppSettings.SidebarThemeMode = SidebarOpacity.AcrylicEnabled;
					}
					else
					{
						App.AppSettings.SidebarThemeMode = SidebarOpacity.Opaque;
					}
				};
			};
		}

	}

   
}
