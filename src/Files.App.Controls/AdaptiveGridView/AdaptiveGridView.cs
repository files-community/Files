// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;

namespace Files.App.Controls
{
	/// <summary>
	/// The AdaptiveGridView control allows to present information within a Grid View perfectly adjusting the
	/// total display available space. It reacts to changes in the layout as well as the content so it can adapt
	/// to different form factors automatically.
	/// </summary>
	/// <remarks>
	/// The number and the width of items are calculated based on the
	/// screen resolution in order to fully leverage the available screen space. The property ItemsHeight define
	/// the items fixed height and the property DesiredWidth sets the minimum width for the elements to add a
	/// new column.</remarks>
	public partial class AdaptiveGridView : GridView
	{
		private bool _isLoaded;
		private ScrollMode _savedVerticalScrollMode;
		private ScrollMode _savedHorizontalScrollMode;
		private ScrollBarVisibility _savedVerticalScrollBarVisibility;
		private ScrollBarVisibility _savedHorizontalScrollBarVisibility;
		private Orientation _savedOrientation;
		private bool _needToRestoreScrollStates;
		private bool _needContainerMarginForLayout;

		/// <summary>
		/// Initializes a new instance of the <see cref="AdaptiveGridView"/> class.
		/// </summary>
		public AdaptiveGridView()
		{
			IsTabStop = false;
			SizeChanged += OnSizeChanged;
			ItemClick += OnItemClick;
			Items.VectorChanged += ItemsOnVectorChanged;
			Loaded += OnLoaded;
			Unloaded += OnUnloaded;

			// Prevent issues with higher DPIs and underlying panel. #1803
			UseLayoutRounding = false;
		}

		/// <summary>
		/// Prepares the specified element to display the specified item.
		/// </summary>
		/// <param name="obj">The element that's used to display the specified item.</param>
		/// <param name="item">The item to display.</param>
		protected override void PrepareContainerForItemOverride(DependencyObject obj, object item)
		{
			base.PrepareContainerForItemOverride(obj, item);
			if (obj is FrameworkElement element)
			{
				var heightBinding = new Binding()
				{
					Source = this,
					Path = new PropertyPath("ItemHeight"),
					Mode = BindingMode.TwoWay
				};

				var widthBinding = new Binding()
				{
					Source = this,
					Path = new PropertyPath("ItemWidth"),
					Mode = BindingMode.TwoWay
				};

				element.SetBinding(HeightProperty, heightBinding);
				element.SetBinding(WidthProperty, widthBinding);
			}

			if (obj is ContentControl contentControl)
			{
				contentControl.HorizontalContentAlignment = HorizontalAlignment.Stretch;
				contentControl.VerticalContentAlignment = VerticalAlignment.Stretch;
			}

			if (_needContainerMarginForLayout)
			{
				_needContainerMarginForLayout = false;
				RecalculateLayout(ActualWidth);
			}
		}

		/// <summary>
		/// Calculates the width of the grid items.
		/// </summary>
		/// <param name="containerWidth">The width of the container control.</param>
		/// <returns>The calculated item width.</returns>
		protected virtual double CalculateItemWidth(double containerWidth)
		{
			if (double.IsNaN(DesiredWidth))
			{
				return DesiredWidth;
			}

			var columns = CalculateColumns(containerWidth, DesiredWidth);

			// If there's less items than there's columns, reduce the column count (if requested);
			if (Items != null && Items.Count > 0 && Items.Count < columns && StretchContentForSingleRow)
			{
				columns = Items.Count;
			}

			// subtract the margin from the width so we place the correct width for placement
			var fallbackThickness = default(Thickness);
			var itemMargin = AdaptiveHeightValueConverter.GetItemMargin(this, fallbackThickness);
			if (itemMargin == fallbackThickness)
			{
				// No style explicitly defined, or no items or no container for the items
				// We need to get an actual margin for proper layout
				_needContainerMarginForLayout = true;
			}

			return (containerWidth / columns) - itemMargin.Left - itemMargin.Right;
		}

		/// <summary>
		/// Invoked whenever application code or internal processes (such as a rebuilding layout pass) call
		/// ApplyTemplate. In simplest terms, this means the method is called just before a UI element displays
		/// in your app. Override this method to influence the default post-template logic of a class.
		/// </summary>
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			OnOneRowModeEnabledChanged(this, OneRowModeEnabled);
		}

