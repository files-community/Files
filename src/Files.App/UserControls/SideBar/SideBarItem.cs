// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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
			CanDrag = true;
			DragStarting += SidebarItem_DragStarting;

			Loaded += SidebarItem_Loaded;
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new SidebarItemAutomationPeer(this);
		}

		internal void Select()
		{
			Owner.SelectedItem = (INavigationControlItem)Owner.DataContext;
		}

		private void SidebarItem_Loaded(object sender, RoutedEventArgs e)
		{
			HookupOwners();
			HookupIconChangeListener(null, Item);

			if (GetTemplateChild("ElementGrid") is Grid grid)
			{
				grid.PointerEntered += ItemGrid_PointerEntered;
				grid.PointerExited += ItemGrid_PointerExited;
				grid.PointerCanceled += ItemGrid_PointerCanceled;
				grid.PointerPressed += ItemGrid_PointerPressed;
				grid.ContextRequested += ItemGrid_ContextRequested;
				grid.DragLeave += ItemGrid_DragLeave;
				grid.DragOver += ItemGrid_DragOver;
				grid.GotFocus += ItemGrid_GotFocus;
				grid.LostFocus += ItemGrid_LostFocus;
				grid.Drop += ItemGrid_Drop;
				grid.AllowDrop = true;
				grid.IsTabStop = true;
			}

			if (GetTemplateChild("ChildrenPresenter") is ItemsRepeater repeater)
			{
				repeater.ElementPrepared += ChildrenPresenter_ElementPrepared;
			}
			if (GetTemplateChild("FlyoutChildrenPresenter") is ItemsRepeater flyoutRepeater)
			{
				flyoutRepeater.ElementPrepared += ChildrenPresenter_ElementPrepared;
			}

			UpdateExpansionState();
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

		private void HookupIconChangeListener(INavigationControlItem? oldItem, INavigationControlItem? newItem)
		{
			if (oldItem != null)
			{
				oldItem.PropertyChanged -= ItemPropertyChangedHandler;
				if (oldItem.ChildItems is not null)
					oldItem.ChildItems.CollectionChanged -= ChildItems_CollectionChanged;
			}
			if (newItem != null)
			{
				newItem.PropertyChanged += ItemPropertyChangedHandler;
				if (newItem.ChildItems is not null)
					newItem.ChildItems.CollectionChanged += ChildItems_CollectionChanged;
			}
			UpdateIcon();
		}

		private void SidebarItem_DragStarting(UIElement sender, DragStartingEventArgs args)
		{
			args.Data.SetData(StandardDataFormats.Text, this.DataContext.ToString());
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
			if (args.PropertyName == "Icon")
			{
				UpdateIcon();
			}
		}

		private void ReevaluateSelection()
		{
			if (!HasChildren)
			{
				IsSelected = DataContext == Owner.SelectedItem;
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

		private void SidebarDisplayModeChanged(SidebarDisplayMode displayMode)
		{
			switch (displayMode)
			{
				case SidebarDisplayMode.Expanded:
					UpdateExpansionState();
					UpdateSelectionState();
					SetFlyoutOpen(false);
					break;
				case SidebarDisplayMode.Minimal:
					UpdateExpansionState();
					SetFlyoutOpen(false);
					break;
				case SidebarDisplayMode.Compact:
					UpdateExpansionState();
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
		}

		private void UpdateIcon()
		{
			Icon = Item.GenerateIconSource()?.CreateIconElement();
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
				return IsSelected || HasChildSelection;
			}
		}

		private void UpdateExpansionState()
		{
			if (!HasChildren || !CollapseEnabled)
			{
				VisualStateManager.GoToState(this, "NoExpansion", true);
			}
			else
			{
				VisualStateManager.GoToState(this, IsExpanded ? "Expanded" : "Collapsed", true);
				VisualStateManager.GoToState(this, IsExpanded ? "ExpandedIconNormal" : "CollapsedIconNormal", true);
			}
			UpdateSelectionState();
		}

		private void ItemGrid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, IsSelected ? "PointerOverSelected" : "PointerOver", true); ;
			isPointerOver = true;
		}

		private void ItemGrid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, IsSelected ? "NormalSelected" : "Normal", true);
			isPointerOver = false;
		}

		private void ItemGrid_PointerCanceled(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, IsSelected ? "NormalSelected" : "Normal", true);
		}

		private void ItemGrid_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, IsSelected ? "PressedSelected" : "Pressed", true);
			VisualStateManager.GoToState(this, IsExpanded ? "ExpandedIconPressed" : "CollapsedIconPressed", true);
		}

		private void Item_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			e.Handled = true;
			if (isPointerOver)
			{
				VisualStateManager.GoToState(this, IsSelected ? "PointerOverSelected" : "PointerOver", true); ;
			}
			else
			{
				VisualStateManager.GoToState(this, IsSelected ? "NormalSelected" : "Normal", true);
			}
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

			e.AcceptedOperation = DataPackageOperation.Move;

			if (UseReorderDrop)
			{
				if (DragTargetAboveCenter(e))
				{
					VisualStateManager.GoToState(this, "DragInsertAbove", true);
				}
				else
				{
					VisualStateManager.GoToState(this, "DragInsertBelow", true);
				}
			}
			else
			{
				VisualStateManager.GoToState(this, "DragOnTop", true);
			}
		}

		private void ItemGrid_LostFocus(object sender, RoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, "Unfocused", false);
		}

		private void ItemGrid_GotFocus(object sender, RoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, "Focused", false);
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
			Owner.RaiseItemDropped(this, e, DragTargetAboveCenter(e), e);
		}

		private bool DragTargetAboveCenter(DragEventArgs args)
		{
			if (GetTemplateChild("ElementGrid") is Grid grid)
			{
				var position = args.GetPosition(grid);
				return position.Y < grid.ActualHeight / 2;
			}
			return false;
		}
	}
}
