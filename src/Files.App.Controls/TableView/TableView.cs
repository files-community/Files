// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Controls;
using System.Collections.Specialized;
using Windows.Foundation.Collections;

namespace Files.App.Controls
{
	public partial class TableView : Control
	{
		private const string TemplatePartName_ColumnsPanel = "PART_ColumnsPanel";

		private Grid? _columnsPanel;

		public TableView()
		{
			Columns = [];

			DefaultStyleKey = typeof(TableView);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_columnsPanel = GetTemplateChild(TemplatePartName_ColumnsPanel) as Grid
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ColumnsPanel} in the given {nameof(TableView)}'s style.");

			Unloaded += TableView_Unloaded;

			UpdateColumns();

			if (View is ListViewBase listViewBase)
			{
				listViewBase.ContainerContentChanging -= ListViewBase_ContainerContentChanging;
				listViewBase.Items.VectorChanged -= ListViewBase_Items_VectorChanged;
				listViewBase.Unloaded -= ListViewBase_Unloaded;

				listViewBase.ContainerContentChanging += ListViewBase_ContainerContentChanging;
				listViewBase.Items.VectorChanged += ListViewBase_Items_VectorChanged;
				listViewBase.Unloaded += ListViewBase_Unloaded;
			}
		}

		private void TableView_Unloaded(object sender, RoutedEventArgs e)
		{
			Unloaded -= TableView_Unloaded;
		}

		private void Columns_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (_columnsPanel is null)
				return;

			// TODO: Re-arrange ColumnDefinitions accordingly

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					int insertIndex = e.NewStartingIndex;
					foreach (UIElement item in e.NewItems!)
						_columnsPanel.Children.Insert(insertIndex++, item);
					break;

				case NotifyCollectionChangedAction.Remove:
					int removeIndex = e.OldStartingIndex;
					for (int i = 0; i < e.OldItems!.Count; i++)
						_columnsPanel.Children.RemoveAt(removeIndex);
					break;

				case NotifyCollectionChangedAction.Replace:
					int replaceIndex = e.OldStartingIndex;
					for (int i = 0; i < e.OldItems!.Count; i++)
						_columnsPanel.Children.RemoveAt(replaceIndex);
					foreach (UIElement item in e.NewItems!)
						_columnsPanel.Children.Insert(replaceIndex++, item);
					break;

				case NotifyCollectionChangedAction.Move:
					if (e.OldItems!.Count is 1)
					{
						if (e.OldItems[0] is UIElement movedItem)
						{
							_columnsPanel.Children.RemoveAt(e.OldStartingIndex);
							_columnsPanel.Children.Insert(e.NewStartingIndex, movedItem);
						}
					}
					else
					{
						throw new NotSupportedException("It is not supported to move multiple items.");
					}
					break;

				case NotifyCollectionChangedAction.Reset:
					_columnsPanel.Children.Clear();
					foreach (UIElement item in Columns)
						_columnsPanel.Children.Add(item);
					break;
			}
		}

		private void ListViewBase_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			//Debug.WriteLine($"ListViewBase_ContainerContentChanging {args.ItemIndex}");

			var itemContainer = args.ItemContainer as Control;
			RecycleRowOf(sender, itemContainer, args.ItemIndex);

			// Recycle the index 1 item since ContainerContentChanging doesn't get called for the index 1st item somehow.
			if (args.ItemIndex is 1 && sender.ContainerFromIndex(0) is Control container)
			{
				RecycleRowOf(sender, container, 0);
			}
		}

		private void ListViewBase_Items_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
		{
			if (View is not ListViewBase listViewBase)
				return;

			// Only need to handle Inserted and Removed because we'll handle everything else in the ListViewBase_ContainerContentChanging event
			if (args.CollectionChange is CollectionChange.ItemInserted or CollectionChange.ItemRemoved)
			{
				Debug.WriteLine($"ListViewBase_Items_VectorChanged {args.Index}~{sender.Count}");

				int index = (int)args.Index;
				for (int i = index; i < sender.Count; i++)
				{
					var itemContainer = listViewBase.ContainerFromIndex(i) as Control;
					if (itemContainer != null)
					{
						RecycleRowOf(listViewBase, itemContainer, i);
					}
				}
			}
		}

		private void ListViewBase_Unloaded(object sender, RoutedEventArgs e)
		{
			if (sender is ListViewBase listViewBase)
			{
				listViewBase.Unloaded -= ListViewBase_Unloaded;
				listViewBase.ContainerContentChanging -= ListViewBase_ContainerContentChanging;
				listViewBase.Items.VectorChanged -= ListViewBase_Items_VectorChanged;
			}
		}

		private void RecycleRowOf(ListViewBase sender, FrameworkElement itemContainer, int itemIndex)
		{
			if (itemContainer is not ListViewItem listViewItem ||
				listViewItem.ContentTemplateRoot is not TableViewRow row ||
				sender.Items.ElementAt(itemIndex) is not ITableViewCellValueProvider cellValueProvider)
				return;

			row.SetOwner(this);

			row.Children.Clear();

			foreach (var column in Columns)
				row.Children.Add(column.BuildCellElement(cellValueProvider));

			row.InvalidateArrange();
			row.InvalidateMeasure();
		}

		public void RearrangeRows()
		{
			if (View is ListViewBase listViewBase)
			{
				if (listViewBase.ItemsPanelRoot is ItemsStackPanel itemsStackPanel)
				{
					for (int index = itemsStackPanel.FirstCacheIndex;
						index <= itemsStackPanel.LastCacheIndex;
						index++)
					{
						if (listViewBase.ContainerFromIndex(index) is not ListViewItem listViewItem ||
							listViewItem.ContentTemplateRoot is not TableViewRow row)
							continue;

						//Debug.WriteLine($"RearrangeRows {index}");

						row.InvalidateArrange();
						row.InvalidateMeasure();
					}
				}
			}
		}

		private void UpdateColumns()
		{
			if (_columnsPanel?.Children.Count is not 0) // TODO: Handle updating the property itself
				return;

			foreach (var column in Columns)
			{
				_columnsPanel.Children.Add(column);
				_columnsPanel.ColumnDefinitions.Add(new() { Width = new(0, GridUnitType.Auto) });
				Grid.SetColumn(column, _columnsPanel.ColumnDefinitions.Count - 1);
				column.SetOwner(this);
			}

			_columnsPanel.ColumnDefinitions.Add(new() { Width = new(0, GridUnitType.Auto) });

			// Put resize visual at the end of the Children collection to make sure it's on top of other elements
			int resizeVisualPosition = 0;
			foreach (var column in Columns)
			{
				// Set resize visual
				var resizeVisual = new ResizeVisual { Target = column, Orientation = Orientation.Horizontal };
				_columnsPanel.Children.Add(resizeVisual);
				Grid.SetColumn(resizeVisual, resizeVisualPosition);
				Grid.SetColumnSpan(resizeVisual, 2);

				resizeVisual.DragDelta += ResizeVisual_DragDelta;
				resizeVisual.DragCompleted += ResizeVisual_DragCompleted;

				resizeVisualPosition++;
			}
		}

		private void ResizeVisual_DragCompleted(object sender, DragCompletedEventArgs e)
		{
			if (sender is ResizeVisual { Target: TableViewColumn column })
			{
				column.OnColumnBeingResized();
			}
		}

		private void ResizeVisual_DragDelta(object sender, DragDeltaEventArgs e)
		{
			if (sender is ResizeVisual { Target: TableViewColumn column })
			{
				column.OnColumnResizeCompleted();
			}
		}
	}
}
