using Files.Views;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Helpers
{
    public class ItemsDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ParentItems { get; set; }
        public DataTemplate CurrentItem { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            var itemsControl = ItemsControl.ItemsControlFromItemContainer(container);
            return itemsControl.IndexFromContainer(container) == (itemsControl.ItemsSource as ObservableCollection<PathBoxItem>).Count - 1 ? CurrentItem : ParentItems;
        }
    }
}