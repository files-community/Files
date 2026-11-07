// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Win32;
using WinRT;

namespace Files.App.UserControls.TabBar
{
	public sealed partial class TabBar : BaseTabBar, INotifyPropertyChanged
	{
		// Dependency injections

		private readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();
		private readonly IAppearanceSettingsService AppearanceSettingsService = Ioc.Default.GetRequiredService<IAppearanceSettingsService>();
		private readonly IGeneralSettingsService GeneralSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();
		private readonly IWindowContext WindowContext = Ioc.Default.GetRequiredService<IWindowContext>();

		// Fields

		private readonly DispatcherTimer tabHoverTimer = new();

		private TabViewItem? hoveredTabViewItem;

		// Tab that currently shows the drop indicator
		private TabViewItem? dropTargetTabViewItem;

		// Used to discard drag over results that resolved after the pointer already left the tab
		private int dragOverRevision;

		// Indicates if the currently dragged tab was started from this TabBar
		private bool isDraggingTab;

		// Serialized parameter of the tab that is currently being dragged, used by the title bar drop target
		private static string? draggedTabPayload;

		// Set when a tab was dropped on the title bar, so the drop is not handled twice
		private static bool isDropHandledOnTitleBar;

		// Tells the title bar drop target that a tab of this process is being dragged
		internal static bool IsTabDragInProgress => draggedTabPayload is not null;

		internal static bool HasDraggedTabPayload => draggedTabPayload is not null;

		internal static bool TryGetDraggedTabPayload(out string payload)
		{
			payload = draggedTabPayload ?? string.Empty;

			return payload.Length > 0;
		}

		internal static void MarkDropHandledOnTitleBar() => isDropHandledOnTitleBar = true;

		private static bool ConsumeDropHandledOnTitleBar()
		{
			var handled = isDropHandledOnTitleBar;
			isDropHandledOnTitleBar = false;

			return handled;
		}

		private bool _lockDropOperation = false;

		// Starting position when dragging a tab
		private System.Drawing.Point dragStartPoint;

		// Starting time when dragging a tab
		private DateTimeOffset dragStartTime;

		// Indicates if drag operation should be canceled.
		// This value gets reset at the start of the drag operation
		private bool isCancelingDragOperation;

		// Properties

		public bool ShowTabActionsButton
			=> AppearanceSettingsService.ShowTabActions;

		public bool AllowTabsDrag
			=> WindowContext.CanDragAndDrop;

		public Rectangle DragArea
			=> DragAreaRectangle;

		// Events

		public static event EventHandler<TabBarItem?>? SelectedTabItemChanged;
		public static event EventHandler<TabBarItem?>? TabDragStarted;
		public static event EventHandler<TabBarItem?>? TabDragCompleted;

		// Constructor

		public TabBar()
		{
			InitializeComponent();

			tabHoverTimer.Interval = TimeSpan.FromMilliseconds(Constants.DragAndDrop.HoverToOpenTimespan);
			tabHoverTimer.Tick += TabHoverSelected;

			AppearanceSettingsService.PropertyChanged += (s, e) =>
			{
				switch (e.PropertyName)
				{
					case nameof(AppearanceSettingsService.ShowTabActions):
						NotifyPropertyChanged(nameof(ShowTabActionsButton));
						break;
				}
			};
		}

		private void TabView_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
		{
			if (args.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemRemoved)
			{
				App.AppModel.TabStripSelectedIndex = HorizontalTabView.SelectedItem is TabBarItem selectedItem
					? Items.IndexOf(selectedItem)
					: -1;
			}

			if (App.AppModel.TabStripSelectedIndex >= 0 && App.AppModel.TabStripSelectedIndex < Items.Count)
			{
				CurrentSelectedAppInstance = GetCurrentSelectedTabInstance();

				if (CurrentSelectedAppInstance is not null)
				{
					OnCurrentInstanceChanged(new CurrentInstanceChangedEventArgs()
					{
						CurrentInstance = CurrentSelectedAppInstance,
						PageInstances = GetAllTabInstances()
					});
				}
			}

			HorizontalTabView.SelectedIndex = App.AppModel.TabStripSelectedIndex;
		}

