using Files.Filesystem;
using Files.Interacts;
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

namespace Files.UserControls.MultitaskingControl
{
    public sealed partial class VerticalTabViewControl : BaseMultitaskingControl
    {
        private readonly DispatcherTimer tabHoverTimer = new DispatcherTimer();
        private TabViewItem hoveredTabViewItem = null;

        public VerticalTabViewControl()
        {
            InitializeComponent();
            tabHoverTimer.Interval = TimeSpan.FromMilliseconds(500);
            tabHoverTimer.Tick += TabHoverSelected;
        }

        private void VerticalTabView_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
        {
            switch (args.CollectionChange)
            {
                case Windows.Foundation.Collections.CollectionChange.ItemRemoved:
                    App.InteractionViewModel.TabStripSelectedIndex = Items.IndexOf(VerticalTabView.SelectedItem as TabItem);
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

                await CurrentSelectedAppInstance.InteractionOperations.FilesystemHelpers.PerformOperationTypeAsync(DataPackageOperation.Move, e.DataView, tabViewItemWorkingDir, true);
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
            VerticalTabView.CanReorderTabs = true;
            tabHoverTimer.Stop();
        }

        private void TabViewItem_DragEnter(object sender, DragEventArgs e)
        {
            if (e.DataView.AvailableFormats.Contains(StandardDataFormats.StorageItems))
            {
                VerticalTabView.CanReorderTabs = false;
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
                VerticalTabView.SelectedItem = hoveredTabViewItem;
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
                VerticalTabView.CanReorderTabs = true;
                e.AcceptedOperation = DataPackageOperation.Move;
                e.DragUIOverride.Caption = "TabStripDragAndDropUIOverrideCaption".GetLocalized();
                e.DragUIOverride.IsCaptionVisible = true;
                e.DragUIOverride.IsGlyphVisible = false;
            }
            else
            {
                VerticalTabView.CanReorderTabs = false;
            }
        }

        private void TabStrip_DragLeave(object sender, DragEventArgs e)
        {
            VerticalTabView.CanReorderTabs = true;
        }

        private async void TabStrip_TabStripDrop(object sender, DragEventArgs e)
        {
            VerticalTabView.CanReorderTabs = true;
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
    }
}