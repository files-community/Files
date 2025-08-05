// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using System.Collections.Specialized;

#pragma warning disable WCTDPG0009

namespace Files.App.Controls
{
	public partial class TableView
	{
		[GeneratedDependencyProperty]
		public partial ObservableCollection<TableViewColumn> Columns { get; set; }

		[GeneratedDependencyProperty]
		public partial object View { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsColumnResizing { get; internal protected set; }

		partial void OnColumnsPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.OldValue is INotifyCollectionChanged oldColumns)
				oldColumns.CollectionChanged -= Columns_CollectionChanged;
			if (e.NewValue is INotifyCollectionChanged newColumns)
				newColumns.CollectionChanged += Columns_CollectionChanged;

			if (_columnsPanel is not null)
			{
				_columnsPanel.Children.Clear();
				_columnsPanel.ColumnDefinitions.Clear();
			}
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
