using Files.Enums;
using Files.EventArguments;
using Files.Filesystem;
using Files.Helpers;
using Files.Helpers.XamlHelpers;
using Files.Interacts;
using Files.Layouts;
using Files.UserControls.Selection;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Files.Backend.Enums;
using Files.Backend.ViewModels.Layouts;

namespace Files.Views.LayoutModes
{
    internal sealed partial class GridViewBrowser : BaseListedLayout<GridViewLayoutViewModel, GridViewBrowser>
    {
        protected override ListViewBase FileListInternal => FileList;

        /// <summary>
        /// The minimum item width for items. Used in the StretchedGridViewItems behavior.
        /// </summary>
        public int GridViewItemMinWidth => FolderSettings.LayoutMode == FolderLayoutModes.TilesView ? Constants.Browser.GridViewBrowser.TilesView : FolderSettings.GridViewSize;

        public GridViewBrowser()
            : base()
        {
            InitializeComponent();
            this.DataContext = this;

            var selectionRectangle = RectangleSelection.Create(FileList, SelectionRectangle, FileList_SelectionChanged);
            selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
        }

        private void ItemManipulationModel_ScrollIntoViewInvoked(object sender, ListedItem e)
        {
            FileList.ScrollIntoView(e);
        }

        private void ItemManipulationModel_StartRenameItemInvoked(object sender, EventArgs e)
        {
            StartRenameItem();
        }

        private void ItemManipulationModel_FocusSelectedItemsInvoked(object sender, EventArgs e)
        {
            if (SelectedItems.Any())
            {
                FileList.ScrollIntoView(SelectedItems.Last());
                (FileList.ContainerFromItem(SelectedItems.Last()) as GridViewItem)?.Focus(FocusState.Keyboard);
            }
        }

        private void ItemManipulationModel_AddSelectedItemInvoked(object sender, ListedItem e)
        {
            if (FileList?.Items.Contains(e) ?? false)
            {
                FileList.SelectedItems.Add(e);
            }
        }

        private void ItemManipulationModel_RemoveSelectedItemInvoked(object sender, ListedItem e)
        {
            if (FileList?.Items.Contains(e) ?? false)
            {
                FileList.SelectedItems.Remove(e);
            }
        }

        private void ItemManipulationModel_ClearSelectionInvoked(object sender, EventArgs e)
        {
            FileList.SelectedItems.Clear();
        }

        private void ItemManipulationModel_SelectAllItemsInvoked(object sender, EventArgs e)
        {
            FileList.SelectAll();
        }