		private async void TabViewItem_Drop(object sender, DragEventArgs e)
		{
			if (sender is not TabViewItem { DataContext: TabBarItem { TabItemContent: { } tabContent } })
				return;

			dragOverRevision++;
			SetDropTargetTabViewItem(null);

			await tabContent.TabItemDrop(sender, e);
			HorizontalTabView.CanReorderTabs = true;
			tabHoverTimer.Stop();
		}

		private async void TabViewItem_DragEnter(object sender, DragEventArgs e)
		{
			if (sender is not TabViewItem { DataContext: TabBarItem { TabItemContent: { } tabContent } } tabViewItem)
				return;

			var revision = ++dragOverRevision;

			await tabContent.TabItemDragOver(sender, e);

			if (revision != dragOverRevision)
				return;

			if (e.AcceptedOperation != DataPackageOperation.None)
			{
				HorizontalTabView.CanReorderTabs = false;
				tabHoverTimer.Start();
				hoveredTabViewItem = tabViewItem;
				SetDropTargetTabViewItem(tabViewItem);
			}
		}

		private void TabViewItem_DragLeave(object sender, DragEventArgs e)
		{
			dragOverRevision++;
			tabHoverTimer.Stop();
			hoveredTabViewItem = null;
			SetDropTargetTabViewItem(null);
		}

		private void SetDropTargetTabViewItem(TabViewItem? tabViewItem)
		{
			if (dropTargetTabViewItem == tabViewItem)
				return;

			if (dropTargetTabViewItem is not null)
				VisualStateManager.GoToState(dropTargetTabViewItem, "NoStorageItemDragOver", true);

			dropTargetTabViewItem = tabViewItem;

			if (tabViewItem is not null)
				VisualStateManager.GoToState(tabViewItem, "StorageItemDragOver", true);
		}

		// Select tab that is hovered over for a certain duration
		private void TabHoverSelected(object? sender, object e)
		{
			tabHoverTimer.Stop();
			if (hoveredTabViewItem?.DataContext is TabBarItem tabItem)
				App.AppModel.TabStripSelectedIndex = Items.IndexOf(tabItem);
		}

		private void TabView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
		{
			// Reset value
			isCancelingDragOperation = false;
			isDraggingTab = true;

			if (args.Item is not TabBarItem { NavigationParameter: { } tabViewItemArgs } tabItem)
				return;

			args.Data.Properties.Add(TabPathIdentifier, tabViewItemArgs.Serialize());
			args.Data.RequestedOperation = DataPackageOperation.Move;

			// The title bar drop target reads the payload from here, OLE does not expose data
			// package properties to it.
			draggedTabPayload = tabViewItemArgs.Serialize();

			// Get cursor position & time to track how far the tab was dragged.
			PInvoke.GetCursorPos(out dragStartPoint);
			dragStartTime = DateTimeOffset.UtcNow;

			// Focus the UI Element, without this the focus sometimes changes
			// and the PreviewKeyDown event won't trigger.
			Focus(FocusState.Programmatic);
			PreviewKeyDown += TabDragging_PreviewKeyDown;

			TabDragStarted?.Invoke(this, tabItem);
		}

		private void TabDragging_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			// Pressing escape will automatically complete the drag event but we need to set the
			// isCancelingDragOperation field in order to detect if escape was pressed.
			if (e.Key is Windows.System.VirtualKey.Escape)
				isCancelingDragOperation = true;
		}

		private void TabView_TabStripDragOver(object sender, DragEventArgs e)
		{
			// The TabView reorders tabs dragged within this control natively and stops doing so
			// if the drag events are marked as handled, so leave those drags alone.
			if (isDraggingTab)
				return;

			if (e.DataView.Properties.ContainsKey(TabPathIdentifier))
			{
				HorizontalTabView.CanReorderTabs = WindowContext.CanDragAndDrop;

				if (!HorizontalTabView.CanReorderTabs)
					return;

				SetTabDragUIOverride(e);
				e.Handled = true;
				return;
			}

			HorizontalTabView.CanReorderTabs = false;
		}

