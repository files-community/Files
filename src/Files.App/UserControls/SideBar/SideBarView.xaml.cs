// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace Files.App.UserControls.SideBar
{
	public record ItemDroppedEventArgs(object DropTarget, DataPackageView DroppedItem, bool InsertAbove, DragEventArgs RawEvent) { }
	public record ItemContextInvokedArgs(object Item, Point Position) { }


	[ContentProperty(Name = "InnerContent")]
	public sealed partial class SideBarView : UserControl, INotifyPropertyChanged
	{
		private bool canOpenInNewPane;

		public bool CanOpenInNewPane
		{
			get => canOpenInNewPane;
			set
			{
				if (value != canOpenInNewPane)
				{
					canOpenInNewPane = value;
					NotifyPropertyChanged(nameof(CanOpenInNewPane));
				}
			}
		}

		private double preManipulationSideBarWidth = 0;
		private const double COMPACT_MAX_WIDTH = 200;

		public event EventHandler<ItemDroppedEventArgs>? ItemDropped;
		public event EventHandler<object>? ItemInvoked;
		public event EventHandler<ItemContextInvokedArgs>? ItemContextInvoked;

		public SideBarView()
		{
			InitializeComponent();
		}


		public event PropertyChangedEventHandler? PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void RaiseItemInvoked(SideBarItem item)
		{
			// Only leaves can be selected
			if (item.HasChildren) return;
			SelectedItem = (item.DataContext as INavigationControlItem)!;
			ItemInvoked?.Invoke(item, item.DataContext);
			ViewModel.HandleItemInvoked(item.DataContext);
		}

		public void RaiseContextRequested(SideBarItem item, Point e)
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

		private void SideBar_ItemContextInvoked(object sender, ItemContextInvokedArgs args)
		{
			ViewModel.HandleItemContextInvoked(sender, args);
		}

		private void SideBarView_SizeChanged(object sender, SizeChangedEventArgs args)
		{
			if (args.NewSize.Width < 650)
			{
				DisplayMode = SideBarDisplayMode.Minimal;
			}
			else if (args.NewSize.Width < 1300)
			{
				DisplayMode = SideBarDisplayMode.Compact;
			}
			else
			{
				DisplayMode = SideBarDisplayMode.Expanded;
			}
		}

		private void TogglePaneButton_Click(object sender, RoutedEventArgs e)
		{
			if (DisplayMode == SideBarDisplayMode.Minimal)
			{
				IsPaneOpen = !IsPaneOpen;
			}
		}

		private async void SideBar_ItemDropped(object sender, ItemDroppedEventArgs e)
		{
			ViewModel?.HandleItemDropped(e);
		}

		private void SideBarView_Loaded(object sender, RoutedEventArgs e)
		{
			(this.FindDescendant("TabContentBorder") as Border)!.Child = TabContent;
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
			IsPaneOpen = false;
		}

		private void PaneLightDismissLayer_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			IsPaneOpen = false;
		}
	}
}
