// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using System.Collections.Specialized;

#pragma warning disable WCTDPG0009

namespace Files.App.Controls
{
	public partial class TableView
	{
		[GeneratedDependencyProperty(IsLocalCacheEnabled = true)]
		public partial ObservableCollection<TableViewColumn> Columns { get; private set; }

		[GeneratedDependencyProperty(IsLocalCacheEnabled = true)]
		public partial object? ColumnsSource { get; set; }

		[GeneratedDependencyProperty(IsLocalCacheEnabled = true)]
		public partial DataTemplate? ColumnTemplate { get; set; }

		[GeneratedDependencyProperty(IsLocalCacheEnabled = true)]
		public partial DataTemplateSelector? ColumnTemplateSelector { get; set; }

		[GeneratedDependencyProperty(IsLocalCacheEnabled = true)]
		public partial ListViewBase? View { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool CanUserReorderColumns { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool CanUserResizeColumns { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool CanUserSortColumns { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsReadOnly { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsColumnResizing { get; internal protected set; }

		partial void OnColumnsPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.OldValue is INotifyCollectionChanged oldColumns)
				oldColumns.CollectionChanged -= Columns_CollectionChanged;
			if (e.NewValue is INotifyCollectionChanged newColumns)
				newColumns.CollectionChanged += Columns_CollectionChanged;

			if (ColumnsSource is null)
				SynchronizeActiveColumns();
		}

		partial void OnColumnsSourcePropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.OldValue is INotifyCollectionChanged oldColumnsSource)
				oldColumnsSource.CollectionChanged -= ColumnsSource_CollectionChanged;
			if (e.NewValue is INotifyCollectionChanged newColumnsSource)
				newColumnsSource.CollectionChanged += ColumnsSource_CollectionChanged;

			_columnsBySourceItem.Clear();
			SynchronizeActiveColumns();
		}

		partial void OnColumnTemplateChanged(DataTemplate? newValue)
		{
			if (ColumnsSource is null)
				return;

			_columnsBySourceItem.Clear();
			SynchronizeActiveColumns();
		}

		partial void OnColumnTemplateSelectorChanged(DataTemplateSelector? newValue)
		{
			if (ColumnsSource is null)
				return;

			_columnsBySourceItem.Clear();
			SynchronizeActiveColumns();
		}

		partial void OnViewPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			UnhookView();
			HookView(e.NewValue as ListViewBase);
			RefreshVisibleRows();
		}

		partial void OnCanUserReorderColumnsChanged(bool newValue)
		{
			UpdateColumnsPanelInteractionState();
		}

		partial void OnCanUserResizeColumnsChanged(bool newValue)
		{
			UpdateResizeVisualInteractionState();
		}

		partial void OnCanUserSortColumnsChanged(bool newValue)
		{
			foreach (var column in ActiveColumns)
				column.ResetPointerEventVisual();
		}

		partial void OnIsReadOnlyChanged(bool newValue)
		{
			if (newValue)
				CancelEdit();

			NotifyPropertyChanged(this, TableViewNotificationTarget.VisibleRows);
		}

		partial void OnIsColumnResizingChanged(bool newValue)
		{
			if (newValue)
			{
				foreach (var column in ActiveColumns)
					column.ResetPointerEventVisual();
			}
		}
	}
}
