using Files.Enums;
using Files.EventArguments;
using Files.Filesystem;
using Files.Helpers;
using Files.Helpers.XamlHelpers;
using Files.Interacts;
using Files.Layouts;
using Files.UserControls;
using Files.UserControls.Selection;
using Files.ViewModels;
using Files.ViewModels.Layouts;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
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
using SortDirection = Files.Enums.SortDirection;

namespace Files.Views.LayoutModes
{
    internal sealed partial class DetailsLayoutBrowser : BaseListedLayout<DetailsLayoutViewModel, DetailsLayoutBrowser>
    {
        protected override ListViewBase FileListInternal => FileList;




        private ColumnsViewModel columnsViewModel = new ColumnsViewModel();

        public ColumnsViewModel ColumnsViewModel
        {
            get => columnsViewModel;
            set
            {
                if (value != columnsViewModel)
                {
                    columnsViewModel = value;
                    NotifyPropertyChanged(nameof(ColumnsViewModel));
                }
            }
        }

        private double maxWidthForRenameTextbox;

        public double MaxWidthForRenameTextbox
        {
            get => maxWidthForRenameTextbox;
            set
            {
                if (value != maxWidthForRenameTextbox)
                {
                    maxWidthForRenameTextbox = value;
                    NotifyPropertyChanged(nameof(MaxWidthForRenameTextbox));
                }
            }
        }

        private RelayCommand<string> UpdateSortOptionsCommand { get; set; }

        public ScrollViewer ContentScroller { get; private set; }

        public DetailsLayoutBrowser()
        {
            InitializeComponent();

            this.ViewModel = new DetailsLayoutViewModel();

            var selectionRectangle = RectangleSelection.Create(FileList, SelectionRectangle, FileList_SelectionChanged);
            selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
        }

        private void ItemManipulationModel_ScrollIntoViewInvoked(object sender, ListedItem e)
        {
            FileList.ScrollIntoView(e);
            ContentScroller?.ChangeView(null, FileList.Items.IndexOf(e) * 36, null, true); // Scroll to index * item height
        }

        private void ItemManipulationModel_StartRenameItemInvoked(object sender, EventArgs e)
        {
            StartRenameItem();
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

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            if (eventArgs.Parameter is NavigationArguments navArgs)
            {
                navArgs.FocusOnNavigation = true;
            }
            base.OnNavigatedTo(eventArgs);

            if (ParentShellPageInstance.InstanceViewModel?.FolderSettings.ColumnsViewModel != null)
            {
                ColumnsViewModel = ParentShellPageInstance.InstanceViewModel.FolderSettings.ColumnsViewModel;
            }

            CurrentIconSize = FolderSettings.GetIconSize();
            FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
            FolderSettings.LayoutModeChangeRequested += FolderSettings_LayoutModeChangeRequested;
            FolderSettings.GridViewSizeChangeRequested -= FolderSettings_GridViewSizeChangeRequested;
            FolderSettings.GridViewSizeChangeRequested += FolderSettings_GridViewSizeChangeRequested;
            FolderSettings.SortDirectionPreferenceUpdated -= FolderSettings_SortDirectionPreferenceUpdated;
            FolderSettings.SortDirectionPreferenceUpdated += FolderSettings_SortDirectionPreferenceUpdated;
            FolderSettings.SortOptionPreferenceUpdated -= FolderSettings_SortOptionPreferenceUpdated;
            FolderSettings.SortOptionPreferenceUpdated += FolderSettings_SortOptionPreferenceUpdated;
            ParentShellPageInstance.FilesystemViewModel.PageTypeUpdated -= FilesystemViewModel_PageTypeUpdated;
            ParentShellPageInstance.FilesystemViewModel.PageTypeUpdated += FilesystemViewModel_PageTypeUpdated;

            var parameters = (NavigationArguments)eventArgs.Parameter;
            if (parameters.IsLayoutSwitch)
            {
                ReloadItemIcons();
            }

            UpdateSortOptionsCommand = new RelayCommand<string>(x =>
            {
                if (!Enum.TryParse<SortOption>(x, out var val))
                {
                    return;
                }
                if (FolderSettings.DirectorySortOption == val)
                {
                    FolderSettings.DirectorySortDirection = (SortDirection)(((int)FolderSettings.DirectorySortDirection + 1) % 2);
                }
                else
                {
                    FolderSettings.DirectorySortOption = val;
                    FolderSettings.DirectorySortDirection = Microsoft.Toolkit.Uwp.UI.SortDirection.Ascending;
                }
            });

            FilesystemViewModel_PageTypeUpdated(null, new PageTypeUpdatedEventArgs()
            {
                IsTypeCloudDrive = InstanceViewModel.IsPageTypeCloudDrive,
                IsTypeRecycleBin = InstanceViewModel.IsPageTypeRecycleBin
            });

            RootGrid_SizeChanged(null, null);
        }

