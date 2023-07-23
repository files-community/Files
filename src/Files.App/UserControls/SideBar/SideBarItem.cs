

// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Drawing.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace Files.App.UserControls.SideBar
{
	public sealed partial class SideBarItem : Control
	{

		private bool isPointerOver = false;

		private object? selectedChildItem = null;

		public bool HasChildren => Item?.ChildItems is not null && Item.ChildItems.Count > 0;
		public bool CollapsableChildren => DisplayMode != SideBarDisplayMode.Compact;
		private bool HasChildSelection => selectedChildItem != null;

		public SideBarItem()
		{
			DefaultStyleKey = typeof(SideBarItem);

			PointerReleased += Item_PointerReleased;
			KeyDown += (sender, args) =>
			{
				args.Handled = true;
				if (args.Key == Windows.System.VirtualKey.Enter)
				{
					Clicked();
				}
			};
			CanDrag = true;
			DragStarting += SideBarItem_DragStarting;

			Loaded += SideBarItem_Loaded;
		}

		private void SideBarItem_DragStarting(UIElement sender, DragStartingEventArgs args)
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

		private void SideBarItem_Loaded(object sender, RoutedEventArgs e)
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
				grid.Drop += ItemGrid_Drop;
				grid.AllowDrop = true;
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

		private void ItemGrid_ContextRequested(UIElement sender, Microsoft.UI.Xaml.Input.ContextRequestedEventArgs args)
		{
			Owner.RaiseContextRequested(this, args.TryGetPosition(this, out var point) ? point : default);
			args.Handled = true;
		}

		private void HookupOwners()
		{
			FrameworkElement resolvingTarget = this;
			if (GetTemplateRoot(Parent) is FrameworkElement element)
			{
				resolvingTarget = element;
			}
			Owner = resolvingTarget.FindAscendant<SideBarPane>()!;

			Owner.RegisterPropertyChangedCallback(SideBarPane.DisplayModeProperty, (sender, args) =>
			{
				DisplayMode = Owner.DisplayMode;
			});
			Owner.RegisterPropertyChangedCallback(SideBarPane.SelectedItemProperty, (sender, args) =>
			{
				ReevaluateSelection();
			});
		}

		private void HookupIconChangeListener(INavigationControlItem? oldItem, INavigationControlItem? newItem)
		{
			if(oldItem != null)
			{
				oldItem.PropertyChanged -= ItemPropertyChangedHandler;
				if (oldItem.ChildItems is not null)
					oldItem.ChildItems.CollectionChanged -= ChildItems_CollectionChanged;
			}
			if (newItem != null)
			{
				newItem.PropertyChanged += ItemPropertyChangedHandler;
				if(newItem.ChildItems is not null)
					newItem.ChildItems.CollectionChanged += ChildItems_CollectionChanged;
			}
			UpdateIcon();
		}

		private void ChildItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			ReevaluateSelection();
			UpdateExpansionState();
		}

		void ItemPropertyChangedHandler(object? sender, PropertyChangedEventArgs args)
		{
			if(args.PropertyName == "Icon")
			{
				UpdateIcon();
			}
		}

		private void ReevaluateSelection()
		{
			if (!HasChildren)
			{
				IsSelected = DataContext == Owner.SelectedItem;
			}
			else if (Item?.ChildItems is IList list)
			{
				if (list.Contains(Owner.SelectedItem))
				{
					selectedChildItem = Owner.SelectedItem;
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
					(args.Element as SideBarItem)!.IsSelected = true;
				}
				else
				{
					(args.Element as SideBarItem)!.IsSelected = false;
				}
			}
		}

		private void Clicked()
		{
			if (HasChildren)
			{
				if (CollapsableChildren)
				{
					IsExpanded = !IsExpanded;
				}
				else
				{
					SetFlyoutOpen(true);
				}
			}
			Owner?.RaiseItemInvoked(this);
		}

		private void SideBarDisplayModeChanged(SideBarDisplayMode displayMode)
		{
			switch (displayMode)
			{
				case SideBarDisplayMode.Expanded:
					UpdateExpansionState();
					UpdateSelectionState();
					SetFlyoutOpen(false);
					break;
				case SideBarDisplayMode.Minimal:
					UpdateExpansionState();
					SetFlyoutOpen(false);
					break;
				case SideBarDisplayMode.Compact:
					UpdateExpansionState();
					UpdateSelectionState();
					break;
			}
		}

		private void UpdateSelectionState()
		{
			VisualStateManager.GoToState(this, ShouldShowSelectionIndicator() ? "Selected" : "Unselected", true);
		}

		private void UpdateIcon()
		{
			Icon = Item.GenerateIconSource()?.CreateIconElement();
		}

		private bool ShouldShowSelectionIndicator()
		{
			if (IsExpanded && CollapsableChildren)
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
			if (!HasChildren)
			{
				VisualStateManager.GoToState(this, "NoExpansion", true);
			}
			else
			{
				VisualStateManager.GoToState(this, IsExpanded ? "Expanded" : "Collapsed", true);
			}
			UpdateSelectionState();
		}

		private void ItemGrid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, "PointerOver", true);
			isPointerOver = true;
		}

		private void ItemGrid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, "Normal", true);
			isPointerOver = false;
		}

		private void ItemGrid_PointerCanceled(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, "Normal", true);
		}

		private void ItemGrid_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, "Pressed", true);
		}

		private void Item_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, isPointerOver ? "PointerOver" : "Normal", true);
			e.Handled = true;
			Clicked();
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

		private bool DragTargetAboveCenter(DragEventArgs args)
		{
			if (GetTemplateChild("ElementGrid") is Grid grid)
			{
				var position = args.GetPosition(grid);
				return position.Y < grid.ActualHeight / 2;
			}
			return false;
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
	}
}
