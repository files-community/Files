// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using Windows.Foundation;
using WinRT;

namespace Files.App.Views.Layouts
{
	/// <summary>
	/// Lays out the cells of a <see cref="DetailsLayoutPage"/> row horizontally in column order.
	/// </summary>
	public sealed partial class DetailsLayoutRowPanel : Panel
	{
		private const double PrimaryCellIndent = 8;

		private readonly Dictionary<string, TextBlock> _generatedCells = [];
		private readonly HashSet<string> _textProperties = [];
		private INotifyPropertyChanged? _item;
		private double[] _childWidths = [];
		private int[] _orderedIndices = [];
		private int[] _orderKeys = [];

		public static readonly DependencyProperty ColumnOrderProperty =
			DependencyProperty.Register(
				nameof(ColumnOrder),
				typeof(object),
				typeof(DetailsLayoutRowPanel),
				new PropertyMetadata(null, (d, _) => ((DetailsLayoutRowPanel)d).SyncGeneratedCells()));

		public object? ColumnOrder
		{
			get => GetValue(ColumnOrderProperty);
			set => SetValue(ColumnOrderProperty, value);
		}

		public static readonly DependencyProperty CellStyleProperty =
			DependencyProperty.Register(
				nameof(CellStyle),
				typeof(Style),
				typeof(DetailsLayoutRowPanel),
				new PropertyMetadata(null, (d, _) => ((DetailsLayoutRowPanel)d).ApplyCellStyle()));

		/// <summary>
		/// Gets or sets the style of the cells generated for text columns.
		/// </summary>
		public Style? CellStyle
		{
			[DynamicWindowsRuntimeCast(typeof(Style))]
			get => (Style?)GetValue(CellStyleProperty);
			set => SetValue(CellStyleProperty, value);
		}

		public static readonly DependencyProperty PrimaryCellStyleProperty =
			DependencyProperty.Register(
				nameof(PrimaryCellStyle),
				typeof(Style),
				typeof(DetailsLayoutRowPanel),
				new PropertyMetadata(null, (d, _) => ((DetailsLayoutRowPanel)d).UpdatePrimaryCell()));

		/// <summary>
		/// Gets or sets the style of the text in the first shown column.
		/// </summary>
		public Style? PrimaryCellStyle
		{
			[DynamicWindowsRuntimeCast(typeof(Style))]
			get => (Style?)GetValue(PrimaryCellStyleProperty);
			set => SetValue(PrimaryCellStyleProperty, value);
		}

		public static readonly DependencyProperty LayoutVersionProperty =
			DependencyProperty.Register(
				nameof(LayoutVersion),
				typeof(int),
				typeof(DetailsLayoutRowPanel),
				new PropertyMetadata(0, (d, _) => ((DetailsLayoutRowPanel)d).OnLayoutVersionChanged()));

		public int LayoutVersion
		{
			get => (int)GetValue(LayoutVersionProperty);
			set => SetValue(LayoutVersionProperty, value);
		}

		public DetailsLayoutRowPanel()
		{
			DataContextChanged += (_, _) => AttachItem();
			Loaded += (_, _) => AttachItem();
			Unloaded += (_, _) => DetachItem();
			ActualThemeChanged += (_, _) => RecreateGeneratedCells();
		}

		/// <summary>
		/// Recreates the cells created in code so that they pick up the current theme.
		/// </summary>
		private void RecreateGeneratedCells()
		{
			foreach (var cell in _generatedCells.Values)
				Children.Remove(cell);

			_generatedCells.Clear();
			SyncGeneratedCells();
		}

		public void RefreshGeneratedCells()
		{
			if (_generatedCells.Count == 0 || DataContext is not ListedItem item)
				return;

			foreach (var (id, cell) in _generatedCells)
			{
				var definition = DetailsLayoutColumnDefinitions.Get(id);
				cell.Text = definition?.GetText?.Invoke(item) ?? string.Empty;
				if (definition?.GetToolTip is { } getToolTip)
					ToolTipService.SetToolTip(cell, getToolTip(item));
				else
					UpdateToolTip(cell);
			}
		}

		private void AttachItem()
		{
			DetachItem();
			_item = DataContext as INotifyPropertyChanged;
			if (_item is not null)
				_item.PropertyChanged += Item_PropertyChanged;

			RefreshGeneratedCells();
		}

		private void DetachItem()
		{
			if (_item is not null)
				_item.PropertyChanged -= Item_PropertyChanged;

			_item = null;
		}

