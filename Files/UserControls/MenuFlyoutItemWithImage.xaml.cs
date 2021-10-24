using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// Il modello di elemento Controllo utente è documentato all'indirizzo https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class MenuFlyoutItemWithImage : MenuFlyoutItem
    {
        public BitmapImage BitmapIcon
        {
            get { return (BitmapImage)GetValue(BitmapIconProperty); }
            set { SetValue(BitmapIconProperty, value); }
        }

        public static readonly DependencyProperty BitmapIconProperty =
            DependencyProperty.Register("BitmapIcon", typeof(BitmapImage), typeof(MenuFlyoutItemWithImage), new PropertyMetadata(null, OnBitmapIconChanged));

        private static void OnBitmapIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MenuFlyoutItem).Icon = e.NewValue != null ? new IconSourceElement() : null;
        }

        public MenuFlyoutItemWithImage()
        {
            this.InitializeComponent();
        }
    }
}