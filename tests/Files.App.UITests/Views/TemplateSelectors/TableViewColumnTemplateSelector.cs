// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.UITests.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UITests.Views.TemplateSelectors
{
	internal sealed class TableViewColumnTemplateSelector : DataTemplateSelector
	{
		public DataTemplate? TextColumnTemplate { get; set; }

		public DataTemplate? DateColumnTemplate { get; set; }

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			if (item is TableViewColumnModel { ValueType: TableViewColumnValueType.DateTimeOffset })
				return DateColumnTemplate ?? TextColumnTemplate ?? base.SelectTemplateCore(item, container);

			return TextColumnTemplate ?? base.SelectTemplateCore(item, container);
		}
	}
}
