// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Files.App.UserControls.TabView
{
	public sealed partial class TabView : BaseTabView
	{
		private readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();

		private readonly DispatcherTimer tabHoverTimer = new();

		private Microsoft.UI.Xaml.Controls.TabViewItem? hoveredTabViewItem;

		public static event EventHandler<TabViewItem?>? SelectedTabItemChanged;

		public Rectangle DragArea
			=> DragAreaRectangle;

		public UIElement ActionsControl
		{
			get => (UIElement)GetValue(ActionsControlProperty); 
			set => SetValue(ActionsControlProperty, value); 
		}

		public static readonly DependencyProperty ActionsControlProperty =
			DependencyProperty.Register(
				nameof(ActionsControl),
				typeof(UIElement),
				typeof(TabView),
				new PropertyMetadata(null));

		public Visibility TabStripVisibility
		{
			get => (Visibility)GetValue(TabStripVisibilityProperty);
			set => SetValue(TabStripVisibilityProperty, value);
		}

		public static readonly DependencyProperty TabStripVisibilityProperty =
			DependencyProperty.Register(
				nameof(TabStripVisibility),
				typeof(Visibility),
				typeof(TabView),
				new PropertyMetadata(Visibility.Visible));

		public TabView()
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

		private void HorizontalTabView_TabItemsChanged(Microsoft.UI.Xaml.Controls.TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
		{
			if (args.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemRemoved)
			{
				App.AppModel.TabStripSelectedIndex = Items.IndexOf(HorizontalTabView.SelectedItem as TabViewItem);
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
			await ((sender as Microsoft.UI.Xaml.Controls.TabViewItem).DataContext as TabViewItem).Control.TabItemContent.TabItemDrop(sender, e);
			HorizontalTabView.CanReorderTabs = true;
			tabHoverTimer.Stop();
		}

		private async void TabViewItem_DragEnter(object sender, DragEventArgs e)
		{
			await ((sender as Microsoft.UI.Xaml.Controls.TabViewItem).DataContext as TabViewItem).Control.TabItemContent.TabItemDragOver(sender, e);
			if (e.AcceptedOperation != DataPackageOperation.None)
			{
				HorizontalTabView.CanReorderTabs = false;
				tabHoverTimer.Start();
				hoveredTabViewItem = sender as Microsoft.UI.Xaml.Controls.TabViewItem;
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
			{
				App.AppModel.TabStripSelectedIndex = Items.IndexOf(hoveredTabViewItem.DataContext as TabViewItem);
			}
		}

		private void TabStrip_TabDragStarting(Microsoft.UI.Xaml.Controls.TabView sender, TabViewTabDragStartingEventArgs args)
		{
			var tabViewItemArgs = (args.Item as TabViewItem).TabItemArguments;
			args.Data.Properties.Add(TabPathIdentifier, tabViewItemArgs.Serialize());
			args.Data.RequestedOperation = DataPackageOperation.Move;
		}

		private void TabStrip_TabStripDragOver(object sender, DragEventArgs e)
		{
			if (e.DataView.Properties.ContainsKey(TabPathIdentifier))
			{
				HorizontalTabView.CanReorderTabs = true;
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

		private void TabStrip_DragLeave(object sender, DragEventArgs e)
		{
			HorizontalTabView.CanReorderTabs = true;
		}

		private async void TabStrip_TabStripDrop(object sender, DragEventArgs e)
		{
			HorizontalTabView.CanReorderTabs = true;
			if (sender is not Microsoft.UI.Xaml.Controls.TabView tabStrip)
			{
				return;
			}

			if (!e.DataView.Properties.TryGetValue(TabPathIdentifier, out object tabViewItemPathObj) || !(tabViewItemPathObj is string tabViewItemString))
			{
				return;
			}

			var index = -1;

			for (int i = 0; i < tabStrip.TabItems.Count; i++)
			{
				var item = tabStrip.ContainerFromIndex(i) as Microsoft.UI.Xaml.Controls.TabViewItem;

				if (e.GetPosition(item).Y - item.ActualHeight < 0)
				{
					index = i;
					break;
				}
			}

			var tabViewItemArgs = TabItemArguments.Deserialize(tabViewItemString);
			ApplicationData.Current.LocalSettings.Values[TabDropHandledIdentifier] = true;
			await mainPageViewModel.AddNewTabByParam(tabViewItemArgs.InitialPageType, tabViewItemArgs.NavigationArg, index);
		}

		private void TabStrip_TabDragCompleted(Microsoft.UI.Xaml.Controls.TabView sender, TabViewTabDragCompletedEventArgs args)
		{
			if (ApplicationData.Current.LocalSettings.Values.ContainsKey(TabDropHandledIdentifier) &&
				(bool)ApplicationData.Current.LocalSettings.Values[TabDropHandledIdentifier])
			{
				CloseTab(args.Item as TabViewItem);
			}
			else
			{
				HorizontalTabView.SelectedItem = args.Tab;
			}

			if (ApplicationData.Current.LocalSettings.Values.ContainsKey(TabDropHandledIdentifier))
			{
				ApplicationData.Current.LocalSettings.Values.Remove(TabDropHandledIdentifier);
			}
		}

		private async void TabStrip_TabDroppedOutside(Microsoft.UI.Xaml.Controls.TabView sender, TabViewTabDroppedOutsideEventArgs args)
		{
			if (sender.TabItems.Count == 1)
			{
				return;
			}

			var indexOfTabViewItem = sender.TabItems.IndexOf(args.Item);
			var tabViewItemArgs = (args.Item as TabViewItem).TabItemArguments;
			var selectedTabViewItemIndex = sender.SelectedIndex;
			Items.Remove(args.Item as TabViewItem);
			if (!await NavigationHelpers.OpenTabInNewWindowAsync(tabViewItemArgs.Serialize()))
			{
				Items.Insert(indexOfTabViewItem, args.Item as TabViewItem);
				sender.SelectedIndex = selectedTabViewItemIndex;
			}
			else
			{
				(args.Item as TabViewItem)?.Unload(); // Dispose tab arguments
			}
		}

		private void TabItemContextMenu_Opening(object sender, object e)
		{
			MenuItemMoveTabToNewWindow.IsEnabled = Items.Count > 1;
			SelectedTabItemChanged?.Invoke(null, ((MenuFlyout)sender).Target.DataContext as TabViewItem);
		}

		private void TabItemContextMenu_Closing(object sender, object e)
		{
			SelectedTabItemChanged?.Invoke(null, null);
		}

		public override DependencyObject ContainerFromItem(ITabViewItem item)
			=> HorizontalTabView.ContainerFromItem(item);

		private void TabViewItem_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is Microsoft.UI.Xaml.Controls.TabViewItem tvi && tvi.FindDescendant("IconControl") is ContentControl control)
			{
				control.Content = (tvi.IconSource as ImageIconSource).CreateIconElement();
				tvi.RegisterPropertyChangedCallback(Microsoft.UI.Xaml.Controls.TabViewItem.IconSourceProperty, (s, args) =>
				{
					if (s is Microsoft.UI.Xaml.Controls.TabViewItem tabViewItem && tabViewItem.FindDescendant("IconControl") is ContentControl iconControl)
						iconControl.Content = (tabViewItem.IconSource as ImageIconSource).CreateIconElement();
				});
			}
		}
	}
}
