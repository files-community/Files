// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.TemplateSelectors
{
	/// <summary>
	/// Provides template selector for Path Breadcrumb template items.
	/// </summary>
	internal sealed class StatusCenterItemSelector : DataTemplateSelector
	{
		public DataTemplate? ExpanderStyleItem { get; set; }

		public DataTemplate? CardStyleItem { get; set; }

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			var itemsControl = ListView.ItemsControlFromItemContainer(container);

			if (itemsControl.ItemsSource is IList<StatusCenterItem> items)
			{
				return
					((StatusCenterItem)itemsControl.ItemFromContainer(container)).ItemState == StatusCenterItemState.InProgress
						? ExpanderStyleItem!
						: CardStyleItem!;
			}
			else
			{
				throw new ArgumentException($"Type of {nameof(itemsControl.ItemsSource)} doesn't match IList<{nameof(StatusCenterItem)}>");
			}
		}
	}
}
