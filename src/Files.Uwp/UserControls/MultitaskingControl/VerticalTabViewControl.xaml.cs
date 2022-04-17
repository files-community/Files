using Files.Uwp.Helpers;
using Files.Uwp.ViewModels;
using Microsoft.Toolkit.Uwp;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Files.Uwp.UserControls.MultitaskingControl
{
    public sealed partial class VerticalTabViewControl : BaseMultitaskingControl
    {
        private readonly DispatcherTimer tabHoverTimer = new DispatcherTimer();
        private TabViewItem hoveredTabViewItem = null;

        public VerticalTabViewControl()
        {
            InitializeComponent();

            TabView = VerticalTabView;

            tabHoverTimer.Interval = TimeSpan.FromMilliseconds(500);
            tabHoverTimer.Tick += TabHoverSelected;
        }

        private void VerticalTabView_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
        {
            if (args.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemRemoved)
            {
                App.MainViewModel.TabStripSelectedIndex = Items.IndexOf(TabView.SelectedItem as TabItem);
            }

            if (App.MainViewModel.TabStripSelectedIndex >= 0 && App.MainViewModel.TabStripSelectedIndex < Items.Count)
            {
                CurrentSelectedAppInstance = GetCurrentSelectedTabInstance();

                if (CurrentSelectedAppInstance != null)
                {
                    OnCurrentInstanceChanged(new CurrentInstanceChangedEventArgs()
                    {
                        CurrentInstance = CurrentSelectedAppInstance,
                        PageInstances = GetAllTabInstances()
                    });
                }
            }
        }

        private async void TabViewItem_Drop(object sender, DragEventArgs e)
        {
            await ((sender as TabViewItem).DataContext as TabItem).Control.TabItemContent.TabItemDrop(sender, e);
            TabView.CanReorderTabs = true;
            tabHoverTimer.Stop();
        }

        private async void TabViewItem_DragEnter(object sender, DragEventArgs e)
        {
            await ((sender as TabViewItem).DataContext as TabItem).Control.TabItemContent.TabItemDragOver(sender, e);
            if (e.AcceptedOperation != DataPackageOperation.None)
            {
                TabView.CanReorderTabs = false;
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
            if (hoveredTabViewItem != null)
            {
                TabView.SelectedItem = hoveredTabViewItem;
            }
        }

        public override DependencyObject ContainerFromItem(ITabItem item) => TabView.ContainerFromItem(item);
    }
}