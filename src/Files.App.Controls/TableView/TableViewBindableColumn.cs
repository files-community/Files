// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;

namespace Files.App.Controls
{
	public abstract partial class TableViewBindableColumn : TableViewColumn
	{
		[GeneratedDependencyProperty]
		public partial Style? ElementStyle { get; set; }

		[GeneratedDependencyProperty]
		public partial Style? EditingElementStyle { get; set; }

		protected T GetPropertyValue<T>(object dataItem)
		{
			if (string.IsNullOrEmpty(Binding) ||
				dataItem is not ITableViewCellValueProvider cellValueProvider)
				throw new ArgumentException($"The data source must implement {nameof(ITableViewCellValueProvider)} to get cell value.", $"{dataItem}");

			return cellValueProvider.GetValue<T>(Binding);
		}

		protected bool SetPropertyValue<T>(object dataItem, T value)
		{
			if (string.IsNullOrEmpty(Binding) ||
				dataItem is not ITableViewCellValueEditor cellValueEditor)
				throw new ArgumentException($"The data source must implement {nameof(ITableViewCellValueEditor)} to edit cell value.", $"{dataItem}");

			return cellValueEditor.TrySetValue(Binding, value);
		}
	}
}