		private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.PropertyName) &&
				!_textProperties.Contains(e.PropertyName))
			{
				return;
			}

			if (DispatcherQueue.HasThreadAccess)
				RefreshGeneratedCells();
			else
				DispatcherQueue.TryEnqueue(RefreshGeneratedCells);
		}

		private void ApplyCellStyle()
		{
			foreach (var cell in _generatedCells.Values)
				cell.Style = CellStyle;

			UpdatePrimaryCell();
		}

		/// <summary>
		/// Indents the cell of the first shown column and gives its text the primary style.
		/// </summary>
		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		[DynamicWindowsRuntimeCast(typeof(TextBlock))]
		[DynamicWindowsRuntimeCast(typeof(Panel))]
		private void UpdatePrimaryCell()
		{
			var primaryId = (ColumnOrder as DetailsLayoutColumnOrder)?.GetPrimaryId();
			foreach (var child in Children)
			{
				if (child is not FrameworkElement cell || DetailsLayoutColumnExtensions.GetId(cell) is not { } id || id == DetailsLayoutColumnIds.Icon)
					continue;

				var isPrimary = id == primaryId;
				var margin = isPrimary ? new Thickness(PrimaryCellIndent, 0, 0, 0) : default;
				if (!cell.Margin.Equals(margin))
					cell.Margin = margin;

				var style = isPrimary ? PrimaryCellStyle : CellStyle;
				if (cell is TextBlock textBlock)
				{
					SetTextStyle(textBlock, style);
				}
				else if (cell is Panel panel)
				{
					foreach (var element in panel.Children)
					{
						if (element is TextBlock cellText)
							SetTextStyle(cellText, style);
					}
				}
			}
		}

		private static void SetTextStyle(TextBlock textBlock, Style? style)
		{
			if (!ReferenceEquals(textBlock.Style, style))
				textBlock.Style = style;
		}

		private void OnLayoutVersionChanged()
		{
			if (AreGeneratedCellsCurrent())
			{
				UpdatePrimaryCell();
				InvalidateMeasure();
			}
			else
			{
				SyncGeneratedCells();
			}
		}

		private bool AreGeneratedCellsCurrent()
		{
			if ((ColumnOrder as DetailsLayoutColumnOrder)?.Columns is not { } columns)
				return _generatedCells.Count == 0;

			var count = 0;
			foreach (var id in columns.AllColumnIds)
			{
				if (!IsGeneratedColumn(columns, id))
					continue;

				if (!_generatedCells.ContainsKey(id))
					return false;

				count++;
			}

			return count == _generatedCells.Count;
		}

		private static bool IsGeneratedColumn(ColumnsViewModel columns, string id)
		{
			return DetailsLayoutColumnDefinitions.Get(id) is { GetText: not null } &&
				columns.FindColumn(id)?.Visibility == Visibility.Visible;
		}

		private void SyncGeneratedCells()
		{
			var columns = (ColumnOrder as DetailsLayoutColumnOrder)?.Columns;
			var columnIds = columns?.AllColumnIds.Where(id => IsGeneratedColumn(columns, id)).ToHashSet() ?? [];

			foreach (var id in _generatedCells.Keys.Where(id => !columnIds.Contains(id)).ToArray())
			{
				Children.Remove(_generatedCells[id]);
				_generatedCells.Remove(id);
			}

			var hasNewCells = false;
			foreach (var id in columnIds)
			{
				if (_generatedCells.ContainsKey(id))
					continue;

				var cell = new TextBlock { Padding = new(10, 0, 0, 0), Style = CellStyle };
				if (DetailsLayoutColumnDefinitions.Get(id)?.GetToolTip is null)
					cell.IsTextTrimmedChanged += (sender, _) => UpdateToolTip(sender);

				DetailsLayoutColumnExtensions.SetId(cell, id);
				_generatedCells[id] = cell;
				Children.Add(cell);
				hasNewCells = true;
			}

			_textProperties.Clear();
			foreach (var id in _generatedCells.Keys)
				_textProperties.UnionWith(DetailsLayoutColumnDefinitions.Get(id)?.TextProperties ?? []);

			if (hasNewCells)
				RefreshGeneratedCells();

			UpdatePrimaryCell();
			InvalidateMeasure();
		}

		private static void UpdateToolTip(TextBlock textBlock)
		{
			ToolTipService.SetToolTip(textBlock, textBlock.IsTextTrimmed ? textBlock.Text : null);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var columns = (ColumnOrder as DetailsLayoutColumnOrder)?.Columns;
			var children = Children;
			var count = children.Count;

			if (_childWidths.Length < count)
			{
				_childWidths = new double[count];
				_orderedIndices = new int[count];
				_orderKeys = new int[count];
			}

			double width = 0, height = 0;
			for (int i = 0; i < count; i++)
			{
				var child = children[i];
				var columnWidth = GetColumnWidth(columns, DetailsLayoutColumnExtensions.GetId(child));

				child.Measure(new Size(double.IsNaN(columnWidth) ? double.PositiveInfinity : columnWidth, availableSize.Height));
				_childWidths[i] = double.IsNaN(columnWidth) ? child.DesiredSize.Width : columnWidth;
				width += _childWidths[i];
				height = Math.Max(height, child.DesiredSize.Height);
			}

			return new(width, height);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var order = ColumnOrder as DetailsLayoutColumnOrder ?? DetailsLayoutColumnOrder.Default;
			var children = Children;
			var count = Math.Min(children.Count, _childWidths.Length);

			for (int i = 0; i < count; i++)
			{
				var key = order.IndexOf(DetailsLayoutColumnExtensions.GetId(children[i]));

				int j = i - 1;
				while (j >= 0 && _orderKeys[j] > key)
				{
					_orderKeys[j + 1] = _orderKeys[j];
					_orderedIndices[j + 1] = _orderedIndices[j];
					j--;
				}

				_orderKeys[j + 1] = key;
				_orderedIndices[j + 1] = i;
			}

			double x = 0;
			for (int i = 0; i < count; i++)
			{
				var index = _orderedIndices[i];
				var child = children[index];
				var width = _childWidths[index];
				child.Arrange(new Rect(x, 0, width, Math.Max(finalSize.Height, child.DesiredSize.Height)));
				x += width;
			}

			return finalSize;
		}

		private static double GetColumnWidth(ColumnsViewModel? columns, string? id)
		{
			if (columns is null || id is null || columns.FindColumn(id) is not { } column)
				return double.NaN;

			return id == DetailsLayoutColumnIds.Icon ? column.LengthPixels : column.LengthIncludingGridSplitterPixels;
		}
	}
}
