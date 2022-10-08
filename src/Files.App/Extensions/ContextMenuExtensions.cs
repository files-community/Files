using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Extensions
{
    public class ContextMenuExtensions : DependencyObject
    {
        public static ItemsControl GetItemsControl(DependencyObject obj)
        {
            return (ItemsControl)obj.GetValue(ItemsControlProperty);
        }

        public static void SetItemsControl(DependencyObject obj, ItemsControl value)
        {
            obj.SetValue(ItemsControlProperty, value);
        }

        public static readonly DependencyProperty ItemsControlProperty =
            DependencyProperty.RegisterAttached("ItemsControl", typeof(ItemsControl), typeof(ContextMenuExtensions), new PropertyMetadata(null));
    }
}
