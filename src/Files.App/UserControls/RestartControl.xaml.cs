using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
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

		public static readonly DependencyProperty ShowDialogProperty =
			DependencyProperty.Register(
				nameof(ShowDialog),
				typeof(bool),
				typeof(RestartControl),
				new PropertyMetadata(false, new PropertyChangedCallback(OnShowDialogPropertyChanged)));

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
			// Tells the app to restore tabs when it's next launched
			UserSettingsService.AppSettingsService.RestoreTabsOnStartup = true;
			// Saves the open tabs
			App.SaveSessionTabs();
			// Launches a new instance of Files
			await Launcher.LaunchUriAsync(new Uri("files-uwp:"));
			// Closes the current instance
			Process.GetCurrentProcess().Kill();
		}

		private void NoButton_Click(object sender, RoutedEventArgs e)
		{
			RestartNotification.Dismiss();
		}
	}
}
