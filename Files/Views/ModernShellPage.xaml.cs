using Files.Dialogs;
using Files.Filesystem;
using Files.Interacts;
using Files.UserControls;
using Files.UserControls.MultiTaskingControl;
using Files.View_Models;
using System;
using System.Linq;
using Windows.ApplicationModel.Resources.Core;
using Windows.Storage;
using Windows.System;
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
        public SettingsViewModel AppSettings => App.AppSettings;

        public ModernShellPage()
        {
            this.InitializeComponent();
            AppSettings.DrivesManager.PropertyChanged += DrivesManager_PropertyChanged;
            DisplayFilesystemConsentDialog();

            var flowDirectionSetting = ResourceContext.GetForCurrentView().QualifierValues["LayoutDirection"];

            if (flowDirectionSetting == "RTL")
            {
                FlowDirection = FlowDirection.RightToLeft;
            }

            App.CurrentInstance = this as IShellPage;
            App.CurrentInstance.NavigationToolbar.PathControlDisplayText = ResourceController.GetTranslation("NewTab");
            App.CurrentInstance.NavigationToolbar.CanGoBack = false;
            App.CurrentInstance.NavigationToolbar.CanGoForward = false;
        }

        private void DrivesManager_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ShowUserConsentOnInit")
            {
                DisplayFilesystemConsentDialog();
            }
        }

        Type IShellPage.CurrentPageType => ItemDisplayFrame.SourcePageType;

        INavigationToolbar IShellPage.NavigationToolbar => NavToolbar;

        INavigationControlItem IShellPage.SidebarSelectedItem { get => SidebarControl.SelectedSidebarItem; set => SidebarControl.SelectedSidebarItem = value; }

        Frame IShellPage.ContentFrame => ItemDisplayFrame;

        Interaction IShellPage.InteractionOperations => interactionOperation;

        ItemViewModel IShellPage.FilesystemViewModel => viewModel;

        BaseLayout IShellPage.ContentPage => GetContentOrNull();

        Control IShellPage.OperationsControl => null;

        StatusBarControl IShellPage.StatusBarControl => StatusBarControl;

        CurrentInstanceViewModel IShellPage.InstanceViewModel => instanceViewModel;

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
            if (AppSettings.DrivesManager.ShowUserConsentOnInit)
            {
                AppSettings.DrivesManager.ShowUserConsentOnInit = false;
                var consentDialogDisplay = new ConsentDialog();
                await consentDialogDisplay.ShowAsync(ContentDialogPlacement.Popup);
            }
        }

        private string NavParams = null;

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            NavParams = eventArgs.Parameter.ToString();
        }

        private ItemViewModel viewModel = null;
        private Interaction interactionOperation = null;
        private readonly CurrentInstanceViewModel instanceViewModel = new CurrentInstanceViewModel();

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel = new ItemViewModel();
            interactionOperation = new Interaction();

            string NavigationPath = ""; // path to navigate

            switch (NavParams)
            {
                case "Start":
                    ItemDisplayFrame.Navigate(typeof(YourHome), NavParams, new SuppressNavigationTransitionInfo());
                    SidebarControl.SelectedSidebarItem = MainPage.sideBarItems.FirstOrDefault();
                    break;

                case "Desktop":
                    NavigationPath = AppSettings.DesktopPath;
                    SidebarControl.SelectedSidebarItem = MainPage.sideBarItems.FirstOrDefault(x => x.Path.Equals(AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "Downloads":
                    NavigationPath = AppSettings.DownloadsPath;
                    SidebarControl.SelectedSidebarItem = MainPage.sideBarItems.FirstOrDefault(x => x.Path.Equals(AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "Documents":
                    NavigationPath = AppSettings.DocumentsPath;
                    SidebarControl.SelectedSidebarItem = MainPage.sideBarItems.FirstOrDefault(x => x.Path.Equals(AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "Pictures":
                    NavigationPath = AppSettings.PicturesPath;
                    SidebarControl.SelectedSidebarItem = MainPage.sideBarItems.FirstOrDefault(x => x.Path.Equals(AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "Music":
                    NavigationPath = AppSettings.MusicPath;
                    SidebarControl.SelectedSidebarItem = MainPage.sideBarItems.FirstOrDefault(x => x.Path.Equals(AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "Videos":
                    NavigationPath = AppSettings.VideosPath;
                    SidebarControl.SelectedSidebarItem = MainPage.sideBarItems.FirstOrDefault(x => x.Path.Equals(AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "RecycleBin":
                    NavigationPath = AppSettings.RecycleBinPath;
                    SidebarControl.SelectedSidebarItem = MainPage.sideBarItems.FirstOrDefault(x => x.Path.Equals(AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "OneDrive":
                    NavigationPath = AppSettings.OneDrivePath;
                    SidebarControl.SelectedSidebarItem = MainPage.sideBarItems.FirstOrDefault(x => x.Path.Equals(AppSettings.OneDrivePath, StringComparison.OrdinalIgnoreCase));
                    break;

                default:
                    if (NavParams == ResourceController.GetTranslation("NewTab"))
                    {
                        ItemDisplayFrame.Navigate(typeof(YourHome), NavParams, new SuppressNavigationTransitionInfo());
                        SidebarControl.SelectedSidebarItem = MainPage.sideBarItems[0];
                    }
                    else if (((NavParams[0] >= 'A' && NavParams[0] <= 'Z') || (NavParams[0] >= 'a' && NavParams[0] <= 'z'))
                        && NavParams[1] == ':')
                    {
                        NavigationPath = NavParams;
                        SidebarControl.SelectedSidebarItem = AppSettings.DrivesManager.Drives.FirstOrDefault(x => x.Path.ToString().Equals($"{NavParams[0]}:\\", StringComparison.OrdinalIgnoreCase));
                    }
                    else if (NavParams.StartsWith("\\\\?\\"))
                    {
                        NavigationPath = NavParams;
                        SidebarControl.SelectedSidebarItem = App.AppSettings.DrivesManager.Drives.FirstOrDefault(x => x.Path.ToString().Equals($"{System.IO.Path.GetPathRoot(NavParams)}", StringComparison.OrdinalIgnoreCase));
                    }
                    else if (NavParams.StartsWith(AppSettings.RecycleBinPath))
                    {
                        NavigationPath = NavParams;
                    }
                    else
                    {
                        SidebarControl.SelectedSidebarItem = null;
                    }
                    break;
            }

            if (NavigationPath != "")
            {
                App.CurrentInstance.ContentFrame.Navigate(AppSettings.GetLayoutType(), NavigationPath, new SuppressNavigationTransitionInfo());
            }

            this.Loaded -= Page_Loaded;
        }

        private void ItemDisplayFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (ItemDisplayFrame.CurrentSourcePageType == typeof(GenericFileBrowser)
                || ItemDisplayFrame.CurrentSourcePageType == typeof(GridViewBrowser))
            {
                // Reset DataGrid Rows that may be in "cut" command mode
                App.CurrentInstance.ContentPage.ResetItemOpacity();
            }
        }

        private async void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            var ctrl = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Control);
            var alt = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Menu);
            var shift = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
            var tabInstance = App.CurrentInstance.CurrentPageType == typeof(GenericFileBrowser)
                || App.CurrentInstance.CurrentPageType == typeof(GridViewBrowser);

            switch (c: ctrl, s: shift, a: alt, t: tabInstance, k: args.KeyboardAccelerator.Key)
            {
                case (true, true, false, true, VirtualKey.N): // ctrl + shift + n, new item
                    var addItemDialog = new AddItemDialog();
                    await addItemDialog.ShowAsync();
                    break;

                case (false, true, false, true, VirtualKey.Delete): // shift + delete, PermanentDelete
                    if (!App.CurrentInstance.NavigationToolbar.IsEditModeEnabled)
                        App.CurrentInstance.InteractionOperations.DeleteItem(StorageDeleteOption.PermanentDelete);
                    break;

                case (true, false, false, true, VirtualKey.C): // ctrl + c, copy
                    if (!App.CurrentInstance.NavigationToolbar.IsEditModeEnabled)
                        App.CurrentInstance.InteractionOperations.CopyItem_ClickAsync(null, null);
                    break;

                case (true, false, false, true, VirtualKey.V): // ctrl + v, paste
                    if (!App.CurrentInstance.NavigationToolbar.IsEditModeEnabled)
                        App.CurrentInstance.InteractionOperations.PasteItem_ClickAsync(null, null);
                    break;

                case (true, false, false, true, VirtualKey.X): // ctrl + x, cut
                    if (!App.CurrentInstance.NavigationToolbar.IsEditModeEnabled)
                        App.CurrentInstance.InteractionOperations.CutItem_Click(null, null);
                    break;

                case (true, false, false, true, VirtualKey.A): // ctrl + a, select all
                    if (!App.CurrentInstance.NavigationToolbar.IsEditModeEnabled)
                        App.CurrentInstance.InteractionOperations.SelectAllItems();
                    break;

                case (true, false, false, false, VirtualKey.N): // ctrl + n, new window
                    App.CurrentInstance.InteractionOperations.LaunchNewWindow();
                    break;

                case (true, false, false, false, VirtualKey.W): // ctrl + w, close tab
                    App.CurrentInstance.InteractionOperations.CloseTab();
                    break;

                case (true, false, false, false, VirtualKey.F4): // ctrl + F4, close tab
                    App.CurrentInstance.InteractionOperations.CloseTab();
                    break;

                case (true, false, false, true, VirtualKey.N): // ctrl + n, new window from layout mode
                    App.CurrentInstance.InteractionOperations.LaunchNewWindow();
                    break;

                case (true, false, false, true, VirtualKey.W): // ctrl + w, close tab from layout mode
                    App.CurrentInstance.InteractionOperations.CloseTab();
                    break;

                case (true, false, false, true, VirtualKey.F4): // ctrl + F4, close tab from layout mode
                    App.CurrentInstance.InteractionOperations.CloseTab();
                    break;

                case (false, false, false, true, VirtualKey.Delete): // delete, delete item
                    if (App.CurrentInstance.ContentPage.IsItemSelected && !App.CurrentInstance.ContentPage.isRenamingItem)
                        App.CurrentInstance.InteractionOperations.DeleteItem(StorageDeleteOption.Default);
                    break;

                case (false, false, false, true, VirtualKey.Space): // space, quick look
                    if (!App.CurrentInstance.NavigationToolbar.IsEditModeEnabled)
                    {
                        if ((App.CurrentInstance.ContentPage).IsQuickLookEnabled)
                        {
                            App.CurrentInstance.InteractionOperations.ToggleQuickLook();
                        }
                    }
                    break;

                case (false, false, true, true, VirtualKey.Left): // alt + back arrow, backward
                    NavigationActions.Back_Click(null, null);
                    break;

                case (false, false, true, true, VirtualKey.Right): // alt + right arrow, forward
                    NavigationActions.Forward_Click(null, null);
                    break;

                case (true, false, false, true, VirtualKey.R): // ctrl + r, refresh
                    NavigationActions.Refresh_Click(null, null);
                    break;

                case (false, false, true, true, VirtualKey.D): // alt + d, select address bar (english)
                case (true, false, false, true, VirtualKey.L): // ctrl + l, select address bar
                    App.CurrentInstance.NavigationToolbar.IsEditModeEnabled = true;
                    break;
            };

            if (App.CurrentInstance.CurrentPageType == typeof(GridViewBrowser))
            {
                switch (args.KeyboardAccelerator.Key)
                {
                    case VirtualKey.F2: //F2, rename
                        if (App.CurrentInstance.ContentPage.IsItemSelected)
                        {
                            App.CurrentInstance.InteractionOperations.RenameItem_Click(null, null);
                        }
                        break;
                }
            }
        }

        private void SmallWindowTitlebar_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SetTitleBar(SmallWindowTitlebar);
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