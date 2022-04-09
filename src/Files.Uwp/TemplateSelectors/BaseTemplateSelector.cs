using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Uwp.TemplateSelectors
{
    internal abstract class BaseTemplateSelector<TItem> : DataTemplateSelector
    {
        protected sealed override DataTemplate SelectTemplateCore(object item)
        {
            return SelectTemplateCore((TItem?)item);
        }

        protected sealed override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return SelectTemplateCore((TItem?)item, container);
        }

        protected virtual DataTemplate SelectTemplateCore(TItem? item)
        {
            return base.SelectTemplateCore(item);
        }

        protected virtual DataTemplate SelectTemplateCore(TItem? item, DependencyObject container)
        {
            return base.SelectTemplateCore(item, container);
        }
    }
}
