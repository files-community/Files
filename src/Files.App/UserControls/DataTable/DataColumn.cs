using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Collections.Specialized;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.UserControls.DataTable
{
	public class DataColumn : ContentControl
	{
		private static GridLength StarLength = new GridLength(1, GridUnitType.Star);

		private ContentSizer? PART_ColumnSizer;

		private WeakReference<DataTable>? _parent;

		/// <summary>
		/// Gets or sets the width of the largest child contained within the visible <see cref="DataRow"/>s of the <see cref="DataTable"/>.
		/// </summary>
		internal double MaxChildDesiredWidth { get; set; }

		/// <summary>
		/// Gets or sets the internal copy of the <see cref="DesiredWidth"/> property to be used in calculations, this gets manipulated in Auto-Size mode.
		/// </summary>
		internal GridLength CurrentWidth { get; private set; }

		/// <summary>
		/// Gets or sets whether the column can be resized by the user.
		/// </summary>
		public bool CanResize
		{
			get { return (bool)GetValue(CanResizeProperty); }
			set { SetValue(CanResizeProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="CanResize"/> property.
		/// </summary>
		public static readonly DependencyProperty CanResizeProperty =
			DependencyProperty.Register("CanResize", typeof(bool), typeof(DataColumn), new PropertyMetadata(false));

		/// <summary>
		/// Gets or sets the desired width of the column upon initialization. Defaults to a <see cref="GridLength"/> of 1 <see cref="GridUnitType.Star"/>.
		/// </summary>
		public GridLength DesiredWidth
		{
			get { return (GridLength)GetValue(DesiredWidthProperty); }
			set { SetValue(DesiredWidthProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="DesiredWidth"/> property.
		/// </summary>
		public static readonly DependencyProperty DesiredWidthProperty =
			DependencyProperty.Register(nameof(DesiredWidth), typeof(GridLength), typeof(DataColumn), new PropertyMetadata(GridLength.Auto, DesiredWidth_PropertyChanged));

		private static void DesiredWidth_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			// If the developer updates the size of the column, update our internal copy
			if (d is DataColumn col)
			{
				col.CurrentWidth = col.DesiredWidth;
			}
		}

		public DataColumn()
		{
			this.DefaultStyleKey = typeof(DataColumn);
		}

		protected override void OnApplyTemplate()
		{
			if (PART_ColumnSizer != null)
			{
				PART_ColumnSizer.TargetControl = null;
				PART_ColumnSizer.ManipulationDelta -= this.PART_ColumnSizer_ManipulationDelta;
				PART_ColumnSizer.ManipulationCompleted -= this.PART_ColumnSizer_ManipulationCompleted;
			}

			PART_ColumnSizer = GetTemplateChild(nameof(PART_ColumnSizer)) as ContentSizer;

			if (PART_ColumnSizer != null)
			{
				PART_ColumnSizer.TargetControl = this;
				PART_ColumnSizer.ManipulationDelta += this.PART_ColumnSizer_ManipulationDelta;
				PART_ColumnSizer.ManipulationCompleted += this.PART_ColumnSizer_ManipulationCompleted;
			}

			// Get DataTable parent weak reference for when we manipulate columns.
			var parent = this.FindAscendant<DataTable>();
			if (parent != null)
			{
				_parent = new(parent);
			}

			base.OnApplyTemplate();
		}

		private void PART_ColumnSizer_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			ColumnResizedByUserSizer();
		}

		private void PART_ColumnSizer_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			ColumnResizedByUserSizer();
		}

		private void ColumnResizedByUserSizer()
		{
			// Update our internal representation to be our size now as a fixed value.
			CurrentWidth = new(this.ActualWidth);

			// Notify the rest of the table to update
			if (_parent?.TryGetTarget(out DataTable? parent) == true
				&& parent != null)
			{
				parent.ColumnResized();
			}
		}
	}
}
