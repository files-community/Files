// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace Files.App.UserControls
{
	public sealed partial class RestartControl : UserControl
	{
		public IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public RestartControl()
		{
			InitializeComponent();
		}

		public static readonly DependencyProperty ShowDialogProperty = DependencyProperty.Register(
		  "ShowDialog", typeof(bool), typeof(RestartControl), new PropertyMetadata(false, new PropertyChangedCallback(OnShowDialogPropertyChanged)));
		public bool ShowDialog
		{
			get => (bool)GetValue(dp: ShowDialogProperty);
			set => SetValue(ShowDialogProperty, value);
		}

		private static void OnShowDialogPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			var dialog = (RestartControl)sender;
			if ((bool)e.NewValue)
			{
				dialog.RestartNotification.Show(10000);
			}
			else
			{
				dialog.RestartNotification.Dismiss();
			}
		}

		public void Show()
		{
			RestartNotification.Show(10000);
		}

		public void Dismiss()
		{
			RestartNotification.Dismiss();
		}

		private async void YesButton_Click(object sender, RoutedEventArgs e)
		{
			UserSettingsService.AppSettingsService.RestoreTabsOnStartup = true; // Tells the app to restore tabs when it's next launched
			App.SaveSessionTabs(); // Saves the open tabs
			await Launcher.LaunchUriAsync(new Uri("files-uwp:")); // Launches a new instance of Files
			Process.GetCurrentProcess().Kill(); // Closes the current instance
		}

		private void NoButton_Click(object sender, RoutedEventArgs e)
		{
			RestartNotification.Dismiss();
		}
	}
}