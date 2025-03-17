// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;

namespace Files.App.Controls
{
	[ContentProperty(Name = nameof(InnerContent))]
	// Template parts
	[TemplatePart(Name = TemplatePartName_PanePanel, Type = typeof(Grid))]
	[TemplatePart(Name = TemplatePartName_PaneSizer, Type = typeof(Border))]
	[TemplatePart(Name = TemplatePartName_PaneLightDismissLayer, Type = typeof(Border))]
	[TemplatePart(Name = TemplatePartName_MenuItemsItemsRepeater, Type = typeof(ItemsRepeater))]
	// Visual states
	[TemplateVisualState(GroupName = VisualStateGroupName_DisplayModes, Name = VisualStateName_Expanded)]
	[TemplateVisualState(GroupName = VisualStateGroupName_DisplayModes, Name = VisualStateName_Compact)]
	[TemplateVisualState(GroupName = VisualStateGroupName_DisplayModes, Name = VisualStateName_MinimalCollapsed)]
	[TemplateVisualState(GroupName = VisualStateGroupName_DisplayModes, Name = VisualStateName_MinimalExpanded)]
	[TemplateVisualState(GroupName = VisualStateGroupName_ResizerStates, Name = VisualStateName_ResizerNormal)]
	[TemplateVisualState(GroupName = VisualStateGroupName_ResizerStates, Name = VisualStateName_ResizerPointerOver)]
	[TemplateVisualState(GroupName = VisualStateGroupName_ResizerStates, Name = VisualStateName_ResizerPressed)]
	public partial class SidebarView : Control
	{
		// Constants

		private const double CompactModeThresholdWidth = 200d;

		private const string TemplatePartName_PanePanel = "PART_PanePanel";
		private const string TemplatePartName_PaneSizer = "PART_PaneResizer";
		private const string TemplatePartName_PaneLightDismissLayer = "PART_PaneLightDismissLayer";
		private const string TemplatePartName_MenuItemsItemsRepeater = "PART_MenuItemsItemsRepeater";

		private const string VisualStateGroupName_DisplayModes = "DisplayModes";
		private const string VisualStateGroupName_ResizerStates = "ResizerStates";

		private const string VisualStateName_Expanded = "Expanded";
		private const string VisualStateName_Compact = "Compact";
		private const string VisualStateName_MinimalCollapsed = "MinimalCollapsed";
		private const string VisualStateName_MinimalExpanded = "MinimalExpanded";

		private const string VisualStateName_ResizerNormal = "ResizerNormal";
		private const string VisualStateName_ResizerPointerOver = "ResizerPointerOver";
		private const string VisualStateName_ResizerPressed = "ResizerPressed";

		// Events

		public event EventHandler<object>? ItemInvoked;
		public event EventHandler<ItemContextInvokedArgs>? ItemContextInvoked;
		public event PropertyChangedEventHandler? PropertyChanged;

		// Fields

		private Grid? _panePanel;
		private Border? _paneSizer;
		private Border? _paneLightDismissLayer;
		private ItemsRepeater? _menuItemsItemsRepeater;

		internal SidebarItem? SelectedItemContainer = null;

		private bool _draggingSidebarResizer;
		private double _preManipulationPaneWidth = 0;

		// Constructor

		public SidebarView()
		{
			DefaultStyleKey = typeof(SidebarView);
		}

		// Methods

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// Get template parts
			_panePanel = GetTemplateChild(TemplatePartName_PanePanel) as Grid
				?? throw new MissingFieldException($"Could not find {TemplatePartName_PanePanel} in the given {nameof(SidebarView)}'s style.");
			_paneSizer = GetTemplateChild(TemplatePartName_PaneSizer) as Border
				?? throw new MissingFieldException($"Could not find {TemplatePartName_PaneSizer} in the given {nameof(SidebarView)}'s style.");
			_paneLightDismissLayer = GetTemplateChild(TemplatePartName_PaneLightDismissLayer) as Border
				?? throw new MissingFieldException($"Could not find {TemplatePartName_PaneLightDismissLayer} in the given {nameof(SidebarView)}'s style.");
			_menuItemsItemsRepeater = GetTemplateChild(TemplatePartName_MenuItemsItemsRepeater) as ItemsRepeater
				?? throw new MissingFieldException($"Could not find {TemplatePartName_MenuItemsItemsRepeater} in the given {nameof(SidebarView)}'s style.");

