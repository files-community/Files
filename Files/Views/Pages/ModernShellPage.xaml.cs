using Files.Filesystem;
using Files.Interacts;
using Files.UserControls;
using Files.View_Models;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Views.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ModernShellPage : Page, IShellPage
    {
        public ModernShellPage()
        {
            this.InitializeComponent();
            if (App.AppSettings.DrivesManager.ShowUserConsentOnInit)
            {
                App.AppSettings.DrivesManager.ShowUserConsentOnInit = false;
                DisplayFilesystemConsentDialog();
            }

            App.CurrentInstance = this as IShellPage;
            App.CurrentInstance.NavigationToolbar.PathControlDisplayText = "New tab";
            App.CurrentInstance.NavigationToolbar.CanGoBack = false;
            App.CurrentInstance.NavigationToolbar.CanGoForward = false;
        }

        Type IShellPage.CurrentPageType => ItemDisplayFrame.SourcePageType;

        INavigationToolbar IShellPage.NavigationToolbar => NavToolbar;

        INavigationControlItem IShellPage.SidebarSelectedItem { get => SidebarControl.SelectedSidebarItem; set => SidebarControl.SelectedSidebarItem = value; }

        Frame IShellPage.ContentFrame => ItemDisplayFrame;

        Interaction IShellPage.InteractionOperations => interactionOperation;

        ItemViewModel IShellPage.ViewModel => viewModel;

        BaseLayout IShellPage.ContentPage => GetContentOrNull();
        Control IShellPage.OperationsControl => null;

        private BaseLayout GetContentOrNull()
        {
            if ((ItemDisplayFrame.Content as BaseLayout) != null)
            {
                return ItemDisplayFrame.Content as BaseLayout;
            }
            else
            {
                return null;
            }
        }

        private async void DisplayFilesystemConsentDialog()
        {
            await App.ConsentDialogDisplay.ShowAsync(ContentDialogPlacement.Popup);
        }

        private string NavParams = null;

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            NavParams = eventArgs.Parameter.ToString();
        }

        private ItemViewModel viewModel = null;
        private Interaction interactionOperation = null;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel = new ItemViewModel();
            interactionOperation = new Interaction();

            string NavigationPath = ""; // path to navigate

            switch (NavParams)
            {
                case "Start":
                    ItemDisplayFrame.Navigate(typeof(YourHome), NavParams, new SuppressNavigationTransitionInfo());
                    SidebarControl.SelectedSidebarItem = App.sideBarItems[0];
                    break;

                case "New tab":
                    ItemDisplayFrame.Navigate(typeof(YourHome), NavParams, new SuppressNavigationTransitionInfo());
                    SidebarControl.SelectedSidebarItem = App.sideBarItems[0];
                    break;

                case "Desktop":
                    NavigationPath = App.AppSettings.DesktopPath;
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "Downloads":
                    NavigationPath = App.AppSettings.DownloadsPath;
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "Documents":
                    NavigationPath = App.AppSettings.DocumentsPath;
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "Pictures":
                    NavigationPath = App.AppSettings.PicturesPath;
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "Music":
                    NavigationPath = App.AppSettings.MusicPath;
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "Videos":
                    NavigationPath = App.AppSettings.VideosPath;
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "OneDrive":
                    NavigationPath = App.AppSettings.OneDrivePath;
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.OneDrivePath, StringComparison.OrdinalIgnoreCase));
                    break;

                default:
                    if (NavParams[0] >= 'A' && NavParams[0] <= 'Z' && NavParams[1] == ':')
                    {
                        NavigationPath = NavParams;
                        SidebarControl.SelectedSidebarItem = App.AppSettings.DrivesManager.Drives.First(x => x.Tag.ToString().Equals($"{NavParams[0]}:\\", StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        SidebarControl.SelectedSidebarItem = null;
                    }
                    break;
            }

            if (NavigationPath != "")
            {
                if (App.AppSettings.LayoutMode == 0) // List View
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), NavigationPath, new SuppressNavigationTransitionInfo());
                }
                else
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(PhotoAlbum), NavigationPath, new SuppressNavigationTransitionInfo());
                }
            }

            this.Loaded -= Page_Loaded;
        }

        private void ItemDisplayFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (ItemDisplayFrame.CurrentSourcePageType == typeof(GenericFileBrowser))
            {
                App.InteractionViewModel.IsPageTypeNotHome = true;
                // Reset DataGrid Rows that may be in "cut" command mode
                IEnumerable items = (ItemDisplayFrame.Content as GenericFileBrowser).AllView.ItemsSource;
                if (items == null)
                    return;
                foreach (ListedItem listedItem in items)
                {
                    FrameworkElement element = (ItemDisplayFrame.Content as GenericFileBrowser).AllView.Columns[0].GetCellContent(listedItem);
                    if (element != null)
                        element.Opacity = 1;
                }
            }
            else if (App.CurrentInstance.CurrentPageType == typeof(PhotoAlbum))
            {
                App.InteractionViewModel.IsPageTypeNotHome = true;
                // Reset Photo Grid items that may be in "cut" command mode
                foreach (ListedItem listedItem in (ItemDisplayFrame.Content as PhotoAlbum).FileList.Items)
                {
                    List<Grid> itemContentGrids = new List<Grid>();
                    GridViewItem gridViewItem = (ItemDisplayFrame.Content as PhotoAlbum).FileList.ContainerFromItem(listedItem) as GridViewItem;
                    if (gridViewItem == null)
                        return;
                    Interaction.FindChildren<Grid>(itemContentGrids, gridViewItem);
                    var imageOfItem = itemContentGrids.Find(x => x.Tag?.ToString() == "ItemImage");
                    imageOfItem.Opacity = 1;
                }
            }
            else if (App.CurrentInstance.CurrentPageType == typeof(YourHome))
            {
                App.InteractionViewModel.IsPageTypeNotHome = false;
            }
        }

        public void UpdateProgressFlyout(InteractionOperationType operationType, int amountComplete, int amountTotal)
        {
            this.FindName("ProgressFlyout");

            string operationText = null;
            switch (operationType)
            {
                case InteractionOperationType.PasteItems:
                    operationText = "Completing Paste";
                    break;

                case InteractionOperationType.DeleteItems:
                    operationText = "Deleting Items";
                    break;
            }
            ProgressFlyoutTextBlock.Text = operationText + " (" + amountComplete + "/" + amountTotal + ")" + "...";
            ProgressFlyoutProgressBar.Value = amountComplete;
            ProgressFlyoutProgressBar.Maximum = amountTotal;

            if (amountComplete == amountTotal)
            {
                UnloadObject(ProgressFlyout);
            }
        }

        private async void ModernShellPage_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var alt = Window.Current.CoreWindow.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
            var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            var tabInstance = App.CurrentInstance != null;

            switch (c: ctrl, s: shift, a: alt, t: tabInstance, k: e.Key)
            {
                case (true, true, false, true, VirtualKey.N): //ctrl + shift + n, new item
                    await App.AddItemDialogDisplay.ShowAsync();
                    break;

                case (false, true, false, true, VirtualKey.Delete): //shift + delete, PermanentDelete
                    if (!App.CurrentInstance.NavigationToolbar.IsEditModeEnabled)
                        App.InteractionViewModel.PermanentlyDelete = true;
                    App.CurrentInstance.InteractionOperations.DeleteItem_Click(null, null);
                    break;

                case (true, false, false, true, VirtualKey.C): //ctrl + c, copy
                    if (!App.CurrentInstance.NavigationToolbar.IsEditModeEnabled)
                        App.CurrentInstance.InteractionOperations.CopyItem_ClickAsync(null, null);
                    break;

                case (true, false, false, true, VirtualKey.V): //ctrl + v, paste
                    if (!App.CurrentInstance.NavigationToolbar.IsEditModeEnabled)
                        App.CurrentInstance.InteractionOperations.PasteItem_ClickAsync(null, null);
                    break;

                case (true, false, false, true, VirtualKey.X): //ctrl + x, cut
                    if (!App.CurrentInstance.NavigationToolbar.IsEditModeEnabled)
                        App.CurrentInstance.InteractionOperations.CutItem_Click(null, null);
                    break;

                case (true, false, false, true, VirtualKey.A): //ctrl + a, select all
                    if (!App.CurrentInstance.NavigationToolbar.IsEditModeEnabled)
                        App.CurrentInstance.InteractionOperations.SelectAllItems();
                    break;

                case (true, false, false, true, VirtualKey.N): //ctrl + n, new window
                    App.CurrentInstance.InteractionOperations.LaunchNewWindow();
                    break;

                case (true, false, false, true, VirtualKey.W): //ctrl + w, close tab
                    App.CurrentInstance.InteractionOperations.CloseTab();
                    break;

                case (true, false, false, true, VirtualKey.F4): //ctrl + F4, close tab
                    App.CurrentInstance.InteractionOperations.CloseTab();
                    break;

                case (false, false, false, true, VirtualKey.Delete): //delete, delete item
                    if (App.CurrentInstance.ContentPage.IsItemSelected && !App.CurrentInstance.ContentPage.isRenamingItem)
                        App.CurrentInstance.InteractionOperations.DeleteItem_Click(null, null);
                    break;

                case (false, false, false, true, VirtualKey.Space): //space, quick look
                    if (!App.CurrentInstance.NavigationToolbar.IsEditModeEnabled)
                    {
                        if ((App.CurrentInstance.ContentPage).IsQuickLookEnabled)
                        {
                            App.CurrentInstance.InteractionOperations.ToggleQuickLook();
                        }
                    }
                    break;

                case (false, false, true, true, VirtualKey.Left): //alt + back arrow, backward
                    NavigationActions.Back_Click(null, null);
                    break;

                case (false, false, true, true, VirtualKey.Right): //alt + right arrow, forward
                    NavigationActions.Forward_Click(null, null);
                    break;

                case (true, false, false, true, VirtualKey.R): //ctrl + r, refresh
                    NavigationActions.Refresh_Click(null, null);
                    break;
                    //case (true, false, false, true, VirtualKey.F): //ctrl + f, search box
                    //    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonTabView.SelectedIndex = 0;
                    //    break;
                    //case (true, false, false, true, VirtualKey.E): //ctrl + e, search box
                    //    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonTabView.SelectedIndex = 0;
                    //    break;
                    //case (false, false, true, true, VirtualKey.H):
                    //    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonTabView.SelectedIndex = 1;
                    //    break;
                    //case (false, false, true, true, VirtualKey.S):
                    //    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonTabView.SelectedIndex = 2;
                    //    break;
                    //case (false, false, true, true, VirtualKey.V):
                    //    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonTabView.SelectedIndex = 3;
                    //    break;
            };

            if (App.CurrentInstance.CurrentPageType == typeof(PhotoAlbum))
            {
                switch (e.Key)
                {
                    case VirtualKey.F2: //F2, rename
                        if ((App.CurrentInstance.ContentPage).SelectedItems.Count > 0)
                        {
                            App.CurrentInstance.InteractionOperations.RenameItem_Click(null, null);
                        }
                        break;
                }
            }
        }
    }

    public enum InteractionOperationType
    {
        PasteItems = 0,
        DeleteItems = 1,
    }

    public class PathBoxItem
    {
        public string Title { get; set; }
        public string Path { get; set; }
    }
}