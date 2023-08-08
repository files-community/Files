// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using SQLitePCL;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.UserControls.Sidebar
{
	public sealed partial class SidebarItem : Control
	{
		private bool isPointerOver = false;
		private object? selectedChildItem = null;

		public bool HasChildren => Item?.ChildItems is not null && Item.ChildItems.Count > 0;
		public bool CollapseEnabled => DisplayMode != SidebarDisplayMode.Compact;
		private bool HasChildSelection => selectedChildItem != null;
		private const double DROP_REPOSITION_THRESHOLD = 0.2; // Percentage of top/bottom at which we consider a drop to be a reposition/insertion
		private ItemsRepeater? childrenRepeater;

		private ISidebarItemModel? lastSubscriber;

		public SidebarItem()
		{
			DefaultStyleKey = typeof(SidebarItem);

			PointerReleased += Item_PointerReleased;
			KeyDown += (sender, args) =>
			{
				if (args.Key == Windows.System.VirtualKey.Enter)
				{
					Clicked();
					args.Handled = true;
				}
			};
			DragStarting += SidebarItem_DragStarting;

			Loaded += SidebarItem_Loaded;
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new SidebarItemAutomationPeer(this);
		}

		internal void Select()
		{
			Owner.SelectedItem = Item!;
		}

		private void SidebarItem_Loaded(object sender, RoutedEventArgs e)
		{
			HookupOwners();
			HookupItenChangeListener(null, Item);

			if (GetTemplateChild("ElementGrid") is Grid grid)
			{
				grid.PointerEntered += ItemGrid_PointerEntered;
				grid.PointerExited += ItemGrid_PointerExited;
				grid.PointerCanceled += ItemGrid_PointerCanceled;
				grid.PointerPressed += ItemGrid_PointerPressed;
				grid.ContextRequested += ItemGrid_ContextRequested;
				grid.DragLeave += ItemGrid_DragLeave;
				grid.DragOver += ItemGrid_DragOver;
				grid.Drop += ItemGrid_Drop;
				grid.AllowDrop = true;
				grid.IsTabStop = true;
			}

			if (GetTemplateChild("ChildrenPresenter") is ItemsRepeater repeater)
			{
				childrenRepeater = repeater;
				repeater.ElementPrepared += ChildrenPresenter_ElementPrepared;
				repeater.SizeChanged += ChildrenPresenter_SizeChanged;
			}
			if (GetTemplateChild("FlyoutChildrenPresenter") is ItemsRepeater flyoutRepeater)
			{
				flyoutRepeater.ElementPrepared += ChildrenPresenter_ElementPrepared;
			}
			HookupItenChangeListener(null, Item);
			UpdateExpansionState();
		}

		private void ChildrenPresenter_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			ChildrenPresenterHeight = e.NewSize.Height;
		}

		private void HookupOwners()
		{
			FrameworkElement resolvingTarget = this;
			if (GetTemplateRoot(Parent) is FrameworkElement element)
			{
				resolvingTarget = element;
			}
			Owner = resolvingTarget.FindAscendant<SidebarView>()!;

			Owner.RegisterPropertyChangedCallback(SidebarView.DisplayModeProperty, (sender, args) =>
			{
				DisplayMode = Owner.DisplayMode;
			});
			DisplayMode = Owner.DisplayMode;

			Owner.RegisterPropertyChangedCallback(SidebarView.SelectedItemProperty, (sender, args) =>
			{
				ReevaluateSelection();
			});
			ReevaluateSelection();
		}

		private void HookupItenChangeListener(ISidebarItemModel? oldItem, ISidebarItemModel? newItem)
		{
			if (lastSubscriber != null)
			{
				lastSubscriber.PropertyChanged -= ItemPropertyChangedHandler;
				if (lastSubscriber.ChildItems is not null)
					lastSubscriber.ChildItems.CollectionChanged -= ChildItems_CollectionChanged;
				Debug.WriteLine($"[{System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}] ** ** ** UN-Subscribed to property changed for {lastSubscriber.Text}");
			}

			if (oldItem != null)
			{
				Debug.WriteLine($"[{System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}] ** ** ** UN-Subscribed to property changed for {oldItem.Text}");
				oldItem.PropertyChanged -= ItemPropertyChangedHandler;
				if (oldItem.ChildItems is not null)
					oldItem.ChildItems.CollectionChanged -= ChildItems_CollectionChanged;
			}
			if (newItem != null)
			{
				Debug.WriteLine($"[{System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}] ** ** ** Subscribed to property changed for {newItem.Text}");

				newItem.PropertyChanged += ItemPropertyChangedHandler;
				lastSubscriber = newItem;
				if (newItem.ChildItems is not null)
					newItem.ChildItems.CollectionChanged += ChildItems_CollectionChanged;
			}
			UpdateIcon();
		}

		private void SidebarItem_DragStarting(UIElement sender, DragStartingEventArgs args)
		{
			args.Data.SetData(StandardDataFormats.Text, Item!.Text.ToString());
		}

		private void SetFlyoutOpen(bool isOpen = true)
		{
			if (Item?.ChildItems is null) return;

			var flyoutOwner = (GetTemplateChild("ElementGrid") as FrameworkElement)!;
			if (isOpen)
			{
				FlyoutBase.ShowAttachedFlyout(flyoutOwner);
			}
			else
			{
				FlyoutBase.GetAttachedFlyout(flyoutOwner).Hide();
			}
		}

		private void ChildItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			ReevaluateSelection();
			UpdateExpansionState();
		}

		void ItemPropertyChangedHandler(object? sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == nameof(ISidebarItemModel.IconSource))
			{
				Debug.WriteLine($"[{System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}] ** ** ** Icon changed for {Item?.Text}");
				UpdateIcon();
			}
		}

		private void ReevaluateSelection()
		{
			if (!HasChildren)
			{
				IsSelected = Item == Owner.SelectedItem;
				if (IsSelected)
				{
					Owner.UpdateSelectedItemContainer(this);
				}
			}
			else if (Item?.ChildItems is IList list)
			{
				if (list.Contains(Owner.SelectedItem))
				{
					selectedChildItem = Owner.SelectedItem;
					SetFlyoutOpen(false);
				}
				else
				{
					selectedChildItem = null;
				}
				UpdateSelectionState();
			}
		}

		private void ChildrenPresenter_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
		{
			if (args.Element is SidebarItem item)
			{
				if (Item?.ChildItems is IList enumerable)
				{
					var newElement = enumerable[args.Index];
					if (newElement == selectedChildItem)
					{
						(args.Element as SidebarItem)!.IsSelected = true;
					}
					else
					{
						(args.Element as SidebarItem)!.IsSelected = false;
					}
					item.UpdateIcon();
				}
			}
		}

		internal void Clicked()
		{
			if (HasChildren)
			{
				if (CollapseEnabled)
				{
					IsExpanded = !IsExpanded;
				}
				else
				{
					SetFlyoutOpen(true);
				}
			}
			RaiseItemInvoked();
		}

		internal void RaiseItemInvoked()
		{
			Owner?.RaiseItemInvoked(this);
		}

		private void SidebarDisplayModeChanged(SidebarDisplayMode oldValue)
		{
			var useAnimations = oldValue != SidebarDisplayMode.Minimal;
			switch (DisplayMode)
			{
				case SidebarDisplayMode.Expanded:
					UpdateExpansionState(useAnimations);
					UpdateSelectionState();
					SetFlyoutOpen(false);
					break;
				case SidebarDisplayMode.Minimal:
					UpdateExpansionState(useAnimations);
					SetFlyoutOpen(false);
					break;
				case SidebarDisplayMode.Compact:
					UpdateExpansionState(useAnimations);
					UpdateSelectionState();
					break;
			}
			if (!IsInFlyout)
			{
				VisualStateManager.GoToState(this, DisplayMode == SidebarDisplayMode.Compact ? "Compact" : "NonCompact", true);
			}
		}

		private void UpdateSelectionState()
		{
			VisualStateManager.GoToState(this, ShouldShowSelectionIndicator() ? "Selected" : "Unselected", true);
			UpdatePointerState();
		}

		private void UpdateIcon()
		{
			Icon = Item?.IconSource?.CreateIconElement();
			if (Icon is not null)
				AutomationProperties.SetAccessibilityView(Icon, AccessibilityView.Raw);
			Debug.WriteLine($"[{System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}] ** ** ** Updated icon for {Item?.Text} with icon being {(Icon != null ? "not null" : "null")}");
		}

		private bool ShouldShowSelectionIndicator()
		{
			if (IsExpanded && CollapseEnabled)
			{
				return IsSelected;
			}
			else
			{
				return IsSelected || HasChildSelection;
			}
		}

		private void UpdatePointerState(bool isPointerDown = false)
		{
			var useSelectedState = ShouldShowSelectionIndicator();
			if (isPointerDown)
			{
				VisualStateManager.GoToState(this, useSelectedState ? "PressedSelected" : "Pressed", true);
			}
			else if (isPointerOver)
			{
				VisualStateManager.GoToState(this, useSelectedState ? "PointerOverSelected" : "PointerOver", true);
			}
			else
			{
				VisualStateManager.GoToState(this, useSelectedState ? "NormalSelected" : "Normal", true);
			}
		}

		private void UpdateExpansionState(bool useAnimations = true)
		{
			if (!HasChildren || !CollapseEnabled)
			{
				VisualStateManager.GoToState(this, "NoExpansion", useAnimations);
			}
			else
			{
				if (childrenRepeater != null)
				{
					if (childrenRepeater.ActualHeight > ChildrenPresenterHeight)
					{
						ChildrenPresenterHeight = childrenRepeater.ActualHeight;
					}
				}
				VisualStateManager.GoToState(this, IsExpanded ? "Expanded" : "Collapsed", useAnimations);
				VisualStateManager.GoToState(this, IsExpanded ? "ExpandedIconNormal" : "CollapsedIconNormal", useAnimations);
			}
			UpdateSelectionState();
		}

		private void ItemGrid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			isPointerOver = true;
			UpdatePointerState();
		}

		private void ItemGrid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			isPointerOver = false;
			UpdatePointerState();
		}

		private void ItemGrid_PointerCanceled(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			UpdatePointerState();
		}

		private void ItemGrid_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			UpdatePointerState(true);
			VisualStateManager.GoToState(this, IsExpanded ? "ExpandedIconPressed" : "CollapsedIconPressed", true);
		}

		private void Item_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			e.Handled = true;
			UpdatePointerState();

			VisualStateManager.GoToState(this, IsExpanded ? "ExpandedIconNormal" : "CollapsedIconNormal", true);
			var updateKind = e.GetCurrentPoint(null).Properties.PointerUpdateKind;
			if (updateKind == PointerUpdateKind.LeftButtonReleased)
			{
				Clicked();
			}
		}

		private void ItemGrid_DragOver(object sender, DragEventArgs e)
		{
			if (HasChildren)
			{
				IsExpanded = true;
			}

			var insertsAbove = DetermineDropTargetPosition(e);
			if (insertsAbove == SidebarItemDropPosition.Center)
			{
				VisualStateManager.GoToState(this, "DragOnTop", true);
			}
			else if (insertsAbove == SidebarItemDropPosition.Top)
			{
				VisualStateManager.GoToState(this, "DragInsertAbove", true);
			}
			else if (insertsAbove == SidebarItemDropPosition.Bottom)
			{
				VisualStateManager.GoToState(this, "DragInsertBelow", true);
			}

			Owner.RaiseItemDragOver(this, insertsAbove, e);
		}

		private void ItemGrid_ContextRequested(UIElement sender, Microsoft.UI.Xaml.Input.ContextRequestedEventArgs args)
		{
			Owner.RaiseContextRequested(this, args.TryGetPosition(this, out var point) ? point : default);
			args.Handled = true;
		}

		private void ItemGrid_DragLeave(object sender, DragEventArgs e)
		{
			VisualStateManager.GoToState(this, "NoDrag", true);
		}

		private void ItemGrid_Drop(object sender, DragEventArgs e)
		{
			VisualStateManager.GoToState(this, "NoDrag", true);
			Owner.RaiseItemDropped(this, DetermineDropTargetPosition(e), e);
		}

		private SidebarItemDropPosition DetermineDropTargetPosition(DragEventArgs args)
		{
			if (UseReorderDrop)
			{
				if (GetTemplateChild("ElementGrid") is Grid grid)
				{
					var position = args.GetPosition(grid);
					if (position.Y < grid.ActualHeight * DROP_REPOSITION_THRESHOLD)
					{
						return SidebarItemDropPosition.Top;
					}
					if (position.Y > grid.ActualHeight * (1 - DROP_REPOSITION_THRESHOLD))
					{
						return SidebarItemDropPosition.Bottom;
					}
					return SidebarItemDropPosition.Center;
				}
			}
			return SidebarItemDropPosition.Center;
		}
	}
}
