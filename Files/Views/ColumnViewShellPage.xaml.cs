using Files.Dialogs;
using Files.Filesystem;
using Files.Filesystem.FilesystemHistory;
using Files.Helpers;
using Files.Interacts;
using Files.UserControls;
using Files.UserControls.Selection;
using Files.View_Models;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Views.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ColumnViewShellPage : Page
    {
        private ItemViewModel FileSystemViewModel;
        private ReadOnlyObservableCollection<ListedItem> ListOfFiles;
        private Interaction Interaction;
        private IShellPage CurrentInstance;
        private int RootBladeNumber;
        private string CurrentFolderPath;

        public static event EventHandler NotifyRoot;
        public static event EventHandler GetCurrentColumnAndClearRest;
        public static event EventHandler SetSelectedItems;
        public ColumnViewShellPage()
        {
            InitializeComponent();

            var selectionRectangle = RectangleSelection.Create(FileList, SelectionRectangle, ListView_SelectionChanged);
            selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
            FileList.Focus(FocusState.Programmatic);
        }

        private async void SelectionRectangle_SelectionEnded(object sender, EventArgs e)
        {
            await Task.Delay(200);
            FileList.Focus(FocusState.Programmatic);
        }

        private void RightClickContextMenu_Opening(object sender, object e)
        {

        }

        private void RightClickItemContextMenu_Opening(object sender, object e)
        {

        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var navparams = e.Parameter as ColumnViewNavParams; 
            FileSystemViewModel = navparams.ViewModel;
            ListOfFiles = navparams.ItemsSource;
            Interaction = navparams.Interaction;
            CurrentInstance = navparams.CurrentInstance;
            RootBladeNumber = navparams.BladeNumber;
            CurrentFolderPath = navparams.Path;
            await CurrentInstance.FilesystemViewModel.SetWorkingDirectoryAsync(navparams.Path);
        }

        private async void FileList_EffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
        {
            foreach (var item in ListOfFiles.ToList())
            {
                await Window.Current.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    FileSystemViewModel.LoadExtendedItemProperties(item, 24);
                    item.ItemPropertiesInitialized = true;
                });
            }
        }

        private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            // Skip code if the control or shift key is pressed
            if (ctrlPressed || shiftPressed)
            {
                return;
            }
            if (App.AppSettings.OpenItemsWithOneclick)
            {
                var item = e.ClickedItem as ListedItem;
                if (item.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    try
                    {
                        //SetSelectedItems?.Invoke(new List<ListedItem> { item }, EventArgs.Empty);
                        await Task.Delay(200);
                        Interaction.OpenItem_Click(null,null);
                    }
                    catch
                    {
                        
                    }
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    //SetSelectedItems?.Invoke(new List<ListedItem> { item }, EventArgs.Empty);
                    await Task.Delay(200);
                    NotifyRoot?.Invoke(new FolderInfo { RootBladeNumber = RootBladeNumber, Folder = item }, EventArgs.Empty);
                }
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            FileList.EffectiveViewportChanged += FileList_EffectiveViewportChanged;
        }

        private void ListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var data = ((FrameworkElement)e.OriginalSource).DataContext;
            if (data is ListedItem)
            {
                if (!FileList.SelectedItems.Cast<ListedItem>().ToList().Contains(data))
                {
                    FileList.SelectedItem = data;
                }
            }
            else
            {
                ClearSelection();
                if (((FrameworkElement)e.OriginalSource).DataContext == CurrentFolderPath)
                {
                    GetCurrentColumnAndClearRest?.Invoke(new FolderInfo { RootBladeNumber = RootBladeNumber, Path = ((FrameworkElement)e.OriginalSource).DataContext.ToString() }, EventArgs.Empty);
                }
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetSelectedItems?.Invoke(FileList.SelectedItems.Cast<ListedItem>().ToList(), EventArgs.Empty);
        }

        private async void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (!App.AppSettings.OpenItemsWithOneclick)
            {
                var data = ((FrameworkElement)e.OriginalSource).DataContext;
                if (data is ListedItem)
                {
                    var item = data as ListedItem;
                    if (item.PrimaryItemAttribute == StorageItemTypes.File)
                    {
                        try
                        {
                            //SetSelectedItems?.Invoke(new List<ListedItem> { item }, EventArgs.Empty);
                            await Task.Delay(200);
                            Interaction.OpenItem_Click(null, null);
                        }
                        catch
                        {

                        }
                    }
                    else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                    {
                        //SetSelectedItems?.Invoke(new List<ListedItem> { item }, EventArgs.Empty);
                        await Task.Delay(200);
                        NotifyRoot?.Invoke(new FolderInfo { RootBladeNumber = RootBladeNumber, Folder = item }, EventArgs.Empty);
                    }
                }
            }
        }
        public void SetSelectedItemOnUi(ListedItem item)
        {
            ClearSelection();
            FileList.SelectedItems.Add(item);
        }

        public void SetSelectedItemsOnUi(List<ListedItem> items)
        {
            ClearSelection();

            foreach (ListedItem item in items)
            {
                FileList.SelectedItems.Add(item);
            }
        }

        public void AddSelectedItemsOnUi(List<ListedItem> selectedItems)
        {
            foreach (ListedItem selectedItem in selectedItems)
            {
                FileList.SelectedItems.Add(selectedItem);
            }
        }

        public void SelectAllItems()
        {
            ClearSelection();
            FileList.SelectAll();
        }

        public void InvertSelection()
        {
            List<ListedItem> allItems = FileList.Items.Cast<ListedItem>().ToList();
            List<ListedItem> newSelectedItems = allItems.Except(FileList.SelectedItems.Cast<ListedItem>().ToList()).ToList();

            SetSelectedItemsOnUi(newSelectedItems);
        }

        public void ClearSelection()
        {
            FileList.SelectedItems.Clear();
        }

        private void FileList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (((FrameworkElement)e.OriginalSource).DataContext == CurrentFolderPath)
            {
                GetCurrentColumnAndClearRest?.Invoke(new FolderInfo { RootBladeNumber = RootBladeNumber, Path = ((FrameworkElement)e.OriginalSource).DataContext.ToString() }, EventArgs.Empty);
            }
        }

        private void Page_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as Page).Properties.IsLeftButtonPressed)
            {
                ClearSelection();
            }
            else if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed)
            {
                CurrentInstance.InteractionOperations.ItemPointerPressed(sender, e);
            }
        }
    }
}
