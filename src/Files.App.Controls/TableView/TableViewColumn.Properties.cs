// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;

namespace Files.App.Controls
{
	public partial class TableViewColumn
	{
		public static readonly DependencyProperty ColumnWidthProperty =
			DependencyProperty.Register(
				nameof(ColumnWidth),
				typeof(GridLength),
				typeof(TableViewColumn),
				new PropertyMetadata(GridLength.Auto, OnColumnWidthPropertyChanged));

		public GridLength ColumnWidth
		{
			get => (GridLength)GetValue(ColumnWidthProperty);
			set
			{
				ValidateColumnWidth(value);
				SetValue(ColumnWidthProperty, value);
			}
		}

		[GeneratedDependencyProperty]
		public partial string? Header { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool CanBeResized { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool CanBeReordered { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool CanBeSorted { get; set; }

		[GeneratedDependencyProperty]
		public partial ListSortDirection? SortDirection { get; set; }

		[GeneratedDependencyProperty]
		public partial string? Binding { get; set; }

		partial void OnSortDirectionChanged(ListSortDirection? newValue)
		{
			UpdateSortVisualState(true);
			NotifySortDirectionChanged();
		}

		partial void OnCanBeResizedChanged(bool newValue)
		{
			NotifyInteractionOptionsChanged();
		}

		partial void OnCanBeReorderedChanged(bool newValue)
		{
			NotifyInteractionOptionsChanged();
		}

		partial void OnCanBeSortedChanged(bool newValue)
		{
			ResetPointerEventVisual();
		}

		partial void OnHeaderChanged(string? newValue)
		{
			Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(this, newValue ?? string.Empty);
		}

		private static void OnColumnWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue is GridLength newValue)
				ValidateColumnWidth(newValue);

			if (d is TableViewColumn column)
				column.NotifyColumnWidthChanged();
		}

		private static void ValidateColumnWidth(GridLength columnWidth)
		{
			if (columnWidth.IsStar)
				throw new NotSupportedException($"{nameof(TableViewColumn)}.{nameof(ColumnWidth)} does not support star sizing. Use pixel width or Auto.");
		}
	}
}