        private void FolderSettings_SortOptionPreferenceUpdated(object sender, SortOption e)
        {
            UpdateSortIndicator();
        }

        private void FolderSettings_SortDirectionPreferenceUpdated(object sender, SortDirection e)
        {
            UpdateSortIndicator();
        }

        private void UpdateSortIndicator()
        {
            NameHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.Name ? FolderSettings.DirectorySortDirection : (SortDirection?)null;
            TagHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.FileTag ? FolderSettings.DirectorySortDirection : (SortDirection?)null;
            OriginalPathHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.OriginalFolder ? FolderSettings.DirectorySortDirection : (SortDirection?)null;
            DateDeletedHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.DateDeleted ? FolderSettings.DirectorySortDirection : (SortDirection?)null;
            DateModifiedHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.DateModified ? FolderSettings.DirectorySortDirection : (SortDirection?)null;
            DateCreatedHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.DateCreated ? FolderSettings.DirectorySortDirection : (SortDirection?)null;
            FileTypeHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.FileType ? FolderSettings.DirectorySortDirection : (SortDirection?)null;
            ItemSizeHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.Size ? FolderSettings.DirectorySortDirection : (SortDirection?)null;
            SyncStatusHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.SyncStatus ? FolderSettings.DirectorySortDirection : (SortDirection?)null;
        }

        private void FilesystemViewModel_PageTypeUpdated(object sender, PageTypeUpdatedEventArgs e)
        {
            // This code updates which columns are hidden and which ones are shwn
            if (!e.IsTypeRecycleBin)
            {
                ColumnsViewModel.DateDeletedColumn.Hide();
                ColumnsViewModel.OriginalPathColumn.Hide();
            }
            else
            {
                ColumnsViewModel.OriginalPathColumn.Show();
                ColumnsViewModel.DateDeletedColumn.Show();
            }

            if (!e.IsTypeCloudDrive)
            {
                ColumnsViewModel.StatusColumn.Hide();
            }
            else
            {
                ColumnsViewModel.StatusColumn.Show();
            }

            if (!UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled)
            {
                ColumnsViewModel.TagColumn.Hide();
            }
            else
            {
                ColumnsViewModel.TagColumn.Show();
            }

            UpdateSortIndicator();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
            FolderSettings.GridViewSizeChangeRequested -= FolderSettings_GridViewSizeChangeRequested;
            FolderSettings.SortDirectionPreferenceUpdated -= FolderSettings_SortDirectionPreferenceUpdated;
            FolderSettings.SortOptionPreferenceUpdated -= FolderSettings_SortOptionPreferenceUpdated;
            ParentShellPageInstance.FilesystemViewModel.PageTypeUpdated -= FilesystemViewModel_PageTypeUpdated;
        }

        private void SelectionRectangle_SelectionEnded(object sender, EventArgs e)
        {
            FileList.Focus(FocusState.Programmatic);
        }

        private void FolderSettings_LayoutModeChangeRequested(object sender, LayoutModeEventArgs e)
        {
        }

        private async void FolderSettings_GridViewSizeChangeRequested(object sender, EventArgs e) // TODO(i): Stuff like this, handle through IMessenger, aaaaandd... move it to base to avoid duplication
        {
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
            while (!(item is ListViewItem))
                item = VisualTreeHelper.GetParent(item);
            var itemContainer = item as ListViewItem;
            itemContainer.ContextFlyout = ItemContextMenuFlyout;
        }

        private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // This prevents the drag selection rectangle from appearing when resizing the columns
            e.Handled = true;
        }

