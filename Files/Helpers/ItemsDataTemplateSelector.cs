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
            DataTemplate _returnTemplate = new DataTemplate();
            var itemsControl = ItemsControl.ItemsControlFromItemContainer(container);
            _returnTemplate = (itemsControl.IndexFromContainer(container) == (itemsControl.ItemsSource as ObservableCollection<Files.PathBoxItem>).Count - 1) ? CurrentItem : ParentItems;
            return _returnTemplate;
        }
    }
}
