using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.UI;
using Files.App.Commands;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls.MultitaskingControl
{
	public sealed partial class HorizontalMultitaskingControl : BaseMultitaskingControl
	{
		public static event EventHandler<TabItem?>? SelectedTabItemChanged;

		private ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		private readonly DispatcherTimer tabHoverTimer = new DispatcherTimer();
		private TabViewItem? hoveredTabViewItem;

		public HorizontalMultitaskingControl()
		{
			InitializeComponent();
			tabHoverTimer.Interval = TimeSpan.FromMilliseconds(500);
			tabHoverTimer.Tick += TabHoverSelected;

			var appWindowTitleBar = App.GetAppWindow(App.Window).TitleBar;
			double rightPaddingColumnWidth = FilePropertiesHelpers.FlowDirectionSettingIsRightToLeft ? appWindowTitleBar.LeftInset : appWindowTitleBar.RightInset;
			RightPaddingColumn.Width = new GridLength(rightPaddingColumnWidth >= 0 ? rightPaddingColumnWidth : 0);
		}

		private void HorizontalTabView_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
		{
			if (args.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemRemoved)
			{
				App.AppModel.TabStripSelectedIndex = Items.IndexOf(HorizontalTabView.SelectedItem as TabItem);
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
			await ((sender as TabViewItem).DataContext as TabItem).Control.TabItemContent.TabItemDrop(sender, e);
			HorizontalTabView.CanReorderTabs = true;
			tabHoverTimer.Stop();
		}

		private async void TabViewItem_DragEnter(object sender, DragEventArgs e)
		{
			await ((sender as TabViewItem).DataContext as TabItem).Control.TabItemContent.TabItemDragOver(sender, e);
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
			{
				App.AppModel.TabStripSelectedIndex = Items.IndexOf(hoveredTabViewItem.DataContext as TabItem);
			}
		}

		private void TabStrip_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
		{
			var tabViewItemArgs = (args.Item as TabItem).TabItemArguments;
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
			if (!(sender is TabView tabStrip))
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
				var item = tabStrip.ContainerFromIndex(i) as TabViewItem;

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

		private void TabStrip_TabDragCompleted(TabView sender, TabViewTabDragCompletedEventArgs args)
		{
			if (ApplicationData.Current.LocalSettings.Values.ContainsKey(TabDropHandledIdentifier) &&
				(bool)ApplicationData.Current.LocalSettings.Values[TabDropHandledIdentifier])
			{
				CloseTab(args.Item as TabItem);
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

		private async void TabStrip_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
		{
			if (sender.TabItems.Count == 1)
			{
				return;
			}

			var indexOfTabViewItem = sender.TabItems.IndexOf(args.Item);
			var tabViewItemArgs = (args.Item as TabItem).TabItemArguments;
			var selectedTabViewItemIndex = sender.SelectedIndex;
			Items.Remove(args.Item as TabItem);
			if (!await NavigationHelpers.OpenTabInNewWindowAsync(tabViewItemArgs.Serialize()))
			{
				Items.Insert(indexOfTabViewItem, args.Item as TabItem);
				sender.SelectedIndex = selectedTabViewItemIndex;
			}
			else
			{
				(args.Item as TabItem)?.Unload(); // Dispose tab arguments
			}
		}

		private void TabItemContextMenu_Opening(object sender, object e)
		{
			MenuItemMoveTabToNewWindow.IsEnabled = Items.Count > 1;
			SelectedTabItemChanged?.Invoke(null, ((MenuFlyout)sender).Target.DataContext as TabItem);
		}
		private void TabItemContextMenu_Closing(object sender, object e)
		{
			SelectedTabItemChanged?.Invoke(null, null);
		}

		public override DependencyObject ContainerFromItem(ITabItem item) => HorizontalTabView.ContainerFromItem(item);

		public UIElement ActionsControl
		{
			get { return (UIElement)GetValue(ActionsControlProperty); }
			set { SetValue(ActionsControlProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ActionsControl.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ActionsControlProperty =
			DependencyProperty.Register("ActionsControl", typeof(UIElement), typeof(HorizontalMultitaskingControl), new PropertyMetadata(null));

		public Visibility TabStripVisibility
		{
			get { return (Visibility)GetValue(TabStripVisibilityProperty); }
			set { SetValue(TabStripVisibilityProperty, value); }
		}

		// Using a DependencyProperty as the backing store for TabStripVisibility.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TabStripVisibilityProperty =
			DependencyProperty.Register("TabStripVisibility", typeof(Visibility), typeof(HorizontalMultitaskingControl), new PropertyMetadata(Visibility.Visible));

		public Rectangle DragArea => DragAreaRectangle;

		private void TabViewItem_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is TabViewItem tvi && tvi.FindDescendant("IconControl") is ContentControl control)
			{
				control.Content = (tvi.IconSource as ImageIconSource).CreateIconElement();
				tvi.RegisterPropertyChangedCallback(TabViewItem.IconSourceProperty, (s, args) =>
				{
					if (s is TabViewItem tabViewItem && tabViewItem.FindDescendant("IconControl") is ContentControl iconControl)
						iconControl.Content = (tabViewItem.IconSource as ImageIconSource).CreateIconElement();
				});
			}
		}
	}
}