		private void TabView_DragLeave(object sender, DragEventArgs e)
		{
			HorizontalTabView.CanReorderTabs = WindowContext.CanDragAndDrop;
			dragOverRevision++;
			SetDropTargetTabViewItem(null);
		}

		[DynamicWindowsRuntimeCast(typeof(TabView))]
		[DynamicWindowsRuntimeCast(typeof(TabViewItem))]
		private async void TabView_TabStripDrop(object sender, DragEventArgs e)
		{
			HorizontalTabView.CanReorderTabs = WindowContext.CanDragAndDrop;

			if (isDraggingTab || sender is not TabView tabStrip || !CanAcceptDrop(e.DataView))
				return;

			// The insertion index is only valid while the event is being handled, so get it before the first await.
			var index = GetTabDropIndex(tabStrip, e);

			await DropOnTabBarAsync(e, index);
		}

		private void TabBar_DragOver(object sender, DragEventArgs e)
		{
			// Tab drags that started in this control are reordered natively by the TabView.
			if (isDraggingTab || e.Handled)
				return;

			if (e.DataView.Properties.ContainsKey(TabPathIdentifier))
			{
				SetTabDragUIOverride(e);
				e.Handled = true;
				return;
			}

			HorizontalTabView.CanReorderTabs = false;

			if (!FilesystemHelpers.HasDraggedStorageItems(e.DataView))
				return;

			SetFileDragUIOverride(e);
			e.Handled = true;
		}

		private async void TabBar_Drop(object sender, DragEventArgs e)
		{
			if (isDraggingTab || e.Handled || !CanAcceptDrop(e.DataView))
				return;

			await DropOnTabBarAsync(e, -1);
		}

		private static bool CanAcceptDrop(DataPackageView dataView)
			=> dataView is not null &&
				(dataView.Properties.ContainsKey(TabPathIdentifier) || FilesystemHelpers.HasDraggedStorageItems(dataView));

		[DynamicWindowsRuntimeCast(typeof(TabViewItem))]
		private static int GetTabDropIndex(TabView tabStrip, DragEventArgs e)
		{
			for (var i = 0; i < tabStrip.TabItems.Count; i++)
			{
				if (tabStrip.ContainerFromIndex(i) is TabViewItem tabItem && e.GetPosition(tabItem).X < tabItem.ActualWidth)
					return i;
			}

			// The pointer is past the last tab, so append the dropped tab.
			return -1;
		}

		private async Task DropOnTabBarAsync(DragEventArgs e, int tabIndex)
		{
			if (_lockDropOperation)
				return;

			e.Handled = true;
			_lockDropOperation = true;

			var deferral = e.GetDeferral();
			try
			{
				if (e.DataView.Properties.ContainsKey(TabPathIdentifier))
					await AddTabFromDragAsync(e.DataView, tabIndex);
				else
					await OpenDraggedItemsInNewTabsAsync(e.DataView);
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Failed to process a drop on the tab bar.");
			}
			finally
			{
				// The deferral must always be completed, otherwise the drag source is left waiting.
				deferral.Complete();
				_lockDropOperation = false;
			}
		}

		private static async Task AddTabFromDragAsync(DataPackageView dataView, int index)
		{
			if (!dataView.Properties.TryGetValue(TabPathIdentifier, out var value) || value is not string serializedTab)
				return;

			TabBarItemParameter tabItemParameter;
			try
			{
				tabItemParameter = TabBarItemParameter.Deserialize(serializedTab);
			}
			catch (JsonException)
			{
				return;
			}

			// Tell the source window that the tab was accepted, so it can close it.
			ApplicationData.Current.LocalSettings.Values[TabDropHandledIdentifier] = true;
			await NavigationHelpers.AddNewTabByParamAsync(tabItemParameter.InitialPageType, tabItemParameter.NavigationParameter, index);
		}