        private void GridSplitter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            UpdateColumnLayout();
        }

        private void GridSplitter_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right)
            {
                UpdateColumnLayout();
                ParentShellPageInstance.InstanceViewModel.FolderSettings.ColumnsViewModel = ColumnsViewModel;
            }
        }

        private void UpdateColumnLayout()
        {
            ColumnsViewModel.IconColumn.UserLength = new GridLength(Column1.ActualWidth, GridUnitType.Pixel);
            ColumnsViewModel.NameColumn.UserLength = new GridLength(Column2.ActualWidth, GridUnitType.Pixel);
            ColumnsViewModel.TagColumn.UserLength = new GridLength(Column3.ActualWidth, GridUnitType.Pixel);
            ColumnsViewModel.OriginalPathColumn.UserLength = new GridLength(Column4.ActualWidth, GridUnitType.Pixel);
            ColumnsViewModel.DateDeletedColumn.UserLength = new GridLength(Column5.ActualWidth, GridUnitType.Pixel);
            ColumnsViewModel.DateModifiedColumn.UserLength = new GridLength(Column6.ActualWidth, GridUnitType.Pixel);
            ColumnsViewModel.DateCreatedColumn.UserLength = new GridLength(Column7.ActualWidth, GridUnitType.Pixel);
            ColumnsViewModel.ItemTypeColumn.UserLength = new GridLength(Column8.ActualWidth, GridUnitType.Pixel);
            ColumnsViewModel.SizeColumn.UserLength = new GridLength(Column9.ActualWidth, GridUnitType.Pixel);
            ColumnsViewModel.StatusColumn.UserLength = new GridLength(Column10.ActualWidth, GridUnitType.Pixel);
        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ColumnsViewModel.SetDesiredSize(Math.Max(0, RootGrid.ActualWidth - 80));
            MaxWidthForRenameTextbox = Math.Max(0, RootGrid.ActualWidth - 80);
        }

        private void GridSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            ParentShellPageInstance.InstanceViewModel.FolderSettings.ColumnsViewModel = ColumnsViewModel;
        }

        private void ToggleMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            ParentShellPageInstance.InstanceViewModel.FolderSettings.ColumnsViewModel = ColumnsViewModel;
        }

        private void GridSplitter_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var columnToResize = (Grid.GetColumn(sender as Microsoft.Toolkit.Uwp.UI.Controls.GridSplitter) - 1) / 2;
            ResizeColumnToFit(columnToResize);
            e.Handled = true;
        }

        private void ResizeColumnToFit(int columnToResize)
        {
            if (!FileList.Items.Any())
            {
                return;
            }

            var maxItemLength = columnToResize switch
            {
                1 => FileList.Items.Cast<ListedItem>().Select(x => x.ItemName?.Length ?? 0).Max(), // file name column
                2 => FileList.Items.Cast<ListedItem>().Select(x => x.FileTagUI?.TagName?.Length ?? 0).Max(), // file tag column
                3 => FileList.Items.Cast<RecycleBinItem>().Select(x => x.ItemOriginalPath?.Length ?? 0).Max(), // original path column
                4 => FileList.Items.Cast<RecycleBinItem>().Select(x => x.ItemDateDeleted?.Length ?? 0).Max(), // date deleted column
                5 => FileList.Items.Cast<ListedItem>().Select(x => x.ItemDateModified?.Length ?? 0).Max(), // date modified column
                6 => FileList.Items.Cast<ListedItem>().Select(x => x.ItemDateCreated?.Length ?? 0).Max(), // date created column
                7 => FileList.Items.Cast<ListedItem>().Select(x => x.ItemType?.Length ?? 0).Max(), // item type column
                8 => FileList.Items.Cast<ListedItem>().Select(x => x.FileSize?.Length ?? 0).Max(), // item size column
                _ => 20 // cloud status column
            };
            var colunmSizeToFit = new[] { 9 }.Contains(columnToResize) ? maxItemLength : MeasureTextColumn(columnToResize, 5, maxItemLength);
            if (colunmSizeToFit > 0)
            {
                var column = columnToResize switch
                {
                    1 => ColumnsViewModel.NameColumn,
                    2 => ColumnsViewModel.TagColumn,
                    3 => ColumnsViewModel.OriginalPathColumn,
                    4 => ColumnsViewModel.DateDeletedColumn,
                    5 => ColumnsViewModel.DateModifiedColumn,
                    6 => ColumnsViewModel.DateCreatedColumn,
                    7 => ColumnsViewModel.ItemTypeColumn,
                    8 => ColumnsViewModel.SizeColumn,
                    _ => ColumnsViewModel.StatusColumn
                };
                if (columnToResize == 1)
                {
                    colunmSizeToFit += UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled ? 20 : 0;
                }
                column.UserLength = new GridLength(Math.Min(colunmSizeToFit + 30, column.NormalMaxLength), GridUnitType.Pixel);
            }

            ParentShellPageInstance.InstanceViewModel.FolderSettings.ColumnsViewModel = ColumnsViewModel;
        }

        private double MeasureTextColumn(int columnIndex, int measureItems, int maxItemLength)
        {
            var tbs = DependencyObjectHelpers.FindChildren<TextBlock>(FileList.ItemsPanelRoot).Where(x => x.Parent is Grid && Grid.GetColumn((Grid)x.Parent) == columnIndex);
            var widthPerLetter = tbs.Where(tb => !string.IsNullOrEmpty(tb.Text)).Take(measureItems).Select(tb =>
            {
                tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                return tb.DesiredSize.Width / Math.Max(1, tb.Text.Length);
            });
            if (!widthPerLetter.Any())
            {
                return 0;
            }
            return widthPerLetter.Average() * maxItemLength;
        }

        private void FileList_Loaded(object sender, RoutedEventArgs e)
        {
            ContentScroller = FileList.FindDescendant<ScrollViewer>(x => x.Name == "ScrollViewer");
        }
    }
}