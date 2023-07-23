using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace Files.App.UserControls.SideBar
{
	public record ItemDroppedEventArgs(object DropTarget, DataPackageView DroppedItem, bool InsertAbove) { }
	public record ItemContextInvokedArgs(object Item, Point Position) { }
	
	
	[ContentProperty(Name = "InnerContent")]
	public sealed partial class SideBarPane : UserControl
	{
		private const double COMPACT_MAX_WIDTH = 200;

		private double preManipulationSideBarWidth = 0;

		public event EventHandler<ItemDroppedEventArgs>? ItemDropped;
		
		public event EventHandler<object>? ItemInvoked;

		public event EventHandler<ItemContextInvokedArgs>? ItemContextInvoked;

		public SideBarPane()
		{
			this.InitializeComponent();
			Loaded += SideBar_Loaded;
		}

		private void SideBar_Loaded(object sender, RoutedEventArgs e)
		{
			if (DisplayMode != SideBarDisplayMode.Minimal)
			{
				UpdateDisplayModeForWidth(MenuItemsHost.ActualWidth);
			}
			else
			{
				UpdateDisplayMode();
			}
		}

		public void RaiseContextRequested(SideBarItem item, Point e)
		{
			// Only leaves can be selected
			ItemContextInvoked?.Invoke(item, new ItemContextInvokedArgs(item.DataContext, e));
		}

		public void RaiseItemInvoked(SideBarItem item)
		{
			// Only leaves can be selected
			if (item.Item?.ChildItems is not null) return;
			SelectedItem = item.DataContext;
			ItemInvoked?.Invoke(item, SelectedItem);
		}

		private void UpdateDisplayMode()
		{
			switch (DisplayMode)
			{
				case SideBarDisplayMode.Compact:
					VisualStateManager.GoToState(this, "Compact", false);
					return;
				case SideBarDisplayMode.Expanded:
					VisualStateManager.GoToState(this, "Expanded", false);
					return;
				case SideBarDisplayMode.Minimal:
					PaneExpanded = false;
					UpdateMinimalMode();
					return;
			}
		}

		private void UpdateMinimalMode()
		{
			if (DisplayMode != SideBarDisplayMode.Minimal) return;

			if (PaneExpanded)
			{
				VisualStateManager.GoToState(this, "MinimalExpanded", false);
			}
			else
			{
				VisualStateManager.GoToState(this, "MinimalCollapsed", false);
			}
		}

		internal void RaiseItemDropped(SideBarItem sideBarItem, DragEventArgs e, bool insertsAbove)
		{
			ItemDropped?.Invoke(this, new ItemDroppedEventArgs(sideBarItem.DataContext, e.DataView, insertsAbove));
		}

		private void GridSplitter_ManipulationStarted(object sender, Microsoft.UI.Xaml.Input.ManipulationStartedRoutedEventArgs e)
		{
			preManipulationSideBarWidth = MenuItemsHost.ActualWidth;
		}

		private void GridSplitter_ManipulationDelta(object sender, Microsoft.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
		{
			var newWidth = preManipulationSideBarWidth + e.Cumulative.Translation.X;
			UpdateDisplayModeForWidth(newWidth);
		}

		private void UpdateDisplayModeForWidth(double newWidth)
		{
			if (newWidth < COMPACT_MAX_WIDTH)
			{
				DisplayMode = SideBarDisplayMode.Compact;
			}
			else if (newWidth > COMPACT_MAX_WIDTH)
			{
				DisplayMode = SideBarDisplayMode.Expanded;
				DisplayColumn.Width = new GridLength(newWidth);
			}
		}

		private void PaneLightDismissLayer_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			PaneExpanded = false;
		}

		private void PaneLightDismissLayer_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			PaneExpanded = false;
		}
	}
}
