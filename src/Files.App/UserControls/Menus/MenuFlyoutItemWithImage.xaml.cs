using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

// Il modello di elemento Controllo utente è documentato all'indirizzo https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls
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
			(d as MenuFlyoutItem).Icon = e.NewValue is not null ? new IconSourceElement() : null;
		}

		public MenuFlyoutItemWithImage()
		{
			this.InitializeComponent();
		}
	}
}