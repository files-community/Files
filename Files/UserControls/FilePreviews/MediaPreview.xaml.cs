using Files.Services;
using Files.ViewModels.Previews;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.FilePreviews
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
            PlayerContext.MediaPlayer.Volume = UserSettingsService.PreviewPaneSettingsService.PreviewPaneMediaVolume;
            PlayerContext.MediaPlayer.VolumeChanged += MediaPlayer_VolumeChanged;
        }

        private void MediaPlayer_VolumeChanged(MediaPlayer sender, object args)
        {
            if (sender.Volume != UserSettingsService.PreviewPaneSettingsService.PreviewPaneMediaVolume)
            {
                UserSettingsService.PreviewPaneSettingsService.PreviewPaneMediaVolume = sender.Volume;
            }
        }
    }
}