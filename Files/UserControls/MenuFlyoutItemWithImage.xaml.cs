using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// Il modello di elemento Controllo utente è documentato all'indirizzo https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class MenuFlyoutItemWithImage : MenuFlyoutItem
    {
        public bool ShowIcon
        {
            get { return (bool)GetValue(ShowIconProperty); }
            set { SetValue(ShowIconProperty, value); }
        }

        public static readonly DependencyProperty ShowIconProperty =
            DependencyProperty.Register("ShowIcon", typeof(bool), typeof(MenuFlyoutItemWithImage), new PropertyMetadata(null, new PropertyChangedCallback(OnShowIconChanged)));

        private static void OnShowIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var mfi = d as MenuFlyoutItemWithImage;
            mfi.UpdateVisualStates();
        }

        public BitmapImage BitmapIcon
        {
            get { return (BitmapImage)GetValue(BitmapIconProperty); }
            set { SetValue(BitmapIconProperty, value); }
        }

        public static readonly DependencyProperty BitmapIconProperty =
            DependencyProperty.Register("BitmapIcon", typeof(BitmapImage), typeof(MenuFlyoutItemWithImage), new PropertyMetadata(null));

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.UpdateVisualStates();
        }

        public MenuFlyoutItemWithImage()
        {
            this.InitializeComponent();
        }

        private void UpdateVisualStates()
        {
            VisualStateManager.GoToState(this, ShowIcon ? "IconVisible" : "IconCollapsed", false);
        }
    }
}