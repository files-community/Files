using Files.Views.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            DataTemplate _returnTemplate = itemsControl.IndexFromContainer(container) == (itemsControl.ItemsSource as ObservableCollection<PathBoxItem>).Count - 1 ? CurrentItem : ParentItems;
            return _returnTemplate;
        }
    }
}