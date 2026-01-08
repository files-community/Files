// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Windows.Foundation;

namespace Files.App.Controls
{
	public partial class TableViewRow : Control
	{
		private const string TemplatePartName_CellsPanel = "PART_CellsPanel";

		private WeakReference<TableView>? _owner;

		private StackPanel? _cellsPanel;

		[GeneratedDependencyProperty]
		[SuppressMessage("DependencyPropertyGenerator", "WCTDPG0009:Non-nullable dependency property is not guaranteed to not be null", Justification = "This is initialized in the constructor.")]
		public partial ObservableCollection<UIElement> Children { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsRowDividerVisible { get; set; }

		public TableViewRow()
		{
			Children = [];

			DefaultStyleKey = typeof(TableViewRow);

			Loaded += TableViewRow_Loaded;
			Unloaded += TableViewRow_Unloaded;
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_cellsPanel = GetTemplateChild(TemplatePartName_CellsPanel) as StackPanel
				?? throw new MissingFieldException($"Could not find {TemplatePartName_CellsPanel} in the given {nameof(TableViewRow)}'s style.");

			foreach (var child in Children)
			{
				_cellsPanel.Children.Add(child);
			}
		}

		public void SetOwner(TableView owner)
		{
			_owner = new(owner);
		}

		private void TableViewRow_Loaded(object sender, RoutedEventArgs e)
		{
			if (this.FindAscendant<TableView>() is { } owner)
			{
				Debug.WriteLine("TableViewRow_Loaded");

				_owner = new(owner);
			}
		}

		private void TableViewRow_Unloaded(object sender, RoutedEventArgs e)
		{
			if (_owner is null || !_owner.TryGetTarget(out var owner))
				return;

			_owner = null;

			Unloaded -= TableViewRow_Unloaded;
		}

		partial void OnChildrenPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.OldValue is INotifyCollectionChanged oldChildren)
				oldChildren.CollectionChanged -= Children_CollectionChanged;
			if (e.NewValue is INotifyCollectionChanged newChildren)
				newChildren.CollectionChanged += Children_CollectionChanged;
		}

		private void Children_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (_cellsPanel is null)
				return;

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					int insertIndex = e.NewStartingIndex;
					foreach (UIElement item in e.NewItems!)
						_cellsPanel.Children.Insert(insertIndex++, item);
					break;

				case NotifyCollectionChangedAction.Remove:
					int removeIndex = e.OldStartingIndex;
					for (int i = 0; i < e.OldItems!.Count; i++)
						_cellsPanel.Children.RemoveAt(removeIndex);
					break;

				case NotifyCollectionChangedAction.Replace:
					int replaceIndex = e.OldStartingIndex;
					for (int i = 0; i < e.OldItems!.Count; i++)
						_cellsPanel.Children.RemoveAt(replaceIndex);
					foreach (UIElement item in e.NewItems!)
						_cellsPanel.Children.Insert(replaceIndex++, item);
					break;

				case NotifyCollectionChangedAction.Move:
					if (e.OldItems!.Count == 1)
					{
						if (e.OldItems[0] is UIElement movedItem)
						{
							_cellsPanel.Children.RemoveAt(e.OldStartingIndex);
							_cellsPanel.Children.Insert(e.NewStartingIndex, movedItem);
						}
					}
					else
					{
						throw new NotSupportedException("It is not supported to move multiple items.");
					}
					break;

				case NotifyCollectionChangedAction.Reset:
					_cellsPanel.Children.Clear();
					foreach (UIElement item in Children)
						_cellsPanel.Children.Add(item);
					break;
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (Children is null || _owner is null || !_owner.TryGetTarget(out var owner))
				return finalSize;

			int index = 0;
			double x = 0;
			double width;
			TableViewColumn column;
			double maxHeight = 0;

			foreach (var child in Children.Cast<FrameworkElement>())
			{
				column = owner.Columns[index];
				width = column.ActualWidth;

				maxHeight = Math.Max(maxHeight, column.ActualHeight);

				child.Arrange(new(
					x,
					0,
					width,
					finalSize.Height));

				x += width;
				index++;
			}

			return new Size(x, maxHeight);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (Children is null || Children.Count is 0 || _owner is null || !_owner.TryGetTarget(out var owner))
				return new(availableSize.Width, 0);

			int index = 0;
			double maxHeight = 0;

			foreach (var child in Children)
			{
				var column = owner.Columns[index];

				child.Measure(new(Math.Max(column.ActualWidth - (column.Padding.Left + column.Padding.Right), 0D), availableSize.Height));

				maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);

				index++;
			}

			return new(availableSize.Width, maxHeight);
		}
	}
}
