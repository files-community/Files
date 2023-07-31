// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI.Controls;
using Files.App.Services.Settings;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.UserControls.Sidebar
{
	[ContentProperty(Name = "InnerContent")]
	public sealed partial class SidebarView : UserControl
	{

		private double preManipulationSidebarWidth = 0;
		private bool useCompactInExpanded = false;

		private const double COMPACT_MAX_WIDTH = 200;

		internal SidebarItem? SelectedItemContainer = null;

		/// <summary>
		/// True if the user is currently resizing the Sidebar
		/// </summary>
		private bool draggingSidebarResizer;


		public event EventHandler<ItemDroppedEventArgs>? ItemDropped;
		public event EventHandler<ItemDragOverEventArgs>? ItemDragOver;
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
				VisualStateManager.GoToState(this, "MinimalExpanded", true);
			}
			else
			{
				VisualStateManager.GoToState(this, "MinimalCollapsed", true);
			}
		}

		private void UpdateDisplayMode()
		{
			switch (DisplayMode)
			{
				case SidebarDisplayMode.Compact:
					VisualStateManager.GoToState(this, "Compact", true);
					return;
				case SidebarDisplayMode.Expanded:
					VisualStateManager.GoToState(this, "Expanded", true);
					return;
				case SidebarDisplayMode.Minimal:
					IsPaneOpen = false;
					UpdateMinimalMode();
					return;
			}
		}

		private void UpdateDisplayModeForSidebarWidth(double newControlWidth)
		{
			if (newControlWidth <= 640)
			{
				DisplayMode = SidebarDisplayMode.Minimal;
			}
			else if (useCompactInExpanded)
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
				useCompactInExpanded = true;
			}
			else if (newPaneWidth > COMPACT_MAX_WIDTH)
			{
				DisplayMode = SidebarDisplayMode.Expanded;
				DisplayColumn.Width = new GridLength(newPaneWidth);
				useCompactInExpanded = false;
			}
		}

		internal void UpdateSelectedItemContainer(SidebarItem container)
		{
			SelectedItemContainer = container;
		}

		internal void RaiseItemInvoked(SidebarItem item)
		{
			// Only leaves can be selected
			if (item.Item is null || item.HasChildren) return;

			SelectedItem = item.Item;
			ItemInvoked?.Invoke(item, item.Item);
			ViewModel.HandleItemInvoked(item.Item);
		}

		internal void RaiseContextRequested(SidebarItem item, Point e)
		{
			ItemContextInvoked?.Invoke(item, new ItemContextInvokedArgs(item.Item, e));
			ViewModel.HandleItemContextInvoked(item, new ItemContextInvokedArgs(item.Item, e));
		}

		internal void RaiseItemDropped(SidebarItem sideBarItem, SidebarItemDropPosition dropPosition, DragEventArgs rawEvent)
		{
			if (sideBarItem.Item is null) return;
			ItemDropped?.Invoke(sideBarItem, new ItemDroppedEventArgs(sideBarItem.Item, rawEvent.DataView, dropPosition, rawEvent));
			ViewModel.HandleItemDropped(new ItemDroppedEventArgs(sideBarItem.Item, rawEvent.DataView, dropPosition, rawEvent));
		}

		internal void RaiseItemDragOver(SidebarItem sideBarItem, SidebarItemDropPosition dropPosition, DragEventArgs rawEvent)
		{
			if (sideBarItem.Item is null) return;
			ItemDragOver?.Invoke(sideBarItem, new ItemDragOverEventArgs(sideBarItem.Item, rawEvent.DataView, dropPosition, rawEvent));
			ViewModel.HandleItemDragOver(new ItemDragOverEventArgs(sideBarItem.Item, rawEvent.DataView, dropPosition, rawEvent));
		}

		private void SidebarView_SizeChanged(object sender, SizeChangedEventArgs args)
		{
			UpdateDisplayModeForSidebarWidth(args.NewSize.Width);
		}

		private void SidebarView_Loaded(object sender, RoutedEventArgs e)
		{
			UpdateDisplayModeForSidebarWidth(ActualWidth);
		}


		private void SidebarResizer_ManipulationStarted(object sender, Microsoft.UI.Xaml.Input.ManipulationStartedRoutedEventArgs e)
		{
			draggingSidebarResizer = true;
			preManipulationSidebarWidth = DisplayColumn.Width.Value;
			VisualStateManager.GoToState(this, "ResizerPressed", true);
		}

		private void SidebarResizer_ManipulationDelta(object sender, Microsoft.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
		{
			var newWidth = preManipulationSidebarWidth + e.Cumulative.Translation.X;
			UpdateDisplayModeForPaneWidth(newWidth);
		}

		private void SidebarResizerControl_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			var primaryInvocation = e.Key == VirtualKey.Space || e.Key == VirtualKey.Enter;
			if (DisplayMode == SidebarDisplayMode.Expanded)
			{
				if (primaryInvocation)
				{
					DisplayMode = SidebarDisplayMode.Compact;
					useCompactInExpanded = true;
					return;
				}

				var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
				var increment = ctrl.HasFlag(CoreVirtualKeyStates.Down) ? 5 : 1;

				// Left makes the pane smaller so we invert the increment
				if (e.Key == VirtualKey.Left)
				{
					increment = -increment;
				}
				var newWidth = DisplayColumn.Width.Value + increment;
				UpdateDisplayModeForPaneWidth(newWidth);
				return;
			}
			else if (DisplayMode == SidebarDisplayMode.Compact)
			{
				if (primaryInvocation || e.Key == VirtualKey.Right)
				{
					useCompactInExpanded = false;
					DisplayMode = SidebarDisplayMode.Expanded;
				}
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

		private void SidebarResizer_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
		{
			if (DisplayMode == SidebarDisplayMode.Expanded)
			{
				DisplayMode = SidebarDisplayMode.Compact;
				useCompactInExpanded = true;
			}
			else
			{
				DisplayMode = SidebarDisplayMode.Expanded;
				useCompactInExpanded = false;
			}
		}

		private void SidebarResizer_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			var sidebarResizer = (FrameworkElement)sender;
			sidebarResizer.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
			VisualStateManager.GoToState(this, "ResizerPointerOver", true);
		}

		private void SidebarResizer_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			if (draggingSidebarResizer)
				return;

			var sidebarResizer = (FrameworkElement)sender;
			sidebarResizer.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
			VisualStateManager.GoToState(this, "ResizerNormal", true);
		}

		private void SidebarResizer_ManipulationCompleted(object sender, Microsoft.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e)
		{
			draggingSidebarResizer = false;
			VisualStateManager.GoToState(this, "ResizerNormal", true);
		}

		private void PaneColumnGrid_ContextRequested(UIElement sender, Microsoft.UI.Xaml.Input.ContextRequestedEventArgs args)
		{
			var newArgs = new ItemContextInvokedArgs(null, args.TryGetPosition(this, out var point) ? point : default);
			ViewModel.HandleItemContextInvoked(this, newArgs);
			args.Handled = true;
		}
	}
}
