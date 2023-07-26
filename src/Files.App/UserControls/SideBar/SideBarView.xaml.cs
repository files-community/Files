// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace Files.App.UserControls.Sidebar
{
	public record ItemDroppedEventArgs(object DropTarget, DataPackageView DroppedItem, bool InsertAbove, DragEventArgs RawEvent) { }
	public record ItemContextInvokedArgs(object Item, Point Position) { }


	[ContentProperty(Name = "InnerContent")]
	public sealed partial class SidebarView : UserControl
	{

		private double preManipulationSidebarWidth = 0;

		private const double COMPACT_MAX_WIDTH = 200;

		internal SidebarItem? SelectedItemContainer = null;

		/// <summary>
		/// True if the user is currently resizing the Sidebar
		/// </summary>
		private bool draggingSidebarResizer;


		public event EventHandler<ItemDroppedEventArgs>? ItemDropped;
		public event EventHandler<object>? ItemInvoked;
		public event EventHandler<ItemContextInvokedArgs>? ItemContextInvoked;

		public SidebarView()
		{
			InitializeComponent();
		}

		private void UpdateMinimalMode()
		{
			if (DisplayMode != SidebarDisplayMode.Minimal) return;

			if (IsPaneOpen)
			{
				VisualStateManager.GoToState(this, "MinimalExpanded", false);
			}
			else
			{
				VisualStateManager.GoToState(this, "MinimalCollapsed", false);
			}
		}

		private void UpdateDisplayMode()
		{
			switch (DisplayMode)
			{
				case SidebarDisplayMode.Compact:
					VisualStateManager.GoToState(this, "Compact", false);
					return;
				case SidebarDisplayMode.Expanded:
					VisualStateManager.GoToState(this, "Expanded", false);
					return;
				case SidebarDisplayMode.Minimal:
					IsPaneOpen = false;
					UpdateMinimalMode();
					return;
			}
		}

		private void UpdateDisplayModeForSidebarWidth(double newControlWidth)
		{
			if (newControlWidth < 650)
			{
				DisplayMode = SidebarDisplayMode.Minimal;
			}
			else if (newControlWidth < 1300)
			{
				DisplayMode = SidebarDisplayMode.Compact;
			}
			else
			{
				DisplayMode = SidebarDisplayMode.Expanded;
			}
		}

		private void UpdateDisplayModeForPaneWidth(double newPaneWidth)
		{
			if (newPaneWidth < COMPACT_MAX_WIDTH)
			{
				DisplayMode = SidebarDisplayMode.Compact;
			}
			else if (newPaneWidth > COMPACT_MAX_WIDTH)
			{
				DisplayMode = SidebarDisplayMode.Expanded;
				DisplayColumn.Width = new GridLength(newPaneWidth);
			}
		}

		internal void UpdateSelectedItemContainer(SidebarItem container)
		{
			SelectedItemContainer = container;
		}

		internal void RaiseItemInvoked(SidebarItem item)
		{
			// Only leaves can be selected
			if (item.HasChildren) return;
			SelectedItem = (item.DataContext as INavigationControlItem)!;
			ItemInvoked?.Invoke(item, item.DataContext);
			ViewModel.HandleItemInvoked(item.DataContext);
		}

		internal void RaiseContextRequested(SidebarItem item, Point e)
		{
			ItemContextInvoked?.Invoke(item, new ItemContextInvokedArgs(item.DataContext, e));
			ViewModel.HandleItemContextInvoked(item, new ItemContextInvokedArgs(item.DataContext, e));
		}

		internal void RaiseItemDropped(SidebarItem sideBarItem, DragEventArgs e, bool insertsAbove, DragEventArgs rawEvent)
		{
			ItemDropped?.Invoke(sideBarItem, new ItemDroppedEventArgs(sideBarItem.DataContext, e.DataView, insertsAbove, rawEvent));
			ViewModel.HandleItemDropped(new ItemDroppedEventArgs(sideBarItem.DataContext, e.DataView, insertsAbove, rawEvent));
		}


		private void SidebarView_SizeChanged(object sender, SizeChangedEventArgs args)
		{
			UpdateDisplayModeForSidebarWidth(args.NewSize.Width);
		}

		private void SidebarView_Loaded(object sender, RoutedEventArgs e)
		{
			UpdateDisplayModeForSidebarWidth(ActualWidth);
		}


		private void GridSplitter_ManipulationStarted(object sender, Microsoft.UI.Xaml.Input.ManipulationStartedRoutedEventArgs e)
		{
			draggingSidebarResizer = true;
			preManipulationSidebarWidth = MenuItemsHost.ActualWidth;
		}

		private void GridSplitter_ManipulationDelta(object sender, Microsoft.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
		{
			var newWidth = preManipulationSidebarWidth + e.Cumulative.Translation.X;
			UpdateDisplayModeForPaneWidth(newWidth);
		}


		private void PaneLightDismissLayer_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			IsPaneOpen = false;
		}

		private void PaneLightDismissLayer_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			IsPaneOpen = false;
		}

		private void SidebarResizer_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
		{
			DisplayMode = DisplayMode == SidebarDisplayMode.Expanded ? SidebarDisplayMode.Compact : SidebarDisplayMode.Expanded;
		}

		private void SidebarResizer_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			var sidebarResizer = (GridSplitter)sender;
			sidebarResizer.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
		}

		private void SidebarResizer_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			if (draggingSidebarResizer)
				return;

			var sidebarResizer = (GridSplitter)sender;
			sidebarResizer.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
		}

		private void SidebarResizer_ManipulationCompleted(object sender, Microsoft.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e)
		{
			draggingSidebarResizer = false;
		}
	}
}
