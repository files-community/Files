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
		public partial object? Header { get; set; }

		[GeneratedDependencyProperty]
		public partial DataTemplate? HeaderTemplate { get; set; }

		[GeneratedDependencyProperty]
		public partial DataTemplateSelector? HeaderTemplateSelector { get; set; }

		[GeneratedDependencyProperty]
		public partial Style? HeaderStyle { get; set; }

		[GeneratedDependencyProperty]
		public partial string? HeaderStringFormat { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool CanUserResize { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool CanUserReorder { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool CanUserSort { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsReadOnly { get; set; }

		[GeneratedDependencyProperty]
		public partial ListSortDirection? SortDirection { get; set; }

		[GeneratedDependencyProperty]
		public partial string? Binding { get; set; }

		partial void OnSortDirectionChanged(ListSortDirection? newValue)
		{
			if (ColumnWidth.IsAuto)
				_autoDesiredWidth = 0;
			UpdateSortVisualState(true);
			NotifySortDirectionChanged();
			NotifyPropertyChanged(TableViewNotificationTarget.ColumnLayout | TableViewNotificationTarget.ColumnHeaders);
		}

		partial void OnCanUserResizeChanged(bool newValue)
		{
			NotifyInteractionOptionsChanged();
		}

		partial void OnCanUserReorderChanged(bool newValue)
		{
			NotifyInteractionOptionsChanged();
		}

		partial void OnCanUserSortChanged(bool newValue)
		{
			ResetPointerEventVisual();
		}

		partial void OnIsReadOnlyChanged(bool newValue)
		{
			if (newValue && GetOwner() is { } owner)
				owner.CancelEdit(this, TableViewEditEndingReason.ReadOnlyChanged);

			NotifyPropertyChanged(TableViewNotificationTarget.VisibleRows);
		}

		partial void OnHeaderChanged(object? newValue)
		{
			Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(this, GetHeaderAutomationName(newValue));
			UpdateHeaderContent();
			NotifyHeaderAppearanceChanged();
		}

		partial void OnHeaderTemplateChanged(DataTemplate? newValue)
		{
			UpdateHeaderContent();
			NotifyHeaderAppearanceChanged();
		}

		partial void OnHeaderTemplateSelectorChanged(DataTemplateSelector? newValue)
		{
			UpdateHeaderContent();
			NotifyHeaderAppearanceChanged();
		}

		partial void OnHeaderStyleChanged(Style? newValue)
		{
			UpdateHeaderStyle();
			NotifyHeaderAppearanceChanged();
		}

		partial void OnHeaderStringFormatChanged(string? newValue)
		{
			UpdateHeaderContent();
			NotifyHeaderAppearanceChanged();
		}

		partial void OnBindingChanged(string? newValue)
		{
			if (ColumnWidth.IsAuto)
				_autoDesiredWidth = 0;

			NotifyPropertyChanged(
				TableViewNotificationTarget.VisibleRows |
				TableViewNotificationTarget.ColumnLayout);
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
			if (columnWidth.Value < 0 || double.IsNaN(columnWidth.Value))
				throw new ArgumentOutOfRangeException(nameof(columnWidth));
		}

		private void NotifyHeaderAppearanceChanged()
		{
			if (ColumnWidth.IsAuto)
				_autoDesiredWidth = 0;

			NotifyPropertyChanged(TableViewNotificationTarget.ColumnLayout | TableViewNotificationTarget.ColumnHeaders);
		}
	}
}
