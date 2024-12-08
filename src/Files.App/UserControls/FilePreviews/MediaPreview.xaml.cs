using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Media.Playback;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class MediaPreview : UserControl
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public MediaPreview(MediaPreviewViewModel model)
		{
			ViewModel = model;
			InitializeComponent();
			PlayerContext.Loaded += PlayerContext_Loaded;
			Unloaded += MediaPreview_Unloaded;
		}

		public MediaPreviewViewModel ViewModel { get; set; }

		private void PlayerContext_Loaded(object sender, RoutedEventArgs e)
		{
			PlayerContext.MediaPlayer.Volume = UserSettingsService.InfoPaneSettingsService.MediaVolume;
			PlayerContext.MediaPlayer.VolumeChanged += MediaPlayer_VolumeChanged;
			ViewModel.TogglePlaybackRequested += TogglePlaybackRequestInvoked;
		}

		private void MediaPreview_Unloaded(object sender, RoutedEventArgs e)
		{
			// The MediaPlayerElement isn't properly disposed by Windows so we set the source to null
			// to avoid issues the next time the control is used.
			PlayerContext.Source = null;

			PlayerContext.Loaded -= PlayerContext_Loaded;
			Unloaded -= MediaPreview_Unloaded;		
	}

		private void MediaPlayer_VolumeChanged(MediaPlayer sender, object args)
		{
			if (sender.Volume != UserSettingsService.InfoPaneSettingsService.MediaVolume)
			{
				UserSettingsService.InfoPaneSettingsService.MediaVolume = sender.Volume;
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