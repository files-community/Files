// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using System.Collections.Specialized;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Controls
{
	public sealed partial class SidebarItem : Control
	{
		private const double DROP_REPOSITION_THRESHOLD = 0.2; // Percentage of top/bottom at which we consider a drop to be a reposition/insertion

		public bool HasChildren => Item?.Children is IList enumerable && enumerable.Count > 0;
		public bool IsGroupHeader => Item?.Children is not null;
		public bool CollapseEnabled => DisplayMode != SidebarDisplayMode.Compact;

		private bool hasChildSelection => selectedChildItem != null;
		private bool isPointerOver = false;
		private bool isClicking = false;
		private object? selectedChildItem = null;
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
					Clicked(PointerUpdateKind.Other);
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
			if (Owner is not null)
				Owner.SelectedItem = Item!;
		}

		private void SidebarItem_Loaded(object sender, RoutedEventArgs e)
		{
			HookupOwners();

			if (GetTemplateChild("ElementBorder") is Border border)
			{
				border.PointerEntered += ItemBorder_PointerEntered;
				border.PointerExited += ItemBorder_PointerExited;
				border.PointerCanceled += ItemBorder_PointerCanceled;
				border.PointerPressed += ItemBorder_PointerPressed;
				border.ContextRequested += ItemBorder_ContextRequested;
				border.DragLeave += ItemBorder_DragLeave;
				border.DragOver += ItemBorder_DragOver;
				border.Drop += ItemBorder_Drop;
				border.AllowDrop = true;
				border.IsTabStop = false;
			}

			if (GetTemplateChild("ChildrenPresenter") is ItemsRepeater repeater)
			{
				childrenRepeater = repeater;
				repeater.ElementPrepared += ChildrenPresenter_ElementPrepared;
			}
			if (GetTemplateChild("FlyoutChildrenPresenter") is ItemsRepeater flyoutRepeater)
			{
				flyoutRepeater.ElementPrepared += ChildrenPresenter_ElementPrepared;
			}

			HandleItemChange();
		}

		public void HandleItemChange()
		{
			HookupItemChangeListener(null, Item);
			UpdateExpansionState();
			ReevaluateSelection();

			if (Item is not null)
				Decorator = Item.ItemDecorator;
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

		private void HookupItemChangeListener(ISidebarItemModel? oldItem, ISidebarItemModel? newItem)
		{
			if (lastSubscriber != null)
			{
				lastSubscriber.PropertyChanged -= ItemPropertyChangedHandler;
				if (lastSubscriber.Children is INotifyCollectionChanged observableCollection)
					observableCollection.CollectionChanged -= ChildItems_CollectionChanged;
			}

			if (oldItem != null)
			{
				oldItem.PropertyChanged -= ItemPropertyChangedHandler;
				if (oldItem.Children is INotifyCollectionChanged observableCollection)
					observableCollection.CollectionChanged -= ChildItems_CollectionChanged;
			}
			if (newItem != null)
			{
				newItem.PropertyChanged += ItemPropertyChangedHandler;
				lastSubscriber = newItem;
				if (newItem.Children is INotifyCollectionChanged observableCollection)
					observableCollection.CollectionChanged += ChildItems_CollectionChanged;
			}
			UpdateIcon();
		}

		private void SidebarItem_DragStarting(UIElement sender, DragStartingEventArgs args)
		{
			args.Data.SetData(StandardDataFormats.Text, Item!.Text.ToString());
		}

		private void SetFlyoutOpen(bool isOpen = true)
		{
			if (Item?.Children is null) return;

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
			if (DisplayMode == SidebarDisplayMode.Compact && !HasChildren)
			{
				SetFlyoutOpen(false);
			}
		}

		void ItemPropertyChangedHandler(object? sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == nameof(ISidebarItemModel.IconSource))
			{
				UpdateIcon();
			}
		}

		private void ReevaluateSelection()
		{
			if (!IsGroupHeader)
			{
				IsSelected = Item == Owner?.SelectedItem;
				if (IsSelected)
				{
					Owner?.UpdateSelectedItemContainer(this);
				}
			}
			else if (Item?.Children is IList list)
			{
				if (list.Contains(Owner?.SelectedItem))
				{
					selectedChildItem = Owner?.SelectedItem;
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
				if (Item?.Children is IList enumerable)
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
					item.HandleItemChange();
				}
			}
		}

		internal void Clicked(PointerUpdateKind pointerUpdateKind)
		{
			if (IsGroupHeader)
			{
				if (CollapseEnabled)
				{
					IsExpanded = !IsExpanded;
				}
				else if (HasChildren)
				{
					SetFlyoutOpen(true);
				}
			}
			RaiseItemInvoked(pointerUpdateKind);
		}

		internal void RaiseItemInvoked(PointerUpdateKind pointerUpdateKind)
		{
			Owner?.RaiseItemInvoked(this, pointerUpdateKind);
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
		}

		private bool ShouldShowSelectionIndicator()
		{
			if (IsExpanded && CollapseEnabled)
			{
				return IsSelected;
			}
			else
			{
				return IsSelected || hasChildSelection;
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
			if (Item?.Children is null || !CollapseEnabled)
			{
				VisualStateManager.GoToState(this, Item?.PaddedItem == true ? "NoExpansionWithPadding" : "NoExpansion", useAnimations);
			}
			else if (!HasChildren)
			{
				VisualStateManager.GoToState(this, "NoChildren", useAnimations);
			}
			else
			{
				if (Item?.Children is IList enumerable && enumerable.Count > 0 && childrenRepeater is not null)
				{
					var firstChild = childrenRepeater.GetOrCreateElement(0);

					// Collapsed elements might have a desired size of 0 so we need to have a sensible fallback
					var childHeight = firstChild.DesiredSize.Height > 0 ? firstChild.DesiredSize.Height : 32;
					ChildrenPresenterHeight = enumerable.Count * childHeight;
				}
				VisualStateManager.GoToState(this, IsExpanded ? "Expanded" : "Collapsed", useAnimations);
				VisualStateManager.GoToState(this, IsExpanded ? "ExpandedIconNormal" : "CollapsedIconNormal", useAnimations);
			}
			UpdateSelectionState();
		}

		private void ItemBorder_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			isPointerOver = true;
			UpdatePointerState();
		}

		private void ItemBorder_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			isPointerOver = false;
			isClicking = false;
			UpdatePointerState();
		}

		private void ItemBorder_PointerCanceled(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			isClicking = false;
			UpdatePointerState();
		}

		private void ItemBorder_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			isClicking = true;
			UpdatePointerState(true);
			VisualStateManager.GoToState(this, IsExpanded ? "ExpandedIconPressed" : "CollapsedIconPressed", true);
		}

		private void Item_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			if (!isClicking)
				return;

			isClicking = false;
			e.Handled = true;
			UpdatePointerState();

			VisualStateManager.GoToState(this, IsExpanded ? "ExpandedIconNormal" : "CollapsedIconNormal", true);
			var pointerUpdateKind = e.GetCurrentPoint(null).Properties.PointerUpdateKind;
			if (pointerUpdateKind == PointerUpdateKind.LeftButtonReleased ||
				pointerUpdateKind == PointerUpdateKind.MiddleButtonReleased)
			{
				Clicked(pointerUpdateKind);
			}
		}

		private async void ItemBorder_DragOver(object sender, DragEventArgs e)
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

			if (Owner is not null)
			{
				var deferral = e.GetDeferral();
				await Owner.RaiseItemDragOver(this, insertsAbove, e);
				deferral.Complete();
			}
		}

		private void ItemBorder_ContextRequested(UIElement sender, Microsoft.UI.Xaml.Input.ContextRequestedEventArgs args)
		{
			Owner?.RaiseContextRequested(this, args.TryGetPosition(this, out var point) ? point : default);
			args.Handled = true;
		}

		private void ItemBorder_DragLeave(object sender, DragEventArgs e)
		{
			UpdatePointerState();
		}

		private async void ItemBorder_Drop(object sender, DragEventArgs e)
		{
			UpdatePointerState();
			if (Owner is not null)
				await Owner.RaiseItemDropped(this, DetermineDropTargetPosition(e), e);
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
