// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Collections.Specialized;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace Files.App.UserControls.DataTable
{
	internal class DataRow : Panel
	{
		private DataTable? _dataTable;

		public DataRow()
		{
			Unloaded += DataRow_Unloaded;
		}

		private void DataRow_Unloaded(object sender, RoutedEventArgs e)
		{
			_dataTable?.Rows.Remove(this);
			_dataTable = null;
		}

		private void InitializeParentHeaderConnection()
		{
			if (this.FindAscendant<ListViewBase>() is not ListViewBase itemsPresenter)
				return;


			if (itemsPresenter.Header is not DataTable dataTable)
				return;

			_dataTable = dataTable;

			_dataTable.Rows.Add(this);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			InitializeParentHeaderConnection();

			double maxHeight = 0;

			// Return our parent's size as the desired size.
			if (Children.Count == 0 || _dataTable is null || _dataTable.Items.Count != Children.Count)
				return new(availableSize.Width, maxHeight);

			for (int i = 0; i < Children.Count; i++)
			{
				if (_dataTable.Items[i] is DataColumn { CurrentWidth.GridUnitType: GridUnitType.Pixel } pixel)
				{
					Children[i].Measure(new(pixel.DesiredWidth.Value, availableSize.Height));
				}

				maxHeight = Math.Max(maxHeight, Children[i].DesiredSize.Height);
			}

			return new(availableSize.Width, maxHeight);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (_dataTable is null)
				return finalSize;

			int column = 0;
			double x = 0;

			// Try and grab Column Spacing from DataTable, if not a parent Grid, if not 0.
			double spacing = _dataTable?.ColumnSpacing ?? 0;

			double width = 0;

			int i = 0;

			foreach (UIElement child in Children.Where(static e => e.Visibility == Visibility.Visible))
			{
				// TODO: Need to check Column visibility here as well...
				if (column < _dataTable?.Items.Count)
				{
					// TODO: This is messy...
					width = (_dataTable.Items[column++] as DataColumn)?.ActualWidth ?? 0;
				}

				// Note: For Auto, since we measured our children and bubbled that up to the DataTable layout, then the DataColumn size we grab above should account for the largest of our children.
				if (i == 0)
				{
					child.Arrange(new Rect(x, 0, width, finalSize.Height));
				}
				else
				{
					// If we're in a tree, remove the indentation from the layout of columns beyond the first.
					child.Arrange(new Rect(x, 0, width, finalSize.Height));
				}

				x += width + spacing;

				i++;
			}

			return new Size(x - spacing, finalSize.Height);
		}
	}
}
