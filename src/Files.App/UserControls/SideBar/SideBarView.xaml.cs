// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace Files.App.UserControls.SideBar
{
	public record ItemDroppedEventArgs(object DropTarget, DataPackageView DroppedItem, bool InsertAbove, DragEventArgs RawEvent) { }
	public record ItemContextInvokedArgs(object Item, Point Position) { }


	[ContentProperty(Name = "InnerContent")]
	public sealed partial class SideBarView : UserControl
	{

		private double preManipulationSideBarWidth = 0;
		private const double COMPACT_MAX_WIDTH = 200;

		public event EventHandler<ItemDroppedEventArgs>? ItemDropped;
		public event EventHandler<object>? ItemInvoked;
		public event EventHandler<ItemContextInvokedArgs>? ItemContextInvoked;

		public SideBarView()
		{
			InitializeComponent();
		}

		internal void RaiseItemInvoked(SideBarItem item)
		{
			// Only leaves can be selected
			if (item.HasChildren) return;
			SelectedItem = (item.DataContext as INavigationControlItem)!;
			ItemInvoked?.Invoke(item, item.DataContext);
			ViewModel.HandleItemInvoked(item.DataContext);
		}

		internal void RaiseContextRequested(SideBarItem item, Point e)
		{
			ItemContextInvoked?.Invoke(item, new ItemContextInvokedArgs(item.DataContext, e));
			ViewModel.HandleItemContextInvoked(item, new ItemContextInvokedArgs(item.DataContext, e));
		}

		internal void RaiseItemDropped(SideBarItem sideBarItem, DragEventArgs e, bool insertsAbove, DragEventArgs rawEvent)
		{
			ItemDropped?.Invoke(sideBarItem, new ItemDroppedEventArgs(sideBarItem.DataContext, e.DataView, insertsAbove, rawEvent));
			ViewModel.HandleItemDropped(new ItemDroppedEventArgs(sideBarItem.DataContext, e.DataView, insertsAbove, rawEvent));
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
					IsPaneOpen = false;
					UpdateMinimalMode();
					return;
			}
		}

		private void SideBarView_SizeChanged(object sender, SizeChangedEventArgs args)
		{
			UpdateDisplayModeForSideBarWidth(args.NewSize.Width);
		}

		private void SideBarView_Loaded(object sender, RoutedEventArgs e)
		{
			UpdateDisplayModeForSideBarWidth(ActualWidth);
		}

		private void UpdateMinimalMode()
		{
			if (DisplayMode != SideBarDisplayMode.Minimal) return;

			if (IsPaneOpen)
			{
				VisualStateManager.GoToState(this, "MinimalExpanded", false);
			}
			else
			{
				VisualStateManager.GoToState(this, "MinimalCollapsed", false);
			}
		}


		private void GridSplitter_ManipulationStarted(object sender, Microsoft.UI.Xaml.Input.ManipulationStartedRoutedEventArgs e)
		{
			preManipulationSideBarWidth = MenuItemsHost.ActualWidth;
		}

		private void GridSplitter_ManipulationDelta(object sender, Microsoft.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
		{
			var newWidth = preManipulationSideBarWidth + e.Cumulative.Translation.X;
			UpdateDisplayModeForPaneWidth(newWidth);
		}

		private void UpdateDisplayModeForSideBarWidth(double newControlWidth)
		{
			if (newControlWidth < 650)
			{
				DisplayMode = SideBarDisplayMode.Minimal;
			}
			else if (newControlWidth < 1300)
			{
				DisplayMode = SideBarDisplayMode.Compact;
			}
			else
			{
				DisplayMode = SideBarDisplayMode.Expanded;
			}
		}

		private void UpdateDisplayModeForPaneWidth(double newPaneWidth)
		{
			if (newPaneWidth < COMPACT_MAX_WIDTH)
			{
				DisplayMode = SideBarDisplayMode.Compact;
			}
			else if (newPaneWidth > COMPACT_MAX_WIDTH)
			{
				DisplayMode = SideBarDisplayMode.Expanded;
				DisplayColumn.Width = new GridLength(newPaneWidth);
			}
		}

		private void PaneLightDismissLayer_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			IsPaneOpen = false;
		}

		private void PaneLightDismissLayer_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			IsPaneOpen = false;
		}
	}
}
