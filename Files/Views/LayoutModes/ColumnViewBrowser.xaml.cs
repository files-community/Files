using Files.EventArguments;
using Files.Filesystem;
using Files.Helpers;
using Files.Helpers.XamlHelpers;
using Files.Interacts;
using Files.UserControls.Selection;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Views.LayoutModes
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ColumnViewBrowser : BaseLayout
    {
        private List<Frame> Frames;
        private string NavParam;
        private DispatcherQueueTimer tapDebounceTimer;
        private ListedItem renamingItem;
        private string oldItemName;
        private TextBlock textBlock;
        public static IShellPage columnparent;
        private NavigationArguments parameters;
        private ListViewItem navigatedfolder;

        public ColumnViewBrowser() : base()
        {
            this.InitializeComponent();
            ColumnViewBase.ItemInvoked += ColumnViewBase_ItemInvoked;
            //this.DataContext = this;
            var selectionRectangle = RectangleSelection.Create(FileList, SelectionRectangle, FileList_SelectionChanged);
            tapDebounceTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        }

        private void ColumnViewBase_ItemInvoked(object sender, EventArgs e)
        {
            var column = sender as ColumnParam;
            var blade = column.ListView.FindAscendant<BladeItem>();
            try
            {
                while (ColumnHost.Items.Count > ColumnHost.Items.IndexOf(blade) + 1)
                {
                    ColumnHost.Items.RemoveAt(ColumnHost.Items.IndexOf(blade) + 1);
                    ColumnHost.ActiveBlades.RemoveAt(ColumnHost.Items.IndexOf(blade) + 1);
                }
            }
            catch
            {

            }

            var frame = new Frame();
            var newblade = new BladeItem();
            newblade.Content = frame;
            ColumnHost.Items.Add(newblade);
            //pane.NavigateWithArguments(typeof(ColumnViewBase), new NavigationArguments()
            //{
            //    NavPathParam = item.ItemPath,
            //    AssociatedTabInstance = ParentShellPageInstance
            //});

            frame.Navigate(typeof(ColumnShellPage), new ColumnParam
            {
                Column = ColumnHost.ActiveBlades.IndexOf(newblade),
                Path = column.Path
            });
        }

        private void ListViewTextBoxItemName_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (FilesystemHelpers.ContainsRestrictedCharacters(textBox.Text))
            {
                FileNameTeachingTip.Visibility = Visibility.Visible;
                FileNameTeachingTip.IsOpen = true;
            }
            else
            {
                if (FileNameTeachingTip.IsOpen == true)
                {
                    FileNameTeachingTip.IsOpen = false;
                    FileNameTeachingTip.Visibility = Visibility.Collapsed;
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            //var param = (eventArgs.Parameter as NavigationArguments);
            //NavParam = param.NavPathParam;
            //var viewmodel = new ItemViewModel(FolderSettings);
            //await ParentShellPageInstance.FilesystemViewModel.SetWorkingDirectoryAsync(NavParam);
            //await viewmodel.SetWorkingDirectoryAsync(NavParam);
            FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
            FolderSettings.LayoutModeChangeRequested += FolderSettings_LayoutModeChangeRequested;
            if (FileList.ItemsSource == null)
            {
                FileList.ItemsSource = ParentShellPageInstance.FilesystemViewModel.FilesAndFolders;
            }
            columnparent = ParentShellPageInstance;
            parameters = (NavigationArguments)eventArgs.Parameter;
            if (parameters.IsLayoutSwitch)
            {
                ReloadItemIcons();
            }
        }

        protected override void InitializeCommandsViewModel()
        {
            CommandsViewModel = new BaseLayoutCommandsViewModel(new BaseLayoutCommandImplementationModel(ParentShellPageInstance));
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
            
        }

        private async void ReloadItemIcons()
        {
            ParentShellPageInstance.FilesystemViewModel.CancelExtendedPropertiesLoading();
            foreach (ListedItem listedItem in ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.ToList())
            {
                listedItem.ItemPropertiesInitialized = false;
                if (FileList.ContainerFromItem(listedItem) != null)
                {
                    listedItem.ItemPropertiesInitialized = true;
                    await ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(listedItem, 24);
                }
            }
        }

        private void FolderSettings_LayoutModeChangeRequested(object sender, LayoutModeEventArgs e)
        {

        }

        public override void SelectAllItems()
        {
            SelectAllMethod.Invoke(FileList, null);
        }

        public override void FocusFileList()
        {
            FileList.Focus(FocusState.Programmatic);
        }

        protected override IEnumerable GetAllItems()
        {
            return (IEnumerable)FileList.ItemsSource;
        }
        private static readonly MethodInfo SelectAllMethod = typeof(ListView)
           .GetMethod("SelectAll", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);

        protected override void AddSelectedItem(ListedItem item)
        {
            FileList.SelectedItems.Add(item);
        }

        public override void InvertSelection()
        {
            List<ListedItem> newSelectedItems = GetAllItems()
                .Cast<ListedItem>()
                .Except(SelectedItems)
                .ToList();

            SetSelectedItemsOnUi(newSelectedItems);
        }

        public override void ClearSelection()
        {
            FileList.SelectedItems.Clear();
        }

        public override void SetDragModeForItems()
        {
            if (!InstanceViewModel.IsPageTypeSearchResults)
            {
                foreach (ListedItem listedItem in FileList.Items.ToList())
                {
                    if (FileList.ContainerFromItem(listedItem) is ListViewItem listViewItem)
                    {
                        listViewItem.CanDrag = listViewItem.IsSelected;
                    }
                }
            }
        }

        public override void ScrollIntoView(ListedItem item)
        {
            try
            {
                FileList.ScrollIntoView(item, ScrollIntoViewAlignment.Default);
            }
            catch (Exception)
            {
                // Catch error where row index could not be found
            }
        }

        public override void SetSelectedItemOnUi(ListedItem selectedItem)
        {
            ClearSelection();
            AddSelectedItem(selectedItem);
        }

        public override void SetSelectedItemsOnUi(List<ListedItem> selectedItems)
        {
            ClearSelection();
            AddSelectedItemsOnUi(selectedItems);
        }

        public override void AddSelectedItemsOnUi(List<ListedItem> selectedItems)
        {
            foreach (ListedItem selectedItem in selectedItems)
            {
                AddSelectedItem(selectedItem);
            }
        }

        public override void FocusSelectedItems()
        {
            FileList.ScrollIntoView(FileList.Items.Last());
        }

        public override void StartRenameItem()
        {
            renamingItem = FileList.SelectedItem as ListedItem;
            int extensionLength = renamingItem.FileExtension?.Length ?? 0;
            ListViewItem listViewItem = FileList.ContainerFromItem(renamingItem) as ListViewItem;
            TextBox textBox = null;
            textBlock = listViewItem.FindDescendant("ItemName") as TextBlock;
            textBox = listViewItem.FindDescendant("ListViewTextBoxItemName") as TextBox;
            //textBlock = (listViewItem.ContentTemplateRoot as Border).FindDescendant("ItemName") as TextBlock;
            //textBox = (listViewItem.ContentTemplateRoot as Border).FindDescendant("ListViewTextBoxItemName") as TextBox;
            textBox.Text = textBlock.Text;
            oldItemName = textBlock.Text;
            textBlock.Visibility = Visibility.Collapsed;
            textBox.Visibility = Visibility.Visible;
            textBox.Focus(FocusState.Pointer);
            textBox.LostFocus += RenameTextBox_LostFocus;
            textBox.KeyDown += RenameTextBox_KeyDown;

            int selectedTextLength = SelectedItem.ItemName.Length;
            if (!SelectedItem.IsShortcutItem && App.AppSettings.ShowFileExtensions)
            {
                selectedTextLength -= extensionLength;
            }
            textBox.Select(0, selectedTextLength);
            IsRenamingItem = true;
        }

        private void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                TextBox textBox = sender as TextBox;
                textBox.LostFocus -= RenameTextBox_LostFocus;
                textBox.Text = oldItemName;
                EndRename(textBox);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Enter)
            {
                TextBox textBox = sender as TextBox;
                textBox.LostFocus -= RenameTextBox_LostFocus;
                CommitRename(textBox);
                e.Handled = true;
            }
        }

        private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = e.OriginalSource as TextBox;
            CommitRename(textBox);
        }

        private async void CommitRename(TextBox textBox)
        {
            EndRename(textBox);
            string newItemName = textBox.Text.Trim().TrimEnd('.');

            bool successful = await UIFilesystemHelpers.RenameFileItemAsync(renamingItem, oldItemName, newItemName, ParentShellPageInstance);
            if (!successful)
            {
                renamingItem.ItemName = oldItemName;
            }
        }

        private void EndRename(TextBox textBox)
        {
            if (textBox.Parent == null)
            {
                // Navigating away, do nothing
            }
            else
            {
                textBox.Visibility = Visibility.Collapsed;
                textBlock.Visibility = Visibility.Visible;
            }

            textBox.LostFocus -= RenameTextBox_LostFocus;
            textBox.KeyDown -= RenameTextBox_KeyDown;
            FileNameTeachingTip.IsOpen = false;
            IsRenamingItem = false;
        }

        public override void ResetItemOpacity()
        {
            // throw new NotImplementedException();
        }

        public override void SetItemOpacity(ListedItem item)
        {
            // throw new NotImplementedException();
        }

        protected override ListedItem GetItemFromElement(object element)
        {
            return (element as ListViewItem).DataContext as ListedItem;
        }

        #region IDisposable

        public override void Dispose()
        {
            Debugger.Break(); // Not Implemented
            CommandsViewModel?.Dispose();
        }

        #endregion IDisposable

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e != null)
            {
                // Do not commit rename if SelectionChanged is due to selction rectangle (#3660)
                //FileList.CommitEdit();
            }
            tapDebounceTimer.Stop();
            SelectedItems = FileList.SelectedItems.Cast<ListedItem>().Where(x => x != null).ToList();
        }


        private void FileList_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (!IsRenamingItem)
            {
                HandleRightClick(sender, e);
            }
        }

        private void HandleRightClick(object sender, RightTappedRoutedEventArgs e)
        {
            var objectPressed = ((FrameworkElement)e.OriginalSource).DataContext as ListedItem;
            if (objectPressed != null)
            {
                {
                    return;
                }
            }
                // Check if RightTapped row is currently selected
            if (IsItemSelected)
            {
                if (SelectedItems.Contains(objectPressed))
                {
                    return;
                }
            }

            // The following code is only reachable when a user RightTapped an unselected row
            SetSelectedItemOnUi(objectPressed);
        }

        private void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            if (e.Key == VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown)
            {
                if (!IsRenamingItem)
                {
                    NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
                    e.Handled = true;
                }
            }
            else if (e.Key == VirtualKey.Enter && e.KeyStatus.IsMenuKeyDown)
            {
                FilePropertiesHelpers.ShowProperties(ParentShellPageInstance);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Space)
            {
                if (!IsRenamingItem && !ParentShellPageInstance.NavigationToolbar.IsEditModeEnabled)
                {
                    
                    if (App.InteractionViewModel.IsQuickLookEnabled)
                    {
                        QuickLookHelpers.ToggleQuickLook(ParentShellPageInstance);
                    }
                    e.Handled = true;
                }
            }
            else if (e.KeyStatus.IsMenuKeyDown && (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right || e.Key == VirtualKey.Up))
            {
                // Unfocus the GridView so keyboard shortcut can be handled
                (ParentShellPageInstance.NavigationToolbar as Control)?.Focus(FocusState.Pointer);
            }
            else if (ctrlPressed && shiftPressed && (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right || e.Key == VirtualKey.W))
            {
                // Unfocus the ListView so keyboard shortcut can be handled (ctrl + shift + W/"->"/"<-")
                (ParentShellPageInstance.NavigationToolbar as Control)?.Focus(FocusState.Pointer);
            }
            else if (e.KeyStatus.IsMenuKeyDown && shiftPressed && e.Key == VirtualKey.Add)
            {
                // Unfocus the ListView so keyboard shortcut can be handled (alt + shift + "+")
                (ParentShellPageInstance.NavigationToolbar as Control)?.Focus(FocusState.Pointer);
            }
        }

        private void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement)?.DataContext is ListedItem && !AppSettings.OpenItemsWithOneclick)
            {
                var item = (e.OriginalSource as FrameworkElement).DataContext as ListedItem;
                if (item.ItemType == "File folder")
                {
                    //var pane = new ModernShellPage();
                    //pane.FilesystemViewModel = new ItemViewModel(InstanceViewModel?.FolderSettings);
                    //await pane.FilesystemViewModel.SetWorkingDirectoryAsync(item.ItemPath);
                    //pane.IsPageMainPane = false;
                    //pane.NavParams = item.ItemPath;
                    try
                    {
                        while (ColumnHost.ActiveBlades.Count > 1)
                        {
                            ColumnHost.Items.RemoveAt(1);
                            ColumnHost.ActiveBlades.RemoveAt(1);
                        }
                    }
                    catch
                    {

                    }
                    if (item.ContainsFilesOrFolders)
                    {
                        var frame = new Frame();
                        var blade = new BladeItem();
                        blade.Content = frame;
                        ColumnHost.Items.Add(blade);
                        //pane.NavigateWithArguments(typeof(ColumnViewBase), new NavigationArguments()
                        //{
                        //    NavPathParam = item.ItemPath,
                        //    AssociatedTabInstance = ParentShellPageInstance
                        //});

                        frame.Navigate(typeof(ColumnShellPage), new ColumnParam
                        {
                            Column = 1,
                            Path = item.ItemPath
                        });
                    }
                }
                // The delay gives time for the item to be selected
                else
                {
                    NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
                }
            }
        }

        private void FileList_Holding(object sender, HoldingRoutedEventArgs e)
        {
            HandleRightClick(sender, e);
        }

        private void HandleRightClick(object sender, HoldingRoutedEventArgs e)
        {
            var objectPressed = ((FrameworkElement)e.OriginalSource).DataContext as ListedItem;
            if (objectPressed != null)
            {
                {
                    return;
                }
            }
            // Check if RightTapped row is currently selected
            if (IsItemSelected)
            {
                if (SelectedItems.Contains(objectPressed))
                {
                    return;
                }
            }

            // The following code is only reachable when a user RightTapped an unselected row
            SetSelectedItemOnUi(objectPressed);
        }

        private async void FileList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            if (ctrlPressed || shiftPressed) // Allow for Ctrl+Shift selection
            {
                return;
            }
            if (IsRenamingItem)
            {
                return;
            }
            var item = (e.ClickedItem as ListedItem);
            // Check if the setting to open items with a single click is turned on
            if (AppSettings.OpenItemsWithOneclick)
            {
                tapDebounceTimer.Stop();
                await Task.Delay(200);
                if (item.ItemType == "File folder")
                {
                    //var pane = new ModernShellPage();
                    //pane.FilesystemViewModel = new ItemViewModel(InstanceViewModel?.FolderSettings);
                    //await pane.FilesystemViewModel.SetWorkingDirectoryAsync(item.ItemPath);
                    //pane.IsPageMainPane = false;
                    //pane.NavParams = item.ItemPath;
                    try
                    {
                        while (ColumnHost.ActiveBlades.Count > 1)
                        {
                            ColumnHost.Items.RemoveAt(1);
                            ColumnHost.ActiveBlades.RemoveAt(1);
                        }
                    }
                    catch
                    {

                    }
                    if (item.ContainsFilesOrFolders)
                    {
                        var frame = new Frame();
                        var blade = new BladeItem();
                        blade.Content = frame;
                        ColumnHost.Items.Add(blade);
                        //pane.NavigateWithArguments(typeof(ColumnViewBase), new NavigationArguments()
                        //{
                        //    NavPathParam = item.ItemPath,
                        //    AssociatedTabInstance = ParentShellPageInstance
                        //});

                        frame.Navigate(typeof(ColumnShellPage), new ColumnParam
                        {
                            Column = 1,
                            Path = item.ItemPath
                        });
                    }
                }
                // The delay gives time for the item to be selected
                else
                {
                    NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
                }
            }
        }

        private void ColumnShellPage_NotifyRoot(object sender, EventArgs e)
        {
            var column = sender as ColumnParam;
            try
            {
                while (ColumnHost.ActiveBlades.Count > column.Column)
                {
                    ColumnHost.ActiveBlades.RemoveAt(column.Column + 1);
                }
            }
            catch
            {

            }
            var frame = new Frame();
            var blade = new BladeItem();
            blade.Content = frame;
            ColumnHost.Items.Add(blade);
            //pane.NavigateWithArguments(typeof(ColumnViewBase), new NavigationArguments()
            //{
            //    NavPathParam = item.ItemPath,
            //    AssociatedTabInstance = ParentShellPageInstance
            //});
            
            frame.Navigate(typeof(ColumnShellPage), new ColumnParam
            {
                Column = ColumnHost.ActiveBlades.IndexOf(blade),
                Path = column.Path
            });
        }

        private void StackPanel_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var parentContainer = DependencyObjectHelpers.FindParent<ListViewItem>(e.OriginalSource as DependencyObject);
            if (parentContainer.IsSelected)
            {
                return;
            }
            // The following code is only reachable when a user RightTapped an unselected row
            SetSelectedItemOnUi(FileList.ItemFromContainer(parentContainer) as ListedItem);
        }
        private void FileListListItem_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.KeyModifiers == VirtualKeyModifiers.Control)
            {
                if ((sender as SelectorItem).IsSelected)
                {
                    (sender as SelectorItem).IsSelected = false;
                    // Prevent issues arising caused by the default handlers attempting to select the item that has just been deselected by ctrl + click
                    e.Handled = true;
                }
            }
            else if (e.GetCurrentPoint(sender as UIElement).Properties.IsLeftButtonPressed)
            {
                if (!(sender as SelectorItem).IsSelected)
                {
                    (sender as SelectorItem).IsSelected = true;
                }
            }
        }
        private async void FileList_ChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
        {
            if (args.ItemContainer == null)
            {
                ListViewItem gvi = new ListViewItem();
                args.ItemContainer = gvi;
            }
            args.ItemContainer.DataContext = args.Item;

            if (args.Item is ListedItem item && !item.ItemPropertiesInitialized)
            {
                args.ItemContainer.PointerPressed += FileListListItem_PointerPressed;
                InitializeDrag(args.ItemContainer);
                args.ItemContainer.CanDrag = args.ItemContainer.IsSelected; // Update CanDrag

                item.ItemPropertiesInitialized = true;
                await ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(item, 24);
            }
        }

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
    }
}
