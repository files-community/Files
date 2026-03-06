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
		public partial object? View { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsColumnResizing { get; internal protected set; }

		partial void OnColumnsPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.OldValue is INotifyCollectionChanged oldColumns)
				oldColumns.CollectionChanged -= Columns_CollectionChanged;
			if (e.NewValue is INotifyCollectionChanged newColumns)
				newColumns.CollectionChanged += Columns_CollectionChanged;

			foreach (var column in Columns)
				column.EnsureOwner(this);

			RefreshVisibleRows();
			InvalidateLayoutOfAllRows();
		}

		partial void OnColumnsSourcePropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.OldValue is INotifyCollectionChanged oldColumnsSource)
				oldColumnsSource.CollectionChanged -= ColumnsSource_CollectionChanged;
			if (e.NewValue is INotifyCollectionChanged newColumnsSource)
				newColumnsSource.CollectionChanged += ColumnsSource_CollectionChanged;

			SynchronizeColumnsFromSource();
		}

		partial void OnColumnTemplateChanged(DataTemplate? newValue)
		{
			if (ColumnsSource is null)
				return;

			RefreshVisibleRows();
			InvalidateLayoutOfAllRows();
		}

		partial void OnColumnTemplateSelectorChanged(DataTemplateSelector? newValue)
		{
			if (ColumnsSource is null)
				return;

			RefreshVisibleRows();
			InvalidateLayoutOfAllRows();
		}

		partial void OnIsColumnResizingChanged(bool newValue)
		{
			if (newValue)
			{
				foreach (var column in Columns)
					column.ResetPointerEventVisual();
			}
		}
	}
}
