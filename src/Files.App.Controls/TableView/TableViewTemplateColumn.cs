// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public partial class TableViewTemplateColumn : TableViewColumn
	{
		public static readonly DependencyProperty CellTemplateProperty =
			DependencyProperty.Register(nameof(CellTemplate), typeof(DataTemplate), typeof(TableViewTemplateColumn), new PropertyMetadata(null));

		public DataTemplate? CellTemplate
		{
			get => (DataTemplate?)GetValue(CellTemplateProperty);
			set => SetValue(CellTemplateProperty, value);
		}

		public TableViewTemplateColumn()
		{
			DefaultStyleKey = typeof(TableViewTemplateColumn);
		}

		public override FrameworkElement GenerateElement(object dataItem)
		{
			if (CellTemplate?.LoadContent() is not FrameworkElement element)
				throw new InvalidOperationException($"{nameof(CellTemplate)} must produce a {nameof(FrameworkElement)}.");

			element.DataContext = dataItem;
			return element;
		}

		protected internal override bool UpdateElement(FrameworkElement element, object dataItem)
		{
			element.DataContext = dataItem;
			return true;
		}
	}
}
