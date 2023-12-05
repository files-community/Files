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
		// TODO: Create our own helper class here for the Header as well vs. straight-Grid.
		// TODO: WeakReference?
		private Panel? _parentPanel;
		private DataTable? _parentTable;

		private bool _isTreeView;
		private double _treePadding;

		public DataRow()
		{
			Unloaded += this.DataRow_Unloaded;
		}

		private void DataRow_Unloaded(object sender, RoutedEventArgs e)
		{
			// Remove our references on unloaded
			_parentTable?.Rows.Remove(this);
			_parentTable = null;
			_parentPanel = null;
		}

		private Panel? InitializeParentHeaderConnection()
		{
			// TODO: Think about this expression instead...
			//       Drawback: Can't have Grid between table and header
			//       Positive: don't have to restart climbing the Visual Tree if we don't find ItemsPresenter...
			////var parent = this.FindAscendant<FrameworkElement>(static (element) => element is ItemsPresenter or Grid);

			// TODO: Investigate what a scenario with an ItemsRepeater would look like (with a StackLayout, but using DataRow as the item's panel inside)
			Panel? panel = null;

			// 1a. Get parent ItemsPresenter to find header
			if (this.FindAscendant<ItemsPresenter>() is ItemsPresenter itemsPresenter)
			{
				// 2. Quickly check if the header is just what we're looking for.
				if (itemsPresenter.Header is Grid or DataTable)
				{
					panel = itemsPresenter.Header as Panel;
				}
				else
				{
					// 3. Otherwise, try and find the inner thing we want.
					panel = itemsPresenter.FindDescendant<Panel>(static (element) => element is Grid or DataTable);
				}

				// Check if we're in a TreeView
				_isTreeView = itemsPresenter.FindAscendant<TreeView>() is TreeView;
			}

			// 1b. If we can't find the ItemsPresenter, then we reach up outside to find the next thing we could use as a parent
			panel ??= this.FindAscendant<Panel>(static (element) => element is Grid or DataTable);

			// Cache actual datatable reference
			if (panel is DataTable table)
			{
				_parentTable = table;
				_parentTable.Rows.Add(this); // Add us to the row list.
			}

			return panel;
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			// We should probably only have to do this once ever?
			_parentPanel ??= InitializeParentHeaderConnection();

			double maxHeight = 0;

			if (Children.Count > 0)
			{
				// If we don't have a grid, just measure first child to get row height and take available space
				if (_parentPanel is null)
				{
					Children[0].Measure(availableSize);
					return new Size(availableSize.Width, Children[0].DesiredSize.Height);
				}
				// Handle DataTable Parent
				else if (_parentTable != null
						 && _parentTable.Children.Count == Children.Count)
				{
					// TODO: Need to check visibility
					// Measure all children since we need to determine the row's height at minimum
					for (int i = 0; i < Children.Count; i++)
					{
						if (_parentTable.Children[i] is DataColumn { CurrentWidth.GridUnitType: GridUnitType.Auto } col)
						{
							Children[i].Measure(availableSize);

							// For TreeView in the first column, we want the header to expand to encompass
							// the maximum indentation of the tree.
							double padding = 0;
							//// TODO: We only want/need to do this once? We may want to do if we're not an Auto column too...?
							if (i == 0 && _isTreeView)
							{
								// Get our containing grid from TreeViewItem, start with our indented padding
								var parentContainer = this.FindAscendant("MultiSelectGrid") as Grid;
								if (parentContainer != null)
								{
									_treePadding = parentContainer.Padding.Left;
									// We assume our 'DataRow' is in the last child slot of the Grid, need to know how large the other columns are.
									for (int j = 0; j < parentContainer.Children.Count - 1; j++)
									{
										// TODO: We may need to get the actual size here later in Arrange?
										_treePadding += parentContainer.Children[j].DesiredSize.Width;
									}
								}
								padding = _treePadding;
							}

							// TODO: Do we want this to ever shrink back?
							var prev = col.MaxChildDesiredWidth;
							col.MaxChildDesiredWidth = Math.Max(col.MaxChildDesiredWidth, Children[i].DesiredSize.Width + padding);
							if (col.MaxChildDesiredWidth != prev)
							{
								// If our measure has changed, then we have to invalidate the arrange of the DataTable
								_parentTable.ColumnResized();
							}

						}
						else if (_parentTable.Children[i] is DataColumn { CurrentWidth.GridUnitType: GridUnitType.Pixel } pixel)
						{
							Children[i].Measure(new(pixel.DesiredWidth.Value, availableSize.Height));
						}
						else
						{
							Children[i].Measure(availableSize);
						}

						maxHeight = Math.Max(maxHeight, Children[i].DesiredSize.Height);
					}
				}
				// Fallback for Grid Hybrid scenario...
				else if (_parentPanel is Grid grid
						 && _parentPanel.Children.Count == Children.Count
						 && grid.ColumnDefinitions.Count == Children.Count)
				{
					// TODO: Need to check visibility
					// Measure all children since we need to determine the row's height at minimum
					for (int i = 0; i < Children.Count; i++)
					{
						if (grid.ColumnDefinitions[i].Width.GridUnitType == GridUnitType.Pixel)
						{
							Children[i].Measure(new(grid.ColumnDefinitions[i].Width.Value, availableSize.Height));
						}
						else
						{
							Children[i].Measure(availableSize);
						}

						maxHeight = Math.Max(maxHeight, Children[i].DesiredSize.Height);
					}
				}
				// TODO: What do we want to do if there's unequal children in the DataTable vs. DataRow?
			}

			// Otherwise, return our parent's size as the desired size.
			return new(_parentPanel?.DesiredSize.Width ?? availableSize.Width, maxHeight);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			int column = 0;
			double x = 0;

			// Try and grab Column Spacing from DataTable, if not a parent Grid, if not 0.
			double spacing = _parentTable?.ColumnSpacing ?? (_parentPanel as Grid)?.ColumnSpacing ?? 0;

			double width = 0;

			if (_parentPanel != null)
			{
				int i = 0;
				foreach (UIElement child in Children.Where(static e => e.Visibility == Visibility.Visible))
				{
					if (_parentPanel is Grid grid &&
						column < grid.ColumnDefinitions.Count)
					{
						width = grid.ColumnDefinitions[column++].ActualWidth;
					}
					// TODO: Need to check Column visibility here as well...
					else if (_parentPanel is DataTable table &&
						column < table.Children.Count)
					{
						// TODO: This is messy...
						width = (table.Children[column++] as DataColumn)?.ActualWidth ?? 0;
					}

					// Note: For Auto, since we measured our children and bubbled that up to the DataTable layout, then the DataColumn size we grab above should account for the largest of our children.
					if (i == 0)
					{
						child.Arrange(new Rect(x, 0, width, finalSize.Height));
					}
					else
					{
						// If we're in a tree, remove the indentation from the layout of columns beyond the first.
						child.Arrange(new Rect(x - _treePadding, 0, width, finalSize.Height));
					}

					x += width + spacing;
					i++;
				}

				return new Size(x - spacing, finalSize.Height);
			}

			return finalSize;
		}
	}
}
