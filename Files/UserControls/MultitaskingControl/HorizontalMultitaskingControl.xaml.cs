using Files.Interacts;
using Files.View_Models;
using Files.Views;
using Files.Views.Pages;
using Microsoft.Toolkit.Uwp.Extensions;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.MultitaskingControl
{
    public sealed partial class HorizontalMultitaskingControl : BaseMultitaskingControl
    {
        private readonly DispatcherTimer tabHoverTimer = new DispatcherTimer();
        private TabViewItem hoveredTabViewItem = null;

        private SettingsViewModel AppSettings => App.AppSettings;

        public HorizontalMultitaskingControl()
        {
            InitializeComponent();
            tabHoverTimer.Interval = TimeSpan.FromMilliseconds(500);
            tabHoverTimer.Tick += TabHoverSelected;
        }

        private void DragArea_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SetTitleBar(sender as Grid);
        }

        private void HorizontalTabView_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
        {
            switch (args.CollectionChange)
            {
                case Windows.Foundation.Collections.CollectionChange.ItemRemoved:
                    App.InteractionViewModel.TabStripSelectedIndex = Items.IndexOf(HorizontalTabView.SelectedItem as TabItem);
                    break;

                case Windows.Foundation.Collections.CollectionChange.ItemInserted:
                    App.InteractionViewModel.TabStripSelectedIndex = (int)args.Index;
                    break;
            }
        }

        private async void TabViewItem_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.AvailableFormats.Contains(StandardDataFormats.StorageItems))
            {
                // TODO: Add Simpler way to find TabItem working directory
                string tabViewItemWorkingDir = ((((
                    (sender as TabViewItem)
                    .DataContext as TabItem)
                    .Content as Grid).Children[0] as Frame)
                    .Content as IShellPage)
                    .FilesystemViewModel
                    .WorkingDirectory;

                await CurrentSelectedAppInstance.InteractionOperations.FilesystemHelpers.PerformOperationTypeAsync(
                    DataPackageOperation.Move,
                    e.DataView,
                    tabViewItemWorkingDir,
                    true);
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
            HorizontalTabView.CanReorderTabs = true;
            tabHoverTimer.Stop();
        }

        private void TabViewItem_DragEnter(object sender, DragEventArgs e)
        {
            if (e.DataView.AvailableFormats.Contains(StandardDataFormats.StorageItems))
            {
                HorizontalTabView.CanReorderTabs = false;
                e.AcceptedOperation = DataPackageOperation.Move;
                tabHoverTimer.Start();
                hoveredTabViewItem = sender as TabViewItem;
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None;
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
            if (hoveredTabViewItem != null)
            {
                HorizontalTabView.SelectedItem = hoveredTabViewItem;
            }
        }

        private void TabStrip_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
        {
            var tabViewItemPath = ((((args.Item as TabItem).Content as Grid).Children[0] as Frame).Tag as TabItemContent).NavigationArg;
            args.Data.Properties.Add(TabPathIdentifier, tabViewItemPath);
            args.Data.RequestedOperation = DataPackageOperation.Move;
        }

        private void TabStrip_TabStripDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey(TabPathIdentifier))
            {
                HorizontalTabView.CanReorderTabs = true;
                e.AcceptedOperation = DataPackageOperation.Move;
                e.DragUIOverride.Caption = "TabStripDragAndDropUIOverrideCaption".GetLocalized();
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

            if (!e.DataView.Properties.TryGetValue(TabPathIdentifier, out object tabViewItemPathObj) || !(tabViewItemPathObj is string tabViewItemPath))
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

            ApplicationData.Current.LocalSettings.Values[TabDropHandledIdentifier] = true;
            await MainPage.AddNewTabByPathAsync(typeof(ModernShellPage), tabViewItemPath, index);
        }

        private void TabStrip_TabDragCompleted(TabView sender, TabViewTabDragCompletedEventArgs args)
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(TabDropHandledIdentifier) &&
                (bool)ApplicationData.Current.LocalSettings.Values[TabDropHandledIdentifier])
            {
                RemoveTab(args.Item as TabItem);
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

            var indexOfTabViewItem = sender.TabItems.IndexOf(args.Tab);
            var tabViewItemPath = ((((args.Item as TabItem).Content as Grid).Children[0] as Frame).Tag as TabItemContent).NavigationArg;
            var selectedTabViewItemIndex = sender.SelectedIndex;
            RemoveTab(args.Item as TabItem);
            if (!await Interaction.OpenPathInNewWindowAsync(tabViewItemPath))
            {
                sender.TabItems.Insert(indexOfTabViewItem, args.Tab);
                sender.SelectedIndex = selectedTabViewItemIndex;
            }
        }

        private void TabItemContextMenu_Opening(object sender, object e)
        {
            if (MainPage.MultitaskingControl.Items.Count == 1)
            {
                MenuItemMoveTabToNewWindow.IsEnabled = false;
            }
            else
            {
                MenuItemMoveTabToNewWindow.IsEnabled = true;
            }
        }
    }
}