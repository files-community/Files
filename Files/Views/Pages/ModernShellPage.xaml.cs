using Files.Filesystem;
using Files.Interacts;
using Files.UserControls;
using Files.View_Models;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
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
            await App.consentDialog.ShowAsync(ContentDialogPlacement.Popup);
        }

        string NavParams = null;

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
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.DesktopPath, new SuppressNavigationTransitionInfo());
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase));
                    break;
                case "Downloads":
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.DownloadsPath, new SuppressNavigationTransitionInfo());
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase));
                    break;
                case "Documents":
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.DocumentsPath, new SuppressNavigationTransitionInfo());
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase));
                    break;
                case "Pictures":
                    ItemDisplayFrame.Navigate(typeof(PhotoAlbum), App.AppSettings.PicturesPath, new SuppressNavigationTransitionInfo());
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase));
                    break;
                case "Music":
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.MusicPath, new SuppressNavigationTransitionInfo());
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase));
                    break;
                case "Videos":
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.VideosPath, new SuppressNavigationTransitionInfo());
                    SidebarControl.SelectedSidebarItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase));
                    break;

                default:
                    if (NavParams[0] >= 'A' && NavParams[0] <= 'Z' && NavParams[1] == ':')
                    {
                        ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), NavParams, new SuppressNavigationTransitionInfo());
                        SidebarControl.SelectedSidebarItem = SettingsViewModel.foundDrives.First(x => x.tag.ToString().Equals($"{NavParams[0]}:\\", StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        SidebarControl.SelectedSidebarItem = null;
                    }
                    break;
            }

            this.Loaded -= Page_Loaded;
        }


        private void ItemDisplayFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (ItemDisplayFrame.CurrentSourcePageType == typeof(GenericFileBrowser))
            {
                // Reset DataGrid Rows that may be in "cut" command mode
                App.CurrentInstance.InteractionOperations.dataGridRows.Clear();
                Interaction.FindChildren<DataGridRow>(App.CurrentInstance.InteractionOperations.dataGridRows, (ItemDisplayFrame.Content as GenericFileBrowser).AllView);
                foreach (DataGridRow dataGridRow in App.CurrentInstance.InteractionOperations.dataGridRows)
                {
                    if ((ItemDisplayFrame.Content as GenericFileBrowser).AllView.Columns[0].GetCellContent(dataGridRow).Opacity < 1)
                    {
                        (ItemDisplayFrame.Content as GenericFileBrowser).AllView.Columns[0].GetCellContent(dataGridRow).Opacity = 1;
                    }
                }
            }
            //RibbonArea.Focus(FocusState.Programmatic);
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
                    await App.addItemDialog.ShowAsync();
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
                    if (App.CurrentInstance.ContentPage.IsItemSelected)
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
}

