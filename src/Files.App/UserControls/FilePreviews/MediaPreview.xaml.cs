using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.ViewModels.Previews;
using Files.Backend.Services.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using Windows.Media.Playback;

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class MediaPreview : UserControl
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public MediaPreviewViewModel ViewModel { get; set; }

		public MediaPreview(MediaPreviewViewModel model)
		{
			ViewModel = model;

			InitializeComponent();

			PlayerContext.Loaded += PlayerContext_Loaded;
		}

		private void PlayerContext_Loaded(object sender, RoutedEventArgs e)
		{
			PlayerContext.MediaPlayer.Volume = UserSettingsService.PaneSettingsService.MediaVolume;
			PlayerContext.MediaPlayer.VolumeChanged += MediaPlayer_VolumeChanged;
			ViewModel.TogglePlaybackRequested += TogglePlaybackRequestInvoked;
		}

		private void MediaPlayer_VolumeChanged(MediaPlayer sender, object args)
		{
			if (sender.Volume != UserSettingsService.PaneSettingsService.MediaVolume)
			{
				UserSettingsService.PaneSettingsService.MediaVolume = sender.Volume;
			}
		}

		private void TogglePlaybackRequestInvoked(object sender, EventArgs e)
		{
			if (PlayerContext.MediaPlayer.PlaybackSession.PlaybackState is not MediaPlaybackState.Playing)
			{
				PlayerContext.MediaPlayer.Play();
			}
			else
			{
				PlayerContext.MediaPlayer.Pause();
			}
		}

		private void TogglePlaybackAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			TogglePlaybackRequestInvoked(sender, null);
		}
	}
}