		private static async Task OpenDraggedItemsInNewTabsAsync(DataPackageView dataView)
		{
			var items = await FilesystemHelpers.GetDraggedStorageItems(dataView);

			// Dropping several files from the same folder must not open that folder more than once.
			var paths = items
				.Select(item => item.ItemType is FilesystemItemType.Directory
					? item.Path
					: System.IO.Path.GetDirectoryName(item.Path))
				.Where(path => !string.IsNullOrEmpty(path))
				.Distinct(StringComparer.OrdinalIgnoreCase);

			foreach (var path in paths)
			{
				if (!string.IsNullOrEmpty(path))
					await NavigationHelpers.OpenPathInNewTab(path, true);
			}
		}

		private static void SetTabDragUIOverride(DragEventArgs e)
		{
			e.AcceptedOperation = DataPackageOperation.Move;
			e.DragUIOverride.Caption = Strings.TabStripDragAndDropUIOverrideCaption.GetLocalizedResource();
			e.DragUIOverride.IsCaptionVisible = true;
			e.DragUIOverride.IsGlyphVisible = false;
		}

		private static void SetFileDragUIOverride(DragEventArgs e)
		{
			e.AcceptedOperation = DataPackageOperation.Link;
			e.DragUIOverride.Caption = Strings.OpenInNewTab.GetLocalizedResource();
			e.DragUIOverride.IsCaptionVisible = true;
		}

		private void TabView_TabDragCompleted(TabView sender, TabViewTabDragCompletedEventArgs args)
		{
			// Unsubscribe from the key down event, it's only needed when a tab is actively being dragged
			PreviewKeyDown -= TabDragging_PreviewKeyDown;

			isDraggingTab = false;
			draggedTabPayload = null;
			TabDragCompleted?.Invoke(this, args.Item as TabBarItem);

			if (ApplicationData.Current.LocalSettings.Values.ContainsKey(TabDropHandledIdentifier) &&
				(bool)ApplicationData.Current.LocalSettings.Values[TabDropHandledIdentifier])
				CloseTab(args.Item as TabBarItem);
			else
				HorizontalTabView.SelectedItem = args.Tab;

			ApplicationData.Current.LocalSettings.Values.Remove(TabDropHandledIdentifier);
		}

		/// <summary>
		/// Opens a tab that was dropped on the empty part of the title bar.
		/// </summary>
		internal async Task OpenTabFromTitleBarDropAsync(string serializedTab)
		{
			TabBarItemParameter tabItemParameter;
			try
			{
				tabItemParameter = TabBarItemParameter.Deserialize(serializedTab);
			}
			catch (JsonException)
			{
				return;
			}

			// Tells the source window that the tab was accepted, so it can close it.
			ApplicationData.Current.LocalSettings.Values[TabDropHandledIdentifier] = true;
			await NavigationHelpers.AddNewTabByParamAsync(tabItemParameter.InitialPageType, tabItemParameter.NavigationParameter, -1);
		}

		private async void TabView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
		{
			// Unsubscribe from the key down event, it's only needed when a tab is actively being dragged
			PreviewKeyDown -= TabDragging_PreviewKeyDown;

			isDraggingTab = false;
			draggedTabPayload = null;

			// The tab was dropped on the title bar and is already handled there.
			if (ConsumeDropHandledOnTitleBar())
			{
				// The tab was moved into the tab strip by the title bar drop target,
				// so only the source tab has to be removed here.
				if (args.Item is TabBarItem droppedTab && Items.Contains(droppedTab))
				{
					Items.Remove(droppedTab);
					droppedTab.Unload();
				}

				ApplicationData.Current.LocalSettings.Values.Remove(TabDropHandledIdentifier);

				return;
			}

			if (isCancelingDragOperation)
				return;

			PInvoke.GetCursorPos(out var droppedPoint);
			var droppedTime = DateTimeOffset.UtcNow;
			var dragTime = droppedTime - dragStartTime;
			var dragDistance = Math.Sqrt(Math.Pow(dragStartPoint.X - droppedPoint.X, 2) + Math.Pow(dragStartPoint.Y - droppedPoint.Y, 2));

			if (sender.TabItems.Count == 1 ||
				(dragTime.TotalSeconds < 1 &&
				dragDistance < 100))
				return;

			var indexOfTabViewItem = sender.TabItems.IndexOf(args.Item);
			if (args.Item is not TabBarItem { NavigationParameter: { } tabViewItemArgs } tabItem)
				return;

			var selectedTabViewItemIndex = sender.SelectedIndex;

			Items.Remove(tabItem);
			if (!await NavigationHelpers.OpenTabInNewWindowAsync(tabViewItemArgs.Serialize(), droppedPoint.X, droppedPoint.Y))
			{
				// Other tabs can close while the new window opens, so the remembered positions may no longer exist
				Items.Insert(Math.Clamp(indexOfTabViewItem, 0, Items.Count), tabItem);
				sender.SelectedIndex = Math.Clamp(selectedTabViewItemIndex, 0, Items.Count - 1);
			}
			else
				// Dispose tab arguments
				tabItem.Unload();
		}

