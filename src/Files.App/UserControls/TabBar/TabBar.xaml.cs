// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Files.App.UserControls.TabBar
{
	public sealed partial class TabBar : BaseTabBar
	{
		public static event EventHandler<TabBarItem?>? SelectedTabItemChanged;

		private readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();

		private readonly DispatcherTimer tabHoverTimer = new();

		private TabViewItem? hoveredTabViewItem;

		public static readonly DependencyProperty FooterElementProperty =
			DependencyProperty.Register(
				nameof(FooterElement),
				typeof(UIElement),
				typeof(TabBar),
				new PropertyMetadata(null));

		public UIElement FooterElement
		{
			get => (UIElement)GetValue(FooterElementProperty);
			set => SetValue(FooterElementProperty, value);
		}

		public static readonly DependencyProperty TabStripVisibilityProperty =
			DependencyProperty.Register(
				nameof(TabStripVisibility),
				typeof(Visibility),
				typeof(TabBar),
				new PropertyMetadata(Visibility.Visible));

		public Visibility TabStripVisibility
		{
			get => (Visibility)GetValue(TabStripVisibilityProperty);
			set => SetValue(TabStripVisibilityProperty, value);
		}

		// Dragging makes the app crash when run as admin.
		// For more information:
		// - https://github.com/files-community/Files/issues/12390
		// - https://github.com/microsoft/terminal/issues/12017#issuecomment-1004129669
		public bool AllowTabsDrag
			=> !ElevationHelpers.IsAppRunAsAdmin();

		public Rectangle DragArea
			=> DragAreaRectangle;

		/// <summary> Starting position when dragging a tab.</summary>
		private InteropHelpers.POINT dragStartPoint;

		/// <summary> Starting time when dragging a tab. </summary>
		private DateTimeOffset dragStartTime;

		/// <summary>
		/// Indicates if drag operation should be canceled.
		/// This value gets reset at the start of the drag operation
		/// </summary>
		private bool isCancelingDragOperation;

		public TabBar()
		{
			InitializeComponent();

			tabHoverTimer.Interval = TimeSpan.FromMilliseconds(500);
			tabHoverTimer.Tick += TabHoverSelected;

			var appWindow = MainWindow.Instance.AppWindow;

			double rightPaddingColumnWidth =
				FilePropertiesHelpers.FlowDirectionSettingIsRightToLeft
					? appWindow.TitleBar.LeftInset
					: appWindow.TitleBar.RightInset;

			RightPaddingColumn.Width = new(rightPaddingColumnWidth >= 0 ? rightPaddingColumnWidth : 0);
		}

		private void TabView_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
		{
			if (args.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemRemoved)
				App.AppModel.TabStripSelectedIndex = Items.IndexOf(HorizontalTabView.SelectedItem as TabBarItem);

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
			await ((sender as TabViewItem).DataContext as TabBarItem).TabItemContent.TabItemDrop(sender, e);
			HorizontalTabView.CanReorderTabs = true;
			tabHoverTimer.Stop();
		}

		private async void TabViewItem_DragEnter(object sender, DragEventArgs e)
		{
			await ((sender as TabViewItem).DataContext as TabBarItem).TabItemContent.TabItemDragOver(sender, e);
			if (e.AcceptedOperation != DataPackageOperation.None)
			{
				HorizontalTabView.CanReorderTabs = false;
				tabHoverTimer.Start();
				hoveredTabViewItem = sender as TabViewItem;
			}
		}

		private void TabViewItem_DragLeave(object sender, DragEventArgs e)
		{
			tabHoverTimer.Stop();
			hoveredTabViewItem = null;
		}

		// Select tab that is hovered over for a certain duration
		private void TabHoverSelected(object sender, object e)
		{
			tabHoverTimer.Stop();
			if (hoveredTabViewItem is not null)
				App.AppModel.TabStripSelectedIndex = Items.IndexOf(hoveredTabViewItem.DataContext as TabBarItem);
		}

		private void TabView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
		{
			// Reset value
			isCancelingDragOperation = false;

			var tabViewItemArgs = (args.Item as TabBarItem).NavigationParameter;
			args.Data.Properties.Add(TabPathIdentifier, tabViewItemArgs.Serialize());
			args.Data.RequestedOperation = DataPackageOperation.Move;

			// Get cursor position & time to track how far the tab was dragged.
			InteropHelpers.GetCursorPos(out dragStartPoint);
			dragStartTime = DateTimeOffset.UtcNow;

			// Focus the UI Element, without this the focus sometimes changes
			// and the PreviewKeyDown event won't trigger.
			Focus(FocusState.Programmatic);
			PreviewKeyDown += TabDragging_PreviewKeyDown;
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
			if (e.DataView.Properties.ContainsKey(TabPathIdentifier))
			{
				HorizontalTabView.CanReorderTabs = true && !ElevationHelpers.IsAppRunAsAdmin();

				e.AcceptedOperation = DataPackageOperation.Move;
				e.DragUIOverride.Caption = "TabStripDragAndDropUIOverrideCaption".GetLocalizedResource();
				e.DragUIOverride.IsCaptionVisible = true;
				e.DragUIOverride.IsGlyphVisible = false;
			}
			else
			{
				HorizontalTabView.CanReorderTabs = false;
			}
		}

		private void TabView_DragLeave(object sender, DragEventArgs e)
		{
			HorizontalTabView.CanReorderTabs = true && !ElevationHelpers.IsAppRunAsAdmin();
		}

		private async void TabView_TabStripDrop(object sender, DragEventArgs e)
		{
			HorizontalTabView.CanReorderTabs = true && !ElevationHelpers.IsAppRunAsAdmin();

			if (!(sender is TabView tabStrip))
				return;

			if (!e.DataView.Properties.TryGetValue(TabPathIdentifier, out object tabViewItemPathObj) ||
				tabViewItemPathObj is not string tabViewItemString)
				return;

			var index = -1;

			for (int i = 0; i < tabStrip.TabItems.Count; i++)
			{
				var item = tabStrip.ContainerFromIndex(i) as TabViewItem;

				if (e.GetPosition(item).Y - item.ActualHeight < 0)
				{
					index = i;
					break;
				}
			}

			var tabViewItemArgs = CustomTabViewItemParameter.Deserialize(tabViewItemString);
			ApplicationData.Current.LocalSettings.Values[TabDropHandledIdentifier] = true;
			await NavigationHelpers.AddNewTabByParamAsync(tabViewItemArgs.InitialPageType, tabViewItemArgs.NavigationParameter, index);
		}

		private void TabView_TabDragCompleted(TabView sender, TabViewTabDragCompletedEventArgs args)
		{
			// Unsubscribe from the key down event, it's only needed when a tab is actively being dragged
			PreviewKeyDown -= TabDragging_PreviewKeyDown;

			if (ApplicationData.Current.LocalSettings.Values.ContainsKey(TabDropHandledIdentifier) &&
				(bool)ApplicationData.Current.LocalSettings.Values[TabDropHandledIdentifier])
				CloseTab(args.Item as TabBarItem);
			else
				HorizontalTabView.SelectedItem = args.Tab;

			if (ApplicationData.Current.LocalSettings.Values.ContainsKey(TabDropHandledIdentifier))
				ApplicationData.Current.LocalSettings.Values.Remove(TabDropHandledIdentifier);
		}

		private async void TabView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
		{
			// Unsubscribe from the key down event, it's only needed when a tab is actively being dragged
			PreviewKeyDown -= TabDragging_PreviewKeyDown;

			if (isCancelingDragOperation)
				return;

			InteropHelpers.GetCursorPos(out var droppedPoint);
			var droppedTime = DateTimeOffset.UtcNow;
			var dragTime = droppedTime - dragStartTime;
			var dragDistance = Math.Sqrt(Math.Pow((dragStartPoint.X - droppedPoint.X), 2) + Math.Pow((dragStartPoint.Y - droppedPoint.Y), 2));

			if (sender.TabItems.Count == 1 ||
				(dragTime.TotalSeconds < 1 &&
				dragDistance < 100))
				return;

			var indexOfTabViewItem = sender.TabItems.IndexOf(args.Item);
			var tabViewItemArgs = (args.Item as TabBarItem).NavigationParameter;
			var selectedTabViewItemIndex = sender.SelectedIndex;

			Items.Remove(args.Item as TabBarItem);
			if (!await NavigationHelpers.OpenTabInNewWindowAsync(tabViewItemArgs.Serialize()))
			{
				Items.Insert(indexOfTabViewItem, args.Item as TabBarItem);
				sender.SelectedIndex = selectedTabViewItemIndex;
			}
			else
				// Dispose tab arguments
				(args.Item as TabBarItem)?.Unload();
		}

		private void TabView_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
		{
			var delta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;

			if (delta > 0)
			{
				// Scroll up, select the next tab
				if (HorizontalTabView.SelectedIndex < HorizontalTabView.TabItems.Count - 1)
					HorizontalTabView.SelectedIndex++;
			}
			else
			{
				// Scroll down, select the previous tab
				if (HorizontalTabView.SelectedIndex > 0)
					HorizontalTabView.SelectedIndex--;
			}

			e.Handled = true;
		}

		private void TabItemContextMenu_Opening(object sender, object e)
		{
			MenuItemMoveTabToNewWindow.IsEnabled = Items.Count > 1;
			SelectedTabItemChanged?.Invoke(null, ((MenuFlyout)sender).Target.DataContext as TabBarItem);
		}

		private void TabItemContextMenu_Closing(object sender, object e)
		{
			SelectedTabItemChanged?.Invoke(null, null);
		}

		public override DependencyObject ContainerFromItem(ITabBarItem item)
		{
			return HorizontalTabView.ContainerFromItem(item);
		}

		private void TabViewItem_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is TabViewItem tvi && tvi.FindDescendant("IconControl") is ContentControl control)
			{
				control.Content = (tvi.IconSource as ImageIconSource)?.CreateIconElement();
				tvi.RegisterPropertyChangedCallback(TabViewItem.IconSourceProperty, (s, args) =>
				{
					if (s is TabViewItem tabViewItem && tabViewItem.FindDescendant("IconControl") is ContentControl iconControl)
						iconControl.Content = (tabViewItem.IconSource as ImageIconSource)?.CreateIconElement();
				});
			}
		}
	}
}
