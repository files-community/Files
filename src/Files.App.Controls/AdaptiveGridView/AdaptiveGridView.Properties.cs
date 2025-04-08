// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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
	/// new column.
	/// </remarks>
	public partial class AdaptiveGridView
	{
		/// <summary>
		/// Identifies the <see cref="ItemClickCommand"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ItemClickCommandProperty =
			DependencyProperty.Register(nameof(ItemClickCommand), typeof(ICommand), typeof(AdaptiveGridView), new PropertyMetadata(null));

		/// <summary>
		/// Identifies the <see cref="ItemHeight"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ItemHeightProperty =
			DependencyProperty.Register(nameof(ItemHeight), typeof(double), typeof(AdaptiveGridView), new PropertyMetadata(double.NaN));

		/// <summary>
		/// Identifies the <see cref="OneRowModeEnabled"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty OneRowModeEnabledProperty =
			DependencyProperty.Register(nameof(OneRowModeEnabled), typeof(bool), typeof(AdaptiveGridView), new PropertyMetadata(false, (o, e) => { OnOneRowModeEnabledChanged(o, e.NewValue); }));

		/// <summary>
		/// Identifies the <see cref="ItemWidth"/> dependency property.
		/// </summary>
		private static readonly DependencyProperty ItemWidthProperty =
			DependencyProperty.Register(nameof(ItemWidth), typeof(double), typeof(AdaptiveGridView), new PropertyMetadata(double.NaN));

		/// <summary>
		/// Identifies the <see cref="DesiredWidth"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty DesiredWidthProperty =
			DependencyProperty.Register(nameof(DesiredWidth), typeof(double), typeof(AdaptiveGridView), new PropertyMetadata(double.NaN, DesiredWidthChanged));

		/// <summary>
		/// Identifies the <see cref="StretchContentForSingleRow"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty StretchContentForSingleRowProperty =
		DependencyProperty.Register(nameof(StretchContentForSingleRow), typeof(bool), typeof(AdaptiveGridView), new PropertyMetadata(true, OnStretchContentForSingleRowPropertyChanged));

		private static void OnOneRowModeEnabledChanged(DependencyObject d, object newValue)
		{
			var self = d as AdaptiveGridView;
			self.DetermineOneRowMode();
		}

		private static void DesiredWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var self = d as AdaptiveGridView;
			self.RecalculateLayout(self.ActualWidth);
		}

		private static void OnStretchContentForSingleRowPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var self = d as AdaptiveGridView;
			self.RecalculateLayout(self.ActualWidth);
		}

		/// <summary>
		/// Gets or sets the desired width of each item
		/// </summary>
		/// <value>The width of the desired.</value>
		public double DesiredWidth
		{
			get { return (double)GetValue(DesiredWidthProperty); }
			set { SetValue(DesiredWidthProperty, value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the control should stretch the content to fill at least one row.
		/// </summary>
		/// <remarks>
		/// If set to <c>true</c> (default) and there is only one row of items, the items will be stretched to fill the complete row.
		/// If set to <c>false</c>, items will have their normal size, which means a gap can exist at the end of the row.
		/// </remarks>
		/// <value>A value indicating whether the control should stretch the content to fill at least one row.</value>
		public bool StretchContentForSingleRow
		{
			get { return (bool)GetValue(StretchContentForSingleRowProperty); }
			set { SetValue(StretchContentForSingleRowProperty, value); }
		}

		/// <summary>
		/// Gets or sets the command to execute when an item is clicked and the IsItemClickEnabled property is true.
		/// </summary>
		/// <value>The item click command.</value>
		public ICommand ItemClickCommand
		{
			get { return (ICommand)GetValue(ItemClickCommandProperty); }
			set { SetValue(ItemClickCommandProperty, value); }
		}

		/// <summary>
		/// Gets or sets the height of each item in the grid.
		/// </summary>
		/// <value>The height of the item.</value>
		public double ItemHeight
		{
			get { return (double)GetValue(ItemHeightProperty); }
			set { SetValue(ItemHeightProperty, value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether only one row should be displayed.
		/// </summary>
		/// <value><c>true</c> if only one row is displayed; otherwise, <c>false</c>.</value>
		public bool OneRowModeEnabled
		{
			get { return (bool)GetValue(OneRowModeEnabledProperty); }
			set { SetValue(OneRowModeEnabledProperty, value); }
		}

		/// <summary>
		/// Gets the template that defines the panel that controls the layout of items.
		/// </summary>
		/// <remarks>
		/// This property overrides the base ItemsPanel to prevent changing it.
		/// </remarks>
		/// <returns>
		/// An ItemsPanelTemplate that defines the panel to use for the layout of the items.
		/// The default value for the ItemsControl is an ItemsPanelTemplate that specifies
		/// a StackPanel.
		/// </returns>
		public new ItemsPanelTemplate ItemsPanel => base.ItemsPanel;

		private double ItemWidth
		{
			get { return (double)GetValue(ItemWidthProperty); }
			set { SetValue(ItemWidthProperty, value); }
		}

		private static int CalculateColumns(double containerWidth, double itemWidth)
		{
			var columns = itemWidth > 0 ? (int)Math.Round(containerWidth / itemWidth) : 0;
			if (columns == 0)
			{
				columns = 1;
			}

			return columns;
		}
	}
}