		private void TabView_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
		{
			var delta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;
			var scrollUpSelectsNextTab = !GeneralSettingsService.ReverseTabScrollDirection;

			if ((delta > 0) == scrollUpSelectsNextTab)
			{
				if (App.AppModel.TabStripSelectedIndex < Items.Count - 1)
					App.AppModel.TabStripSelectedIndex++;
			}
			else
			{
				if (App.AppModel.TabStripSelectedIndex > 0)
					App.AppModel.TabStripSelectedIndex--;
			}

			e.Handled = true;
		}

		[DynamicWindowsRuntimeCast(typeof(MenuFlyout))]
		private void TabItemContextMenu_Opening(object sender, object e)
		{
			MenuItemMoveTabToNewWindow.IsEnabled = Items.Count > 1;
			SelectedTabItemChanged?.Invoke(null, ((MenuFlyout)sender).Target.DataContext as TabBarItem);
		}

		private void TabItemContextMenu_Closing(object sender, object e)
		{
			SelectedTabItemChanged?.Invoke(null, null);
		}

		private async void TabBarAddNewTabButton_Drop(object sender, DragEventArgs e)
		{
			if (e.Handled || _lockDropOperation || !FilesystemHelpers.HasDraggedStorageItems(e.DataView))
				return;

			await DropOnTabBarAsync(e, -1);
		}

		private void TabBarAddNewTabButton_DragOver(object sender, DragEventArgs e)
		{
			if (!FilesystemHelpers.HasDraggedStorageItems(e.DataView))
			{
				e.AcceptedOperation = DataPackageOperation.None;
				return;
			}

			SetFileDragUIOverride(e);
			e.Handled = true;
		}

		public override DependencyObject ContainerFromItem(ITabBarItem item)
		{
			return HorizontalTabView.ContainerFromItem(item);
		}

		[DynamicWindowsRuntimeCast(typeof(TabViewItem))]
		[DynamicWindowsRuntimeCast(typeof(ContentControl))]
		private void TabViewItem_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is TabViewItem tvi && tvi.FindDescendant("IconControl") is ContentControl control)
			{
				control.Content = tvi.IconSource?.CreateIconElement();
				tvi.RegisterPropertyChangedCallback(TabViewItem.IconSourceProperty, (s, args) =>
				{
					if (s is TabViewItem tabViewItem && tabViewItem.FindDescendant("IconControl") is ContentControl iconControl)
						iconControl.Content = tabViewItem.IconSource?.CreateIconElement();
				});
			}
		}

		private void DragAreaRectangle_Loaded(object sender, RoutedEventArgs e)
			=> this.RunAfterNextRender(UpdateTitleBarInsets);

		private void UpdateTitleBarInsets()
		{
			var titleBarInset = ((AppLanguageHelper.IsPreferredLanguageRtl
				? MainWindow.Instance.AppWindow.TitleBar.LeftInset
				: MainWindow.Instance.AppWindow.TitleBar.RightInset) / DragAreaRectangle.XamlRoot.RasterizationScale) + 40;

			RightPaddingColumn.Width = new(titleBarInset > 40 ? titleBarInset : 138);
			HorizontalTabView.Measure(new(
				Math.Max(0, HorizontalTabView.ActualWidth - TabBarAddNewTabButton.Width - titleBarInset),
				Math.Max(0, HorizontalTabView.ActualHeight)));
		}
	}
}
