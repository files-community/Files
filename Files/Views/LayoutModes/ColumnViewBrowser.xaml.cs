using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.UserControls;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files.Views.LayoutModes
{
    public sealed partial class ColumnViewBrowser : BaseLayout
    {
        private IBaseLayout mainPageLayout;

        public ColumnViewBrowser() : base()
        {
            this.InitializeComponent();
        }

        protected override void HookEvents()
        {
            UnhookEvents();
            ItemManipulationModel.FocusFileListInvoked += ItemManipulationModel_FocusFileListInvoked;
            ItemManipulationModel.SelectAllItemsInvoked += ItemManipulationModel_SelectAllItemsInvoked;
            ItemManipulationModel.ClearSelectionInvoked += ItemManipulationModel_ClearSelectionInvoked;
            ItemManipulationModel.InvertSelectionInvoked += ItemManipulationModel_InvertSelectionInvoked;
            ItemManipulationModel.AddSelectedItemInvoked += ItemManipulationModel_AddSelectedItemInvoked;
            ItemManipulationModel.RemoveSelectedItemInvoked += ItemManipulationModel_RemoveSelectedItemInvoked;
            ItemManipulationModel.FocusSelectedItemsInvoked += ItemManipulationModel_FocusSelectedItemsInvoked;
            ItemManipulationModel.StartRenameItemInvoked += ItemManipulationModel_StartRenameItemInvoked;
            ItemManipulationModel.ScrollIntoViewInvoked += ItemManipulationModel_ScrollIntoViewInvoked;
        }

        private void ItemManipulationModel_FocusFileListInvoked(object sender, EventArgs e)
        {
            mainPageLayout?.ItemManipulationModel.FocusFileList();
        }

        private void ItemManipulationModel_SelectAllItemsInvoked(object sender, EventArgs e)
        {
            mainPageLayout?.ItemManipulationModel.SelectAllItems();
        }

        private void ItemManipulationModel_ClearSelectionInvoked(object sender, EventArgs e)
        {
            mainPageLayout?.ItemManipulationModel.ClearSelection();
        }

        private void ItemManipulationModel_InvertSelectionInvoked(object sender, EventArgs e)
        {
            mainPageLayout?.ItemManipulationModel.InvertSelection();
        }

        private void ItemManipulationModel_AddSelectedItemInvoked(object sender, ListedItem e)
        {
            mainPageLayout?.ItemManipulationModel.AddSelectedItem(e);
        }

        private void ItemManipulationModel_RemoveSelectedItemInvoked(object sender, ListedItem e)
        {
            mainPageLayout?.ItemManipulationModel.RemoveSelectedItem(e);
        }

        private void ItemManipulationModel_FocusSelectedItemsInvoked(object sender, EventArgs e)
        {
            mainPageLayout?.ItemManipulationModel.FocusSelectedItems();
        }

        private void ItemManipulationModel_StartRenameItemInvoked(object sender, EventArgs e)
        {
            mainPageLayout?.ItemManipulationModel.StartRenameItem();
        }

        private void ItemManipulationModel_ScrollIntoViewInvoked(object sender, ListedItem e)
        {
            mainPageLayout?.ItemManipulationModel.ScrollIntoView(e);
        }

        protected override void UnhookEvents()
        {
            if (ItemManipulationModel != null)
            {
                ItemManipulationModel.FocusFileListInvoked -= ItemManipulationModel_FocusFileListInvoked;
                ItemManipulationModel.SelectAllItemsInvoked -= ItemManipulationModel_SelectAllItemsInvoked;
                ItemManipulationModel.ClearSelectionInvoked -= ItemManipulationModel_ClearSelectionInvoked;
                ItemManipulationModel.InvertSelectionInvoked -= ItemManipulationModel_InvertSelectionInvoked;
                ItemManipulationModel.AddSelectedItemInvoked -= ItemManipulationModel_AddSelectedItemInvoked;
                ItemManipulationModel.RemoveSelectedItemInvoked -= ItemManipulationModel_RemoveSelectedItemInvoked;
                ItemManipulationModel.FocusSelectedItemsInvoked -= ItemManipulationModel_FocusSelectedItemsInvoked;
                ItemManipulationModel.StartRenameItemInvoked -= ItemManipulationModel_StartRenameItemInvoked;
                ItemManipulationModel.ScrollIntoViewInvoked -= ItemManipulationModel_ScrollIntoViewInvoked;
            }
        }

        protected override ListedItem GetItemFromElement(object element)
        {
            return null;
        }

        private void ColumnViewBase_ItemInvoked(object sender, EventArgs e)
        {
            var column = sender as ColumnParam;
            if (column.ListView.FindAscendant<ColumnViewBrowser>() != this)
            {
                return;
            }

            DismissOtherBlades(column.ListView);

            var frame = new Frame();
            frame.Navigated += Frame_Navigated;
            var newblade = new BladeItem();
            newblade.Content = frame;
            ColumnHost.Items.Add(newblade);

            frame.Navigate(typeof(ColumnShellPage), new ColumnParam
            {
                Column = ColumnHost.ActiveBlades.IndexOf(newblade),
                Path = column.Path
            });
        }

        private void ContentChanged(IShellPage p)
        {
            (ParentShellPageInstance as ModernShellPage)?.RaiseContentChanged(p, p.TabItemArguments);
            if (p == MainPageFrame.Content)
            {
                mainPageLayout = p.SlimContentPage;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);

            var navigationArguments = (NavigationArguments)eventArgs.Parameter;
            MainPageFrame.Navigated += Frame_Navigated;
            MainPageFrame.Navigate(typeof(ColumnShellPage), new ColumnParam
            {
                Column = 0,
                Path = navigationArguments.NavPathParam
            });
        }

        protected override void InitializeCommandsViewModel()
        {
            CommandsViewModel = new BaseLayoutCommandsViewModel(new BaseLayoutCommandImplementationModel(ParentShellPageInstance, ItemManipulationModel));
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
        }

        #region IDisposable

        public override void Dispose()
        {
            base.Dispose();
            ColumnHost.ActiveBlades.ForEach(x => (((x.Content as Frame)?.Content as ColumnShellPage).SlimContentPage as ColumnViewBase).ItemInvoked -= ColumnViewBase_ItemInvoked);
            ColumnHost.ActiveBlades.Select(x => (x.Content as Frame)?.Content).OfType<IDisposable>().ForEach(x => x.Dispose());
            UnhookEvents();
            CommandsViewModel?.Dispose();
        }

        #endregion IDisposable

        private void DismissOtherBlades(ListView listView)
        {
            DismissOtherBlades(listView.FindAscendant<BladeItem>());
        }

        private void DismissOtherBlades(BladeItem blade)
        {
            var index = ColumnHost.ActiveBlades.IndexOf(blade);
            if (index >= 0)
            {
                Common.Extensions.IgnoreExceptions(() =>
                {
                    while (ColumnHost.ActiveBlades.Count > index + 1)
                    {
                        if ((ColumnHost.ActiveBlades[index + 1].Content as Frame)?.Content is IDisposable disposableContent)
                        {
                            disposableContent.Dispose();
                        }
                        (((ColumnHost.ActiveBlades[index + 1].Content as Frame).Content as ColumnShellPage).SlimContentPage as ColumnViewBase).ItemInvoked -= ColumnViewBase_ItemInvoked;
                        ColumnHost.Items.RemoveAt(index + 1);
                        ColumnHost.ActiveBlades.RemoveAt(index + 1);
                    }
                });
            }
            ContentChanged(LastColumnShellPage);
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            var f = sender as Frame;
            f.Navigated -= Frame_Navigated;
            (f.Content as IShellPage).ContentChanged += ColumnViewBrowser_ContentChanged;
        }

        private void ColumnViewBrowser_ContentChanged(object sender, UserControls.MultitaskingControl.TabItemArguments e)
        {
            var c = sender as IShellPage;
            c.ContentChanged -= ColumnViewBrowser_ContentChanged;
            (c.SlimContentPage as ColumnViewBase).ItemInvoked -= ColumnViewBase_ItemInvoked;
            (c.SlimContentPage as ColumnViewBase).ItemInvoked += ColumnViewBase_ItemInvoked;
            ContentChanged(c);
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            var itemContainer = (sender as Grid)?.FindAscendant<ListViewItem>();
            if (itemContainer is null)
            {
                return;
            }

            itemContainer.ContextFlyout = ItemContextMenuFlyout;
        }

        public void UpColumn()
        {
            if (!IsLastColumnBase)
            {
                DismissOtherBlades(ColumnHost.ActiveBlades[ColumnHost.ActiveBlades.Count - 2]);
            }
        }

        public void SetSelectedPathOrNavigate(PathNavigationEventArgs e)
        {
            if (!IsLastColumnBase)
            {
                foreach (var item in ColumnHost.ActiveBlades)
                {
                    if ((item.Content as Frame)?.Content is ColumnShellPage s &&
                        PathNormalization.NormalizePath(s.FilesystemViewModel.WorkingDirectory) ==
                        PathNormalization.NormalizePath(e.ItemPath))
                    {
                        DismissOtherBlades(item);
                        return;
                    }
                }
            }
            if (PathNormalization.NormalizePath(ParentShellPageInstance.FilesystemViewModel.WorkingDirectory) !=
                PathNormalization.NormalizePath(e.ItemPath))
            {
                ParentShellPageInstance.NavigateToPath(e.ItemPath);
            }
            else
            {
                DismissOtherBlades(ColumnHost.ActiveBlades[0]);
            }
        }

        public IShellPage LastColumnShellPage => IsLastColumnBase ? ParentShellPageInstance : ((ColumnHost.ActiveBlades.Last().Content as Frame).Content as ColumnShellPage);

        public bool IsLastColumnBase => (ColumnHost?.ActiveBlades is null) || ColumnHost.ActiveBlades.Count == 1;
    }
}