			// Subscribe events
			_paneSizer.DoubleTapped += SidebarResizer_DoubleTapped;
			_paneSizer.PointerEntered += SidebarResizer_PointerEntered;
			_paneSizer.PointerExited += SidebarResizer_PointerExited;
			_paneSizer.ManipulationStarted += SidebarResizer_ManipulationStarted;
			_paneSizer.ManipulationDelta += SidebarResizer_ManipulationDelta;
			_paneSizer.ManipulationCompleted += SidebarResizer_ManipulationCompleted;
			_paneSizer.KeyDown += SidebarResizer_KeyDown;
			_paneLightDismissLayer.PointerPressed += PaneLightDismissLayer_PointerPressed;
			_paneLightDismissLayer.Tapped += PaneLightDismissLayer_Tapped;
			_panePanel.ContextRequested += PanePanel_ContextRequested;
			_menuItemsItemsRepeater.ElementPrepared += MenuItemsItemsRepeater_ElementPrepared;

			// Initialize visuals
			UpdateDisplayMode();
			UpdateOpenPaneLength();
			_panePanel.Translation = new System.Numerics.Vector3(0, 0, 32);
		}

		internal void UpdateSelectedItemContainer(SidebarItem container)
		{
			SelectedItemContainer = container;
		}

		internal void RaiseItemInvoked(SidebarItem item, PointerUpdateKind pointerUpdateKind)
		{
			// Only leaves can be selected
			if (item.Item is null || item.IsGroupHeader) return;

			SelectedItem = item.Item;
			ItemInvoked?.Invoke(item, item.Item);
			ViewModel?.HandleItemInvokedAsync(item.Item, pointerUpdateKind);
		}

		internal void RaiseContextRequested(SidebarItem item, Point e)
		{
			ItemContextInvoked?.Invoke(item, new ItemContextInvokedArgs(item.Item, e));
			ViewModel?.HandleItemContextInvokedAsync(item, new ItemContextInvokedArgs(item.Item, e));
		}

		internal async Task RaiseItemDropped(SidebarItem sideBarItem, SidebarItemDropPosition dropPosition, DragEventArgs rawEvent)
		{
			if (sideBarItem.Item is null || ViewModel is null) return;
			await ViewModel.HandleItemDroppedAsync(new ItemDroppedEventArgs(sideBarItem.Item, rawEvent.DataView, dropPosition, rawEvent));
		}

		internal async Task RaiseItemDragOver(SidebarItem sideBarItem, SidebarItemDropPosition dropPosition, DragEventArgs rawEvent)
		{
			if (sideBarItem.Item is null || ViewModel is null) return;
			await ViewModel.HandleItemDragOverAsync(new ItemDragOverEventArgs(sideBarItem.Item, rawEvent.DataView, dropPosition, rawEvent));
		}

		private void UpdateMinimalMode()
		{
			if (DisplayMode is not SidebarDisplayMode.Minimal)
				return;

			VisualStateManager.GoToState(this, IsPaneOpen ? VisualStateName_MinimalExpanded : VisualStateName_MinimalCollapsed, true);
		}

		private void UpdateDisplayMode()
		{
			if (DisplayMode is SidebarDisplayMode.Minimal)
			{
				IsPaneOpen = false;
				UpdateMinimalMode();
				return;
			}

			VisualStateManager.GoToState(
				this,
				DisplayMode switch
				{
					SidebarDisplayMode.Compact => VisualStateName_Compact,
					SidebarDisplayMode.Expanded => VisualStateName_Expanded,
					_ => throw new InvalidOperationException("Invalid display mode"),
				},
				true);

		}

		private void UpdateDisplayModeForPaneWidth(double newPaneWidth)
		{
			if (newPaneWidth < CompactModeThresholdWidth)
			{
				DisplayMode = SidebarDisplayMode.Compact;
			}
			else if (newPaneWidth > CompactModeThresholdWidth)
			{
				DisplayMode = SidebarDisplayMode.Expanded;
				OpenPaneLength = newPaneWidth;
			}
		}

		private void UpdateOpenPaneLength()
		{
			if (_panePanel is not null)
				_panePanel.Width = OpenPaneLength;
		}
	}
}
