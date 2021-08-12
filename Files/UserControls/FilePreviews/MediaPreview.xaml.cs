using Files.ViewModels.Previews;
using System.Diagnostics;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.FilePreviews
{
    public sealed partial class MediaPreview : UserControl
    {
        public MediaPreview(MediaPreviewViewModel model)
        {
            ViewModel = model;
            InitializeComponent();
        }

        public MediaPreviewViewModel ViewModel { get; set; }
    }

    // Exposing MediaPlayer Volume Property
    public sealed class PreviewMediaPlayerElement : MediaPlayerElement
    {
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.MediaPlayer.VolumeChanged += MediaPlayer_VolumeChanged;
            this.MediaPlayer.Volume = App.AppSettings.MediaVolume;
        }

        private void MediaPlayer_VolumeChanged(MediaPlayer sender, object args)
        {
            if (sender.Volume != App.AppSettings.MediaVolume)
            {
                App.AppSettings.MediaVolume = sender.Volume;
            }
        }
    }
}