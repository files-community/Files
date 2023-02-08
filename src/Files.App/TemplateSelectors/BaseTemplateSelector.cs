using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.TemplateSelectors
{
	internal abstract class BaseTemplateSelector<TItem> : DataTemplateSelector
	{
		protected sealed override DataTemplate SelectTemplateCore(object item)
		{
			SelectTemplateCore((TItem?)item);
		}

		protected sealed override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			SelectTemplateCore((TItem?)item, container);
		}

		protected virtual DataTemplate SelectTemplateCore(TItem? item)
		{
			base.SelectTemplateCore(item);
		}

		protected virtual DataTemplate SelectTemplateCore(TItem? item, DependencyObject container)
		{
			base.SelectTemplateCore(item, container);
		}
	}
}
