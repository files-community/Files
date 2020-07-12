using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Helpers
{
    public class AttachedProperties : DependencyObject
    {
        public static readonly DependencyProperty BitmapIconProperty =
        DependencyProperty.RegisterAttached(
          "BitmapIcon",
          typeof(BitmapImage),
          typeof(AttachedProperties),
          new PropertyMetadata(null)
        );
        public static void SetBitmapIcon(UIElement element, BitmapImage value)
        {
            element.SetValue(BitmapIconProperty, value);
        }
        public static BitmapImage GetBitmapIcon(UIElement element)
        {
            return (BitmapImage)element.GetValue(BitmapIconProperty);
        }
    }
}
