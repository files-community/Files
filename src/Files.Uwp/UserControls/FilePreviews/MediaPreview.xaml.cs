using Files.Backend.Services.Settings;
using Files.Uwp.ViewModels.Previews;
using CommunityToolkit.Mvvm.DependencyInjection;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using System;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.Uwp.UserControls.FilePreviews
{
    public sealed partial class MediaPreview : UserControl
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public MediaPreview(MediaPreviewViewModel model)
        {
            ViewModel = model;
            InitializeComponent();
            PlayerContext.Loaded += PlayerContext_Loaded;
        }

        public MediaPreviewViewModel ViewModel { get; set; }

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