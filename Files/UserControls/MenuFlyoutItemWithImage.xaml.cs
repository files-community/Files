using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

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
            DependencyProperty.Register("BitmapIcon", typeof(BitmapImage), typeof(MenuFlyoutItemWithImage), new PropertyMetadata(null));

        public MenuFlyoutItemWithImage()
        {
            this.InitializeComponent();
        }
    }
}
