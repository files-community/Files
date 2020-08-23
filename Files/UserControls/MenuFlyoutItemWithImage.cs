using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.UserControls
{
    public class MenuFlyoutItemWithImage : MenuFlyoutItem
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
            
        }
    }
}
