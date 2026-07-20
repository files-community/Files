// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using System.Collections.Specialized;
using System.IO;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Files.App.Controls
{
	public sealed partial class SidebarItem : Control
	{
		private const double DROP_REPOSITION_THRESHOLD = 0.2; // Percentage of top/bottom at which we consider a drop to be a reposition/insertion

		public bool HasChildren => (Item?.Children is IList enumerable && enumerable.Count > 0) || (Item?.HasUnrealizedChildren ?? false);
		public bool IsGroupHeader => Item?.Children is not null;
		public bool CollapseEnabled => DisplayMode != SidebarDisplayMode.Compact;

		private bool hasChildSelection => selectedChildItem != null;
		private bool isPointerOver = false;
		private bool isClicking = false;
		private object? selectedChildItem = null;
		private ISidebarItemModel? lastSubscriber;
		// Owner DisplayMode callback runs once per container, gated by isWiredUp. Template-child handlers (ElementBorder pointer events etc.) run once per template application, gated by isTemplateWired — they can't share the gate because Loaded can fire on a Visibility=Collapsed container before OnApplyTemplate has supplied any template children to hook up.
		private bool isWiredUp;
		private bool isTemplateWired;
		private DispatcherQueueTimer? dragOverTimer;
		private DispatcherQueueTimer? dragOverExpandTimer;

		public SidebarItem()
		{
			DefaultStyleKey = typeof(SidebarItem);

			PointerReleased += Item_PointerReleased;
			KeyDown += (sender, args) =>
			{
				switch (args.Key)
				{
					case Windows.System.VirtualKey.Enter:
						Clicked(PointerUpdateKind.Other);
						args.Handled = true;
						break;
					case Windows.System.VirtualKey.Right when HasChildren && CollapseEnabled && !IsExpanded:
						IsExpanded = true;
						args.Handled = true;
						break;
					case Windows.System.VirtualKey.Left when HasChildren && CollapseEnabled && IsExpanded:
						IsExpanded = false;
						args.Handled = true;
						break;
				}
			};
			DragStarting += SidebarItem_DragStarting;

			Loaded += SidebarItem_Loaded;
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new SidebarItemAutomationPeer(this);
		}

		// Template-tied work needs to run *here* (not in Loaded) because Loaded can fire while the control is still not measured; template parts may not exist yet. Sub-rows realized later would otherwise keep isWiredUp=true with no handlers attached.
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			if (!isTemplateWired)
			{
				isTemplateWired = true;
				if (GetTemplateChild("ElementBorder") is Border border)
				{
					border.PointerEntered += ItemBorder_PointerEntered;
					border.PointerExited += ItemBorder_PointerExited;
					border.PointerCanceled += ItemBorder_PointerCanceled;
					border.PointerPressed += ItemBorder_PointerPressed;
					border.ContextRequested += ItemBorder_ContextRequested;
					border.DoubleTapped += ItemBorder_DoubleTapped;
					border.DragLeave += ItemBorder_DragLeave;
					border.DragOver += ItemBorder_DragOver;
					border.Drop += ItemBorder_Drop;
					border.AllowDrop = true;
					border.IsTabStop = false;
				}
				if (GetTemplateChild("ChevronContainer") is Border chevronContainer)
					chevronContainer.PointerPressed += ChevronContainer_PointerPressed;
				if (GetTemplateChild("FlyoutChildrenPresenter") is ItemsRepeater flyoutRepeater)
					flyoutRepeater.ElementPrepared += FlyoutChildrenPresenter_ElementPrepared;
			}

			if (Owner is null)
				return;
			VisualStateManager.GoToState(this, Owner.SupportsExpansion ? "OwnerSupportsExpansion" : "OwnerDoesNotSupportExpansion", false);
			// Flyout items inherit DisplayMode=Compact from the parent SidebarView but render full-size inside the overlay; they must NOT enter the Compact visual state or their text gets hidden. This matches the !IsInFlyout guard in SidebarDisplayModeChanged.
			if (!IsInFlyout)
				VisualStateManager.GoToState(this, DisplayMode == SidebarDisplayMode.Compact ? "Compact" : "NonCompact", false);
			UpdateExpansionState();
		}

		internal void Select()
		{
			if (Owner is not null)
				Owner.SelectedItem = Item!;
		}

		private void SidebarItem_Loaded(object sender, RoutedEventArgs e)
		{
			// Loaded fires every time ItemsRepeater recycles the container; only the per-row HandleItemChange runs each time.
			if (!isWiredUp)
			{
				HookupOwners();
				// HookupOwners can leave Owner null for static SidebarItems whose FindAscendant walk fires before they're parented into a SidebarView (rare). Leave isWiredUp=false so the next Loaded retries.
				if (Owner is not null)
					isWiredUp = true;
			}
			HandleItemChange();
		}

		public void HandleItemChange()
		{
			HookupItemChangeListener(null, Item);
			UpdateExpansionState();
			ReevaluateSelection();
			CanDrag = Item?.Path is string path && Path.IsPathRooted(path);
		}

		private void HookupOwners()
		{
			// Owner is pushed in by the hosting SidebarView's MenuItemsHost_ElementPrepared (top-level rows) or the parent SidebarItem's FlyoutChildrenPresenter_ElementPrepared (flyout children) before Loaded fires. Static SidebarItems declared directly in XAML (MainPage's SettingsButton in SidebarView.Footer) aren't realized through either path, so resolve Owner via a visual-tree walk for them. OwnerExpansionSupport state is applied by OnOwnerChanged.
			if (Owner is null)
				Owner = this.FindAscendant<SidebarView>();
			if (Owner is null)
				return;

			Owner.RegisterPropertyChangedCallback(SidebarView.DisplayModeProperty, (sender, args) =>
			{
				DisplayMode = Owner.DisplayMode;
			});
			DisplayMode = Owner.DisplayMode;
			// Setting the DP above only fires SidebarDisplayModeChanged (which calls GoToState) when the value actually changes from the default — sub-rows realized after Compact→Expanded never trigger it because both default and new value are Expanded. Force the state transition. Flyout items are skipped (same as in SidebarDisplayModeChanged) so they don't enter Compact and hide their text.
			if (!IsInFlyout)
				VisualStateManager.GoToState(this, DisplayMode == SidebarDisplayMode.Compact ? "Compact" : "NonCompact", false);

			// Static SidebarItems (MainPage's SettingsButton inside SidebarView.Footer) sit outside MenuItemsHost, so SidebarView.OnSelectedItemChanged's broadcast can't reach them. The per-row callback fills that gap.
			Owner.RegisterPropertyChangedCallback(SidebarView.SelectedItemProperty, (sender, args) =>
			{
				ReevaluateSelection();
			});
		}

		private void HookupItemChangeListener(ISidebarItemModel? oldItem, ISidebarItemModel? newItem)
		{
			if (lastSubscriber != null)
			{
				if (lastSubscriber.Children is INotifyCollectionChanged observableCollection)
					observableCollection.CollectionChanged -= ChildItems_CollectionChanged;
				lastSubscriber.PropertyChanged -= Item_PropertyChanged;
			}

			if (oldItem != null)
			{
				if (oldItem.Children is INotifyCollectionChanged observableCollection)
					observableCollection.CollectionChanged -= ChildItems_CollectionChanged;
				oldItem.PropertyChanged -= Item_PropertyChanged;
			}
			if (newItem != null)
			{
				lastSubscriber = newItem;
				if (newItem.Children is INotifyCollectionChanged observableCollection)
					observableCollection.CollectionChanged += ChildItems_CollectionChanged;
				newItem.PropertyChanged += Item_PropertyChanged;
			}
		}

		private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(ISidebarItemModel.HasUnrealizedChildren):
				case nameof(ISidebarItemModel.IsLeafWithChildren):
				case nameof(ISidebarItemModel.Children):
					UpdateExpansionState();
					ReevaluateSelection();
					break;
			}
		}

		private void SidebarItem_DragStarting(UIElement sender, DragStartingEventArgs args)
		{
			if (Item?.Path is not string dragPath || !Path.IsPathRooted(dragPath))
				return;

			args.Data.SetData(StandardDataFormats.Text, dragPath);
			args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
			args.Data.SetDataProvider(StandardDataFormats.StorageItems, async request =>
			{
				var deferral = request.GetDeferral();
				try
				{
					if (Directory.Exists(dragPath))
					{
						var folder = await StorageFolder.GetFolderFromPathAsync(dragPath);
						request.SetData(new IStorageItem[] { folder });
					}
				}
				catch
				{
				}
				finally
				{
					deferral.Complete();
				}
			});
		}

		private void SetFlyoutOpen(bool isOpen = true)
		{
			if (Item?.Children is null) return;

			var flyoutOwner = (GetTemplateChild("ElementGrid") as FrameworkElement)!;
			try
			{
				if (isOpen)
				{
					FlyoutBase.ShowAttachedFlyout(flyoutOwner);
				}
				else
				{
					FlyoutBase.GetAttachedFlyout(flyoutOwner).Hide();
				}
			}
			// ArgumentException when GetAttachedFlyout/ShowAttachedFlyout runs before the template is applied (e.g. DisplayMode toggled via ToggleSidebarAction at startup, before all containers have realized).
			catch (ArgumentException) { }
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

		// Entry point for SidebarView's SelectedItem PropertyChangedCallback to broadcast selection changes to every realized row, bypassing the per-row RegisterPropertyChangedCallback (which only attaches after Loaded).
		internal void ReevaluateSelectionFromOwner() => ReevaluateSelection();

		private void ReevaluateSelection()
		{
			// Leaves-with-children (tree-view folder rows) can be selected themselves as well as host a selected descendant.
			var isLeafWithChildren = Item?.IsLeafWithChildren == true;
			var selected = Owner?.SelectedItem;
			if (!IsGroupHeader || isLeafWithChildren)
			{
				// Item-null guard avoids the null==null match that paints cleared/recycled containers as selected when SelectedItem is also null (e.g. after collapsing the section that held the active path).
				IsSelected = Item is not null && Item == selected;
				if (IsSelected)
				{
					Owner?.UpdateSelectedItemContainer(this);
				}
			}
			else
			{
				// Recycled container previously bound to a selected leaf carries IsSelected=true into its new section-header binding; left unset, the header paints selected alongside the actual selected row after Compact↔overlay flips rebuild the flat list.
				IsSelected = false;
			}
			if (IsGroupHeader && Item?.Children is IList list && selected is not null && list.Contains(selected))
			{
				selectedChildItem = selected;
				SetFlyoutOpen(false);
			}
			else
			{
				selectedChildItem = null;
			}
			UpdateSelectionState();
		}

		// Flyout items live outside the flat list and need their selection state mirrored here so the realized row matches what the inline row would render.
		private void FlyoutChildrenPresenter_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
		{
			if (args.Element is SidebarItem item && Item?.Children is IList enumerable)
			{
				// Inherit the owning SidebarView so the flyout row's click routes to the correct view's RaiseItemInvoked instead of falling through to a FindAscendant walk that — inside a popup-hosted ItemsRepeater — can resolve to the wrong SidebarView entirely.
				item.Owner = Owner;
				var newElement = enumerable[args.Index];
				item.IsSelected = newElement == selectedChildItem;
				item.HandleItemChange();
			}
		}

		internal void Clicked(PointerUpdateKind pointerUpdateKind)
		{
			// Section headers (Pinned, Drives, ...) toggle expansion on row click since they have no navigation target. Tree-view folder rows (leaves-with-children) only navigate — their expansion is reserved for the chevron click target.
			if (IsGroupHeader && Item?.IsLeafWithChildren != true)
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

		// Chevron press: suppress the bubbling press; otherwise ElementBorder treats the chevron click as a row click and raises ItemInvoked.
		private void ChevronContainer_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
			=> e.Handled = TryToggleExpansion();

		private void ItemBorder_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
		{
			// Group-header rows already toggle on every PointerReleased via Clicked, so a DoubleTapped toggle stacks on top of the per-click toggles and lands the section in the opposite state (the rapid-click stutter). Leaves-with-children navigate on row click and only toggle via the chevron, so double-tap on the row is still the expected toggle path there.
			if (IsGroupHeader && Item?.IsLeafWithChildren != true)
			{
				e.Handled = true;
				return;
			}
			e.Handled = TryToggleExpansion();
		}

		private bool TryToggleExpansion()
		{
			if (!HasChildren || !CollapseEnabled)
				return false;
			IsExpanded = !IsExpanded;
			return true;
		}

		internal void RaiseItemInvoked(PointerUpdateKind pointerUpdateKind)
		{
			Owner?.RaiseItemInvoked(this, pointerUpdateKind);
		}

		private void SidebarDisplayModeChanged(SidebarDisplayMode oldValue)
		{
			switch (DisplayMode)
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
				VisualStateManager.GoToState(this, DisplayMode == SidebarDisplayMode.Compact ? "Compact" : "NonCompact", false);
				ReapplyOwnerExpansionState();
			}
		}

		private void ReapplyOwnerExpansionState()
		{
			if (Owner is null || Owner.SupportsExpansion)
				return;
			VisualStateManager.GoToState(this, "OwnerSupportsExpansion", false);
			VisualStateManager.GoToState(this, "OwnerDoesNotSupportExpansion", false);
		}

		private void UpdateSelectionState()
		{
			// Containers re-bind constantly during fast scroll; play state changes without transitions so no implicit animations fire on each ItemsRepeater realization.
			VisualStateManager.GoToState(this, ShouldShowSelectionIndicator() ? "Selected" : "Unselected", false);
			UpdatePointerState();
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
				VisualStateManager.GoToState(this, useSelectedState ? "PressedSelected" : "Pressed", false);
			}
			else if (isPointerOver)
			{
				VisualStateManager.GoToState(this, useSelectedState ? "PointerOverSelected" : "PointerOver", false);
			}
			else
			{
				VisualStateManager.GoToState(this, useSelectedState ? "NormalSelected" : "Normal", false);
			}
		}

		private void UpdateExpansionState()
		{
			if (Owner?.SupportsExpansion == false)
			{
				VisualStateManager.GoToState(this, "NoExpansion", false);
				UpdateSelectionState();
				return;
			}

			if (Item?.Children is null || !CollapseEnabled)
			{
				VisualStateManager.GoToState(this, "NoExpansion", false);
			}
			else if (!HasChildren)
			{
				// Empty folder leaves render like normal leaves; empty group headers keep the section-heading style.
				VisualStateManager.GoToState(this, Item?.IsLeafWithChildren == true ? "NoExpansion" : "NoChildren", false);
			}
			else
			{
				VisualStateManager.GoToState(this, Item?.IsLeafWithChildren == true ? "LeafWithChildren" : (IsExpanded ? "Expanded" : "Collapsed"), false);
				VisualStateManager.GoToState(this, IsExpanded ? "ExpandedIconNormal" : "CollapsedIconNormal", false);
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

			Owner?.RaiseItemDragOver(this, insertsAbove, e);

			var openDelay = Owner?.HoverToOpenDelay ?? TimeSpan.Zero;
			var expandDelay = Owner?.HoverToExpandDelay ?? TimeSpan.Zero;
			var isCenter = insertsAbove == SidebarItemDropPosition.Center;
			var canHoverOpen = openDelay > TimeSpan.Zero && isCenter && Item is not null && (!IsGroupHeader || Item.IsLeafWithChildren);
			var canHoverExpand = expandDelay > TimeSpan.Zero && isCenter && HasChildren && CollapseEnabled;
			if (canHoverExpand)
			{
				dragOverExpandTimer ??= DispatcherQueue.CreateTimer();
				dragOverExpandTimer.Debounce(
					() =>
					{
						dragOverExpandTimer!.Stop();
						IsExpanded = true;
					},
					expandDelay,
					false);
			}
			else
			{
				dragOverExpandTimer?.Stop();
			}
			if (canHoverOpen)
			{
				dragOverTimer ??= DispatcherQueue.CreateTimer();
				dragOverTimer.Debounce(
					() =>
					{
						dragOverTimer!.Stop();
						RaiseItemInvoked(PointerUpdateKind.Other);
					},
					openDelay,
					false);
			}
			else
			{
				dragOverTimer?.Stop();
			}
		}

		private void ItemBorder_ContextRequested(UIElement sender, Microsoft.UI.Xaml.Input.ContextRequestedEventArgs args)
		{
			Owner?.RaiseContextRequested(this, args.TryGetPosition(this, out var point) ? point : default);
			args.Handled = true;
		}

		private void ItemBorder_DragLeave(object sender, DragEventArgs e)
		{
			dragOverTimer?.Stop();
			dragOverExpandTimer?.Stop();
			UpdatePointerState();
		}

		private void ItemBorder_Drop(object sender, DragEventArgs e)
		{
			dragOverTimer?.Stop();
			dragOverExpandTimer?.Stop();
			UpdatePointerState();
			Owner?.RaiseItemDropped(this, DetermineDropTargetPosition(e), e);
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