        private void ItemManipulationModel_FocusFileListInvoked(object sender, EventArgs e)
        {
            FileList.Focus(FocusState.Programmatic);
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

        protected override void InitializeCommandsViewModel()
        {
            CommandsViewModel = new BaseLayoutCommandsViewModel(new BaseLayoutCommandImplementationModel(ParentShellPageInstance, ItemManipulationModel));
        }

        protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            if (eventArgs.Parameter is NavigationArguments navArgs)
            {
                navArgs.FocusOnNavigation = true;
            }
            base.OnNavigatedTo(eventArgs);

            CurrentIconSize = FolderSettings.GetIconSize();
            FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
            FolderSettings.LayoutModeChangeRequested += FolderSettings_LayoutModeChangeRequested;
            SetItemTemplate(); // Set ItemTemplate
            if (FileList.ItemsSource == null)
            {
                FileList.ItemsSource = ParentShellPageInstance.FilesystemViewModel.FilesAndFolders;
            }
            var parameters = (NavigationArguments)eventArgs.Parameter;
            if (parameters.IsLayoutSwitch)
            {
                await ReloadItemIcons();
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
            FolderSettings.GridViewSizeChangeRequested -= FolderSettings_GridViewSizeChangeRequested;
        }

        private void SelectionRectangle_SelectionEnded(object sender, EventArgs e)
        {
            FileList.Focus(FocusState.Programmatic);
        }

        private async void FolderSettings_LayoutModeChangeRequested(object sender, LayoutModeEventArgs e)
        {
            if (FolderSettings.LayoutMode == FolderLayoutModes.GridView || FolderSettings.LayoutMode == FolderLayoutModes.TilesView)
            {
                SetItemTemplate(); // Set ItemTemplate
                var requestedIconSize = FolderSettings.GetIconSize();
                if (requestedIconSize != CurrentIconSize)
                {
                    CurrentIconSize = requestedIconSize;
                    await ReloadItemIcons();
                }
            }
        }

        private void SetItemTemplate()
        {
            FileList.ItemTemplate = (FolderSettings.LayoutMode == FolderLayoutModes.TilesView) ? TilesBrowserTemplate : GridViewBrowserTemplate; // Choose Template
            SetItemMinWidth();

            // Set GridViewSize event handlers
            if (FolderSettings.LayoutMode == FolderLayoutModes.TilesView)
            {
                FolderSettings.GridViewSizeChangeRequested -= FolderSettings_GridViewSizeChangeRequested;
            }
            else if (FolderSettings.LayoutMode == FolderLayoutModes.GridView)
            {
                FolderSettings.GridViewSizeChangeRequested -= FolderSettings_GridViewSizeChangeRequested;
                FolderSettings.GridViewSizeChangeRequested += FolderSettings_GridViewSizeChangeRequested;
            }
        }

        /// <summary>
        /// Updates the min size for the item containers
        /// </summary>
        private void SetItemMinWidth()
        {
            NotifyPropertyChanged(nameof(GridViewItemMinWidth));
        }

        override protected void StartRenameItem() // TODO(i): Don't copy so much code aaaa!!! - reuse it in base
        {
            RenamingItem = SelectedItem;
            if (RenamingItem == null)
            {
                return;
            }
            int extensionLength = RenamingItem.FileExtension?.Length ?? 0;
            GridViewItem gridViewItem = FileList.ContainerFromItem(RenamingItem) as GridViewItem;
            TextBox textBox = null;
            if (gridViewItem == null)
            {
                return;
            }
            // Handle layout differences between tiles browser and photo album
            if (FolderSettings.LayoutMode == FolderLayoutModes.GridView)
            {
                Popup popup = gridViewItem.FindDescendant("EditPopup") as Popup;
                TextBlock textBlock = gridViewItem.FindDescendant("ItemName") as TextBlock;
                //Popup popup = (gridViewItem.ContentTemplateRoot as Grid).FindName("EditPopup") as Popup;
                //TextBlock textBlock = (gridViewItem.ContentTemplateRoot as Grid).FindName("ItemName") as TextBlock;
                textBox = popup.Child as TextBox;
                textBox.Text = textBlock.Text;
                popup.IsOpen = true;
                OldItemName = textBlock.Text;
            }
            else
            {
                TextBlock textBlock = gridViewItem.FindDescendant("ItemName") as TextBlock;
                textBox = gridViewItem.FindDescendant("TileViewTextBoxItemName") as TextBox;
                //TextBlock textBlock = (gridViewItem.ContentTemplateRoot as Grid).FindName("ItemName") as TextBlock;
                //textBox = (gridViewItem.ContentTemplateRoot as Grid).FindName("TileViewTextBoxItemName") as TextBox;
                textBox.Text = textBlock.Text;
                OldItemName = textBlock.Text;
                textBlock.Visibility = Visibility.Collapsed;
                textBox.Visibility = Visibility.Visible;
            }

            textBox.Focus(FocusState.Pointer);
            textBox.LostFocus += RenameTextBox_LostFocus;
            textBox.KeyDown += RenameTextBox_KeyDown;

            int selectedTextLength = SelectedItem.ItemName.Length;
            if (!SelectedItem.IsShortcutItem && UserSettingsService.PreferencesSettingsService.ShowFileExtensions)
            {
                selectedTextLength -= extensionLength;
            }
            textBox.Select(0, selectedTextLength);
            IsRenamingItem = true;
        }

        private async void FolderSettings_GridViewSizeChangeRequested(object sender, EventArgs e)
        {
            SetItemMinWidth();
            var requestedIconSize = FolderSettings.GetIconSize(); // Get new icon size

            // Prevents reloading icons when the icon size hasn't changed
            if (requestedIconSize != CurrentIconSize)
            {
                CurrentIconSize = requestedIconSize; // Update icon size before refreshing
                await ReloadItemIcons();
            }
        }

        #region IDisposable

        public override void Dispose()
        {
            base.Dispose();
            UnhookEvents();
            CommandsViewModel?.Dispose();
        }

        #endregion IDisposable

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            // This is the best way I could find to set the context flyout, as doing it in the styles isn't possible
            // because you can't use bindings in the setters
            DependencyObject item = VisualTreeHelper.GetParent(sender as Grid);
            while (!(item is GridViewItem))
                item = VisualTreeHelper.GetParent(item);
            var itemContainer = item as GridViewItem;
            itemContainer.ContextFlyout = ItemContextMenuFlyout;
        }
    }
}