		private void ItemsOnVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
		{
			if (!double.IsNaN(ActualWidth))
			{
				// If the item count changes, check if more or less columns needs to be rendered,
				// in case we were having fewer items than columns.
				RecalculateLayout(ActualWidth);
			}
		}

		private void OnItemClick(object sender, ItemClickEventArgs e)
		{
			var cmd = ItemClickCommand;
			if (cmd != null)
			{
				if (cmd.CanExecute(e.ClickedItem))
				{
					cmd.Execute(e.ClickedItem);
				}
			}
		}

		private void OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			// If we are in center alignment, we only care about relayout if the number of columns we can display changes
			// Fixes #1737
			if (HorizontalAlignment != HorizontalAlignment.Stretch)
			{
				var prevColumns = CalculateColumns(e.PreviousSize.Width, DesiredWidth);
				var newColumns = CalculateColumns(e.NewSize.Width, DesiredWidth);

				// If the width of the internal list view changes, check if more or less columns needs to be rendered.
				if (prevColumns != newColumns)
				{
					RecalculateLayout(e.NewSize.Width);
				}
			}
			else if (e.PreviousSize.Width != e.NewSize.Width)
			{
				// We need to recalculate width as our size changes to adjust internal items.
				RecalculateLayout(e.NewSize.Width);
			}
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			_isLoaded = true;
			DetermineOneRowMode();
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			_isLoaded = false;
		}

		private void DetermineOneRowMode()
		{
			if (_isLoaded)
			{
				var itemsWrapGridPanel = ItemsPanelRoot as ItemsWrapGrid;

				if (OneRowModeEnabled)
				{
					var b = new Binding()
					{
						Source = this,
						Path = new PropertyPath("ItemHeight"),
						Converter = new AdaptiveHeightValueConverter(),
						ConverterParameter = this
					};

					if (itemsWrapGridPanel != null)
					{
						_savedOrientation = itemsWrapGridPanel.Orientation;
						itemsWrapGridPanel.Orientation = Orientation.Vertical;
					}

					SetBinding(MaxHeightProperty, b);

					_savedHorizontalScrollMode = ScrollViewer.GetHorizontalScrollMode(this);
					_savedVerticalScrollMode = ScrollViewer.GetVerticalScrollMode(this);
					_savedHorizontalScrollBarVisibility = ScrollViewer.GetHorizontalScrollBarVisibility(this);
					_savedVerticalScrollBarVisibility = ScrollViewer.GetVerticalScrollBarVisibility(this);
					_needToRestoreScrollStates = true;

					ScrollViewer.SetVerticalScrollMode(this, ScrollMode.Disabled);
					ScrollViewer.SetVerticalScrollBarVisibility(this, ScrollBarVisibility.Hidden);
					ScrollViewer.SetHorizontalScrollBarVisibility(this, ScrollBarVisibility.Visible);
					ScrollViewer.SetHorizontalScrollMode(this, ScrollMode.Enabled);
				}
				else
				{
					ClearValue(MaxHeightProperty);

					if (!_needToRestoreScrollStates)
					{
						return;
					}

					_needToRestoreScrollStates = false;

					if (itemsWrapGridPanel != null)
					{
						itemsWrapGridPanel.Orientation = _savedOrientation;
					}

					ScrollViewer.SetVerticalScrollMode(this, _savedVerticalScrollMode);
					ScrollViewer.SetVerticalScrollBarVisibility(this, _savedVerticalScrollBarVisibility);
					ScrollViewer.SetHorizontalScrollBarVisibility(this, _savedHorizontalScrollBarVisibility);
					ScrollViewer.SetHorizontalScrollMode(this, _savedHorizontalScrollMode);
				}
			}
		}

		private void RecalculateLayout(double containerWidth)
		{
			var itemsPanel = ItemsPanelRoot as Panel;
			var panelMargin = itemsPanel != null ?
							  itemsPanel.Margin.Left + itemsPanel.Margin.Right :
							  0;
			var padding = Padding.Left + Padding.Right;
			var border = BorderThickness.Left + BorderThickness.Right;

			// width should be the displayable width
			containerWidth = containerWidth - padding - panelMargin - border;
			if (containerWidth > 0)
			{
				var newWidth = CalculateItemWidth(containerWidth);
				ItemWidth = Math.Floor(newWidth);
			}
		}
	}
}
