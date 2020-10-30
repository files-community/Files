using Files.Commands;
using Files.Common;
using Files.Dialogs;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.UserControls;
using Files.View_Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;


namespace Files.Views.Pages
{
    public sealed partial class ModernShellPage : Page, IShellPage
    {
        public SettingsViewModel AppSettings => App.AppSettings;
        public bool IsCurrentInstance { get; set; } = false;
        public StatusBarControl BottomStatusStripControl => StatusBarControl;
        public Frame ContentFrame => ItemDisplayFrame;
        public Interaction InteractionOperations { get; private set; } = null;
        public ItemViewModel FilesystemViewModel { get; private set; } = null;
        public CurrentInstanceViewModel InstanceViewModel { get; } = new CurrentInstanceViewModel();
        public BaseLayout ContentPage => GetContentOrNull();
        public Control OperationsControl => null;
        public Type CurrentPageType => ItemDisplayFrame.SourcePageType;
        public INavigationControlItem SidebarSelectedItem { get => SidebarControl.SelectedSidebarItem; set => SidebarControl.SelectedSidebarItem = value; }
        public INavigationToolbar NavigationToolbar => NavToolbar;

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

            NavigationToolbar.EditModeEnabled += NavigationToolbar_EditModeEnabled;
            NavigationToolbar.QuerySubmitted += NavigationToolbar_QuerySubmitted;

            if ((NavigationToolbar as ModernNavigationToolbar) != null)
            {
                (NavigationToolbar as ModernNavigationToolbar).ToolbarFlyoutItemInvoked += ModernShellPage_NavigationRequested;
                (NavigationToolbar as ModernNavigationToolbar).ToolbarPathItemInvoked += ModernShellPage_NavigationRequested;
                (NavigationToolbar as ModernNavigationToolbar).ToolbarFlyoutOpened += ModernShellPage_ToolbarFlyoutOpened;
                (NavigationToolbar as ModernNavigationToolbar).ToolbarPathItemLoaded += ModernShellPage_ToolbarPathItemLoaded;
                (NavigationToolbar as ModernNavigationToolbar).AddressBarTextEntered += ModernShellPage_AddressBarTextEntered;
                (NavigationToolbar as ModernNavigationToolbar).PathBoxItemDropped += ModernShellPage_PathBoxItemDropped;
            }

            NavigationToolbar.ItemDraggedOverPathItem += ModernShellPage_NavigationRequested;
            NavigationToolbar.PathControlDisplayText = ResourceController.GetTranslation("NewTab");
            NavigationToolbar.CanGoBack = false;
            NavigationToolbar.CanGoForward = false;

            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
            SystemNavigationManager.GetForCurrentView().BackRequested += ModernShellPage_BackRequested;
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            Clipboard_ContentChanged(null, null);
        }

        private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            if (args.CurrentPoint.Properties.IsXButton1Pressed)
            {
                Back_Click(null, null);
            }
            else if (args.CurrentPoint.Properties.IsXButton2Pressed)
            {
                Forward_Click(null, null);
            }
        }

        private void ModernShellPage_PathBoxItemDropped(object sender, PathBoxItemDroppedEventArgs e)
        {
            InteractionOperations.ItemOperationCommands.PasteItemWithStatus(e.Package, e.Path, e.AcceptedOperation);
        }

        private void ModernShellPage_AddressBarTextEntered(object sender, AddressBarTextEnteredEventArgs e)
        {
            SetAddressBarSuggestions(e.AddressBarTextField);
        }

        private async void SetAddressBarSuggestions(AutoSuggestBox sender, int maxSuggestions = 7)
        {
            var mNavToolbar = (NavigationToolbar as ModernNavigationToolbar);
            if (mNavToolbar != null)
            {
                try
                {
                    IList<ListedItem> suggestions = null;
                    var expandedPath = StorageFileExtensions.GetPathWithoutEnvironmentVariable(sender.Text);
                    var folderPath = Path.GetDirectoryName(expandedPath) ?? expandedPath;
                    var folder = await FilesystemViewModel.GetFolderWithPathFromPathAsync(folderPath);
                    var currPath = await folder.GetFoldersWithPathAsync(Path.GetFileName(expandedPath), (uint)maxSuggestions);
                    if (currPath.Count() >= maxSuggestions)
                    {
                        suggestions = currPath.Select(x => new ListedItem(null) { ItemPath = x.Path, ItemName = x.Folder.Name }).ToList();
                    }
                    else if (currPath.Any())
                    {
                        var subPath = await currPath.First().GetFoldersWithPathAsync((uint)(maxSuggestions - currPath.Count()));
                        suggestions = currPath.Select(x => new ListedItem(null) { ItemPath = x.Path, ItemName = x.Folder.Name }).Concat(
                            subPath.Select(x => new ListedItem(null) { ItemPath = x.Path, ItemName = Path.Combine(currPath.First().Folder.Name, x.Folder.Name) })).ToList();
                    }
                    else
                    {
                        suggestions = new List<ListedItem>() { new ListedItem(null) {
                        ItemPath = FilesystemViewModel.WorkingDirectory,
                        ItemName = ResourceController.GetTranslation("NavigationToolbarVisiblePathNoResults") } };
                    }


                    // NavigationBarSuggestions becoming empty causes flickering of the suggestion box
                    // Here we check whether at least an element is in common between old and new list
                    if (!mNavToolbar.NavigationBarSuggestions.IntersectBy(suggestions, x => x.ItemName).Any())
                    {
                        // No elemets in common, update the list in-place
                        for (int si = 0; si < suggestions.Count; si++)
                        {
                            if (si < mNavToolbar.NavigationBarSuggestions.Count)
                            {
                                mNavToolbar.NavigationBarSuggestions[si].ItemName = suggestions[si].ItemName;
                                mNavToolbar.NavigationBarSuggestions[si].ItemPath = suggestions[si].ItemPath;
                            }
                            else
                            {
                                mNavToolbar.NavigationBarSuggestions.Add(suggestions[si]);
                            }
                        }
                        while (mNavToolbar.NavigationBarSuggestions.Count > suggestions.Count)
                        {
                            mNavToolbar.NavigationBarSuggestions.RemoveAt(mNavToolbar.NavigationBarSuggestions.Count - 1);
                        }
                    }
                    else
                    {
                        // At least an element in common, show animation
                        foreach (var s in mNavToolbar.NavigationBarSuggestions.ExceptBy(suggestions, x => x.ItemName).ToList())
                        {
                            mNavToolbar.NavigationBarSuggestions.Remove(s);
                        }
                        foreach (var s in suggestions.ExceptBy(mNavToolbar.NavigationBarSuggestions, x => x.ItemName).ToList())
                        {
                            mNavToolbar.NavigationBarSuggestions.Insert(suggestions.IndexOf(s), s);
                        }
                    }
                }
                catch
                {
                    mNavToolbar.NavigationBarSuggestions.Clear();
                    mNavToolbar.NavigationBarSuggestions.Add(new ListedItem(null)
                    {
                        ItemPath = FilesystemViewModel.WorkingDirectory,
                        ItemName = ResourceController.GetTranslation("NavigationToolbarVisiblePathNoResults")
                    });
                }
            }
        }

        private async void ModernShellPage_ToolbarPathItemLoaded(object sender, ToolbarPathItemLoadedEventArgs e)
        {
            await SetPathBoxDropDownFlyout(e.OpenedFlyout, e.Item);
        }

        private async void ModernShellPage_ToolbarFlyoutOpened(object sender, ToolbarFlyoutOpenedEventArgs e)
        {
            await SetPathBoxDropDownFlyout(e.OpenedFlyout, (e.OpenedFlyout.Target as FontIcon).DataContext as PathBoxItem);
        }

        private async Task SetPathBoxDropDownFlyout(MenuFlyout flyout, PathBoxItem pathItem)
        {
            var nextPathItemTitle = NavigationToolbar.PathComponents
                [NavigationToolbar.PathComponents.IndexOf(pathItem) + 1].Title;
            IList<StorageFolderWithPath> childFolders = new List<StorageFolderWithPath>();

            try
            {
                var folder = await FilesystemViewModel.GetFolderWithPathFromPathAsync(pathItem.Path);
                childFolders = await folder.GetFoldersWithPathAsync(string.Empty);
            }
            catch
            {
                // Do nothing.
            }
            finally
            {
                flyout.Items?.Clear();
            }

            if (childFolders.Count == 0)
            {
                var flyoutItem = new MenuFlyoutItem
                {
                    Icon = new FontIcon { FontFamily = Application.Current.Resources["FluentUIGlyphs"] as FontFamily, Glyph = "\uEC17" },
                    Text = ResourceController.GetTranslation("SubDirectoryAccessDenied"),
                    //Foreground = (SolidColorBrush)Application.Current.Resources["SystemControlErrorTextForegroundBrush"],
                    FontSize = 12
                };
                flyout.Items.Add(flyoutItem);
                return;
            }

            var boldFontWeight = new FontWeight { Weight = 800 };
            var normalFontWeight = new FontWeight { Weight = 400 };
            var customGlyphFamily = Application.Current.Resources["FluentUIGlyphs"] as FontFamily;

            var workingPath = NavigationToolbar.PathComponents
                    [NavigationToolbar.PathComponents.Count - 1].
                    Path.TrimEnd(Path.DirectorySeparatorChar);
            foreach (var childFolder in childFolders)
            {
                var isPathItemFocused = childFolder.Item.Name == nextPathItemTitle;

                var flyoutItem = new MenuFlyoutItem
                {
                    Icon = new FontIcon
                    {
                        FontFamily = customGlyphFamily,
                        Glyph = "\uEA5A",
                        FontWeight = isPathItemFocused ? boldFontWeight : normalFontWeight
                    },
                    Text = childFolder.Item.Name,
                    FontSize = 12,
                    FontWeight = isPathItemFocused ? boldFontWeight : normalFontWeight
                };

                if (workingPath != childFolder.Path)
                {
                    flyoutItem.Click += (sender, args) => {
                        ContentFrame.Navigate(AppSettings.GetLayoutType(), new NavigationArguments() { NavPathParam = childFolder.Path, AssociatedTabInstance = this, ServiceConnection = Connection });
                    };
                }

                flyout.Items.Add(flyoutItem);
            }
        }

        private void ModernShellPage_NavigationRequested(object sender, PathNavigationEventArgs e)
        {
            ContentFrame.Navigate(e.LayoutType, new NavigationArguments() { NavPathParam = e.ItemPath, AssociatedTabInstance = this, ServiceConnection = Connection });
        }

        private void NavigationToolbar_QuerySubmitted(object sender, ToolbarQuerySubmittedEventArgs e)
        {
            CheckPathInput(FilesystemViewModel, e.QueryText,
                            NavigationToolbar.PathComponents[NavigationToolbar.PathComponents.Count - 1].Path);
        }

        public async void CheckPathInput(ItemViewModel instance, string currentInput, string currentSelectedPath)
        {
            if (currentSelectedPath == currentInput) return;

            if (currentInput != instance.WorkingDirectory || ContentFrame.CurrentSourcePageType == typeof(YourHome))
            {
                if (currentInput.Equals("Home", StringComparison.OrdinalIgnoreCase) || currentInput.Equals(ResourceController.GetTranslation("NewTab"), StringComparison.OrdinalIgnoreCase))
                {
                    await FilesystemViewModel.SetWorkingDirectory(ResourceController.GetTranslation("NewTab"));
                    ContentFrame.Navigate(typeof(YourHome), new NavigationArguments() { NavPathParam = ResourceController.GetTranslation("NewTab"), AssociatedTabInstance = this, ServiceConnection = Connection }, new SuppressNavigationTransitionInfo());
                }
                else
                {
                    var workingDir = string.IsNullOrEmpty(FilesystemViewModel.WorkingDirectory)
                        ? AppSettings.HomePath
                        : FilesystemViewModel.WorkingDirectory;

                    currentInput = StorageFileExtensions.GetPathWithoutEnvironmentVariable(currentInput);
                    if (currentSelectedPath == currentInput) return;
                    var item = await DrivesManager.GetRootFromPath(currentInput);

                    try
                    {
                        var pathToNavigate = (await StorageFileExtensions.GetFolderWithPathFromPathAsync(currentInput, item)).Path;
                        ContentFrame.Navigate(AppSettings.GetLayoutType(), new NavigationArguments() { NavPathParam = pathToNavigate, AssociatedTabInstance = this, ServiceConnection = Connection }); // navigate to folder
                    }
                    catch (Exception) // Not a folder or inaccessible
                    {
                        try
                        {
                            var pathToInvoke = (await StorageFileExtensions.GetFileWithPathFromPathAsync(currentInput, item)).Path;
                            await InteractionOperations.InvokeWin32Component(pathToInvoke);
                        }
                        catch (Exception ex) // Not a file or not accessible
                        {
                            // Launch terminal application if possible
                            foreach (var terminal in AppSettings.TerminalController.Model.Terminals)
                            {
                                if (terminal.Path.Equals(currentInput, StringComparison.OrdinalIgnoreCase) || terminal.Path.Equals(currentInput + ".exe", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (Connection != null)
                                    {
                                        var value = new ValueSet
                                        {
                                            { "WorkingDirectory", workingDir },
                                            { "Application", terminal.Path },
                                            { "Arguments", string.Format(terminal.Arguments,
                                            Helpers.PathNormalization.NormalizePath(FilesystemViewModel.WorkingDirectory)) }
                                        };
                                        await Connection.SendMessageAsync(value);
                                    }
                                    return;
                                }
                            }

                            try
                            {
                                if (!await Launcher.LaunchUriAsync(new Uri(currentInput)))
                                {
                                    throw new Exception();
                                }
                            }
                            catch
                            {
                                await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("InvalidItemDialogTitle"),
                                    string.Format(ResourceController.GetTranslation("InvalidItemDialogContent"), Environment.NewLine, ex.Message));
                            }
                        }
                    }
                }

                NavigationToolbar.PathControlDisplayText = FilesystemViewModel.WorkingDirectory;
            }
        }

        private void NavigationToolbar_EditModeEnabled(object sender, EventArgs e)
        {
            if (NavigationToolbar is ModernNavigationToolbar)
            {
                var mNavToolbar = NavigationToolbar as ModernNavigationToolbar;
                mNavToolbar.ManualEntryBoxLoaded = true;
                mNavToolbar.ClickablePathLoaded = false;
                mNavToolbar.PathText = string.IsNullOrEmpty(FilesystemViewModel.WorkingDirectory)
                    ? AppSettings.HomePath
                    : FilesystemViewModel.WorkingDirectory;
            }
        }

        private void ModernShellPage_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (IsCurrentInstance)
            {
                if (ContentFrame.CanGoBack)
                {
                    e.Handled = true;
                    Back_Click(null, null);
                }
                else
                {
                    e.Handled = false;
                }
            }
        }

        private void DrivesManager_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ShowUserConsentOnInit")
            {
                DisplayFilesystemConsentDialog();
            }
        }

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


        AppServiceConnection Connection = null;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Connection = new AppServiceConnection();
            FilesystemViewModel = new ItemViewModel(this, Connection);
            InteractionOperations = new Interaction(this, Connection);
            App.Current.Suspending += Current_Suspending;
            FilesystemViewModel.WorkingDirectoryModified += ViewModel_WorkingDirectoryModified;
            string NavigationPath = ""; // path to navigate

            switch (NavParams)
            {
                case "Start":
                    ItemDisplayFrame.Navigate(typeof(YourHome), new NavigationArguments() { NavPathParam = NavParams, AssociatedTabInstance = this, ServiceConnection = Connection }, new SuppressNavigationTransitionInfo());
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
                        ItemDisplayFrame.Navigate(typeof(YourHome), new NavigationArguments() { NavPathParam = NavParams, AssociatedTabInstance = this, ServiceConnection = Connection }, new SuppressNavigationTransitionInfo());
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
                ContentFrame.Navigate(AppSettings.GetLayoutType(), new NavigationArguments() { NavPathParam = NavigationPath, AssociatedTabInstance = this, ServiceConnection = Connection }, new SuppressNavigationTransitionInfo());
            }

            this.Loaded -= Page_Loaded;
        }

        private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            if (Connection != null)
            {
                Connection.Dispose();
                Connection = null;
            }
        }

        private void ViewModel_WorkingDirectoryModified(object sender, WorkingDirectoryModifiedEventArgs e)
        {
            string value = e.Path;

            INavigationControlItem item = null;
            List<INavigationControlItem> sidebarItems = MainPage.sideBarItems.Where(x => !string.IsNullOrWhiteSpace(x.Path)).ToList();

            item = sidebarItems.FirstOrDefault(x => x.Path.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => x.Path.Equals(value + "\\", StringComparison.OrdinalIgnoreCase));
            }
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => value.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase));
            }
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => x.Path.Equals(Path.GetPathRoot(value), StringComparison.OrdinalIgnoreCase));
            }

            if (SidebarSelectedItem != item)
            {
                SidebarSelectedItem = item;
            }
        }

        private void ItemDisplayFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (ItemDisplayFrame.CurrentSourcePageType == typeof(GenericFileBrowser)
                || ItemDisplayFrame.CurrentSourcePageType == typeof(GridViewBrowser))
            {
                // Reset DataGrid Rows that may be in "cut" command mode
                ContentPage.ResetItemOpacity();
            }
        }

        public void Clipboard_ContentChanged(object sender, object e)
        {
            try
            {
                if (IsCurrentInstance)
                {
                    DataPackageView packageView = Clipboard.GetContent();
                    if (packageView.Contains(StandardDataFormats.StorageItems)
                        && CurrentPageType != typeof(YourHome)
                        && !FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath))
                    {
                        App.InteractionViewModel.IsPasteEnabled = true;
                    }
                    else
                    {
                        App.InteractionViewModel.IsPasteEnabled = false;
                    }
                }
                else
                {
                    App.InteractionViewModel.IsPasteEnabled = false;
                }
            }
            catch (Exception)
            {
                App.InteractionViewModel.IsPasteEnabled = false;
            }
        }

        private async void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            var ctrl = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Control);
            var alt = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Menu);
            var shift = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
            var tabInstance = CurrentPageType == typeof(GenericFileBrowser)
                || CurrentPageType == typeof(GridViewBrowser);

            switch (c: ctrl, s: shift, a: alt, t: tabInstance, k: args.KeyboardAccelerator.Key)
            {
                case (true, true, false, true, VirtualKey.N): // ctrl + shift + n, new item
                    if (InstanceViewModel.CanCreateFileInPage)
                    {
                        var addItemDialog = new AddItemDialog();
                        await addItemDialog.ShowAsync();
                        if (addItemDialog.ResultType != AddItemType.Cancel)
                        {
                            InteractionOperations.CreateFileFromDialogResultType(addItemDialog.ResultType);
                        }
                    }
                    break;

                case (false, true, false, true, VirtualKey.Delete): // shift + delete, PermanentDelete
                    if (!NavigationToolbar.IsEditModeEnabled)
                        InteractionOperations.ItemOperationCommands.DeleteItemWithStatus(StorageDeleteOption.PermanentDelete);
                    break;

                case (true, false, false, true, VirtualKey.C): // ctrl + c, copy
                    if (!NavigationToolbar.IsEditModeEnabled && !ContentPage.isRenamingItem)
                        InteractionOperations.CopyItem_ClickAsync(null, null);
                    break;

                case (true, false, false, true, VirtualKey.V): // ctrl + v, paste
                    if (!NavigationToolbar.IsEditModeEnabled && !ContentPage.isRenamingItem)
                        InteractionOperations.PasteItem_ClickAsync(null, null);
                    break;

                case (true, false, false, true, VirtualKey.X): // ctrl + x, cut
                    if (!NavigationToolbar.IsEditModeEnabled && !ContentPage.isRenamingItem)
                        InteractionOperations.CutItem_Click(null, null);
                    break;

                case (true, false, false, true, VirtualKey.A): // ctrl + a, select all
                    if (!NavigationToolbar.IsEditModeEnabled && !ContentPage.isRenamingItem)
                        InteractionOperations.SelectAllItems();
                    break;

                case (true, false, false, false, VirtualKey.N): // ctrl + n, new window
                    InteractionOperations.LaunchNewWindow();
                    break;

                case (true, false, false, false, VirtualKey.W): // ctrl + w, close tab
                    InteractionOperations.CloseTab();
                    break;

                case (true, false, false, false, VirtualKey.F4): // ctrl + F4, close tab
                    InteractionOperations.CloseTab();
                    break;

                case (true, false, false, true, VirtualKey.N): // ctrl + n, new window from layout mode
                    InteractionOperations.LaunchNewWindow();
                    break;

                case (true, false, false, true, VirtualKey.W): // ctrl + w, close tab from layout mode
                    InteractionOperations.CloseTab();
                    break;

                case (true, false, false, true, VirtualKey.F4): // ctrl + F4, close tab from layout mode
                    InteractionOperations.CloseTab();
                    break;

                case (false, false, false, true, VirtualKey.Delete): // delete, delete item
                    if (ContentPage.IsItemSelected && !ContentPage.isRenamingItem)
                        InteractionOperations.ItemOperationCommands.DeleteItemWithStatus(StorageDeleteOption.Default);
                    break;

                case (false, false, false, true, VirtualKey.Space): // space, quick look
                    if (!NavigationToolbar.IsEditModeEnabled)
                    {
                        if ((ContentPage).IsQuickLookEnabled)
                        {
                            InteractionOperations.ToggleQuickLook();
                        }
                    }
                    break;

                case (true, false, false, true, VirtualKey.R): // ctrl + r, refresh
                    Refresh_Click(null, null);
                    break;

                case (false, false, true, true, VirtualKey.D): // alt + d, select address bar (english)
                case (true, false, false, true, VirtualKey.L): // ctrl + l, select address bar
                    NavigationToolbar.IsEditModeEnabled = true;
                    break;
            };

            if (CurrentPageType == typeof(GridViewBrowser))
            {
                switch (args.KeyboardAccelerator.Key)
                {
                    case VirtualKey.F2: //F2, rename
                        if (ContentPage.IsItemSelected)
                        {
                            InteractionOperations.RenameItem_Click(null, null);
                        }
                        break;
                }
            }
        }

        public async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            NavigationToolbar.CanRefresh = false;
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var ContentOwnedViewModelInstance = FilesystemViewModel;
                ContentOwnedViewModelInstance.RefreshItems();
            });
        }

        public void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationToolbar.CanGoBack = false;
            Frame instanceContentFrame = ContentFrame;
            if (instanceContentFrame.CanGoBack)
            {
                FilesystemViewModel.CancelLoadAndClearFiles();
                var previousSourcePageType = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].SourcePageType;

                SelectSidebarItemFromPath(previousSourcePageType);
                instanceContentFrame.GoBack();
            }
        }

        public async void Forward_Click(object sender, RoutedEventArgs e)
        {
            NavigationToolbar.CanGoForward = false;
            Frame instanceContentFrame = ContentFrame;

            if (instanceContentFrame.CanGoForward)
            {
                FilesystemViewModel.CancelLoadAndClearFiles();
                var incomingSourcePageType = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].SourcePageType;
                var Parameter = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].Parameter;
                SelectSidebarItemFromPath(incomingSourcePageType);
                await FilesystemViewModel.SetWorkingDirectory(Parameter.ToString());
                instanceContentFrame.GoForward();
            }
        }

        public void Up_Click(object sender, RoutedEventArgs e)
        {
            NavigationToolbar.CanNavigateToParent = false;
            Frame instanceContentFrame = ContentFrame;
            FilesystemViewModel.CancelLoadAndClearFiles();
            var instance = FilesystemViewModel;
            string parentDirectoryOfPath;
            // Check that there isn't a slash at the end
            if ((instance.WorkingDirectory.Count() - 1) - instance.WorkingDirectory.LastIndexOf("\\") > 0)
            {
                parentDirectoryOfPath = instance.WorkingDirectory.Remove(instance.WorkingDirectory.LastIndexOf("\\"));
            }
            else  // Slash found at end
            {
                var currentPathWithoutEndingSlash = instance.WorkingDirectory.Remove(instance.WorkingDirectory.LastIndexOf("\\"));
                parentDirectoryOfPath = currentPathWithoutEndingSlash.Remove(currentPathWithoutEndingSlash.LastIndexOf("\\"));
            }

            SelectSidebarItemFromPath();
            instanceContentFrame.Navigate(CurrentPageType, new NavigationArguments() { NavPathParam = parentDirectoryOfPath, AssociatedTabInstance = this, ServiceConnection = Connection }, new SuppressNavigationTransitionInfo());
        }

        private void SelectSidebarItemFromPath(Type incomingSourcePageType = null)
        {
            if (incomingSourcePageType == typeof(YourHome) && incomingSourcePageType != null)
            {
                SidebarSelectedItem = MainPage.sideBarItems.First(x => x.Path.Equals("Home"));
                NavigationToolbar.PathControlDisplayText = ResourceController.GetTranslation("NewTab");
            }
        }

        private void SmallWindowTitlebar_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SetTitleBar(SmallWindowTitlebar);
        }

        public void Dispose()
        {
            Window.Current.CoreWindow.PointerPressed -= CoreWindow_PointerPressed;
            FilesystemViewModel.WorkingDirectoryModified -= ViewModel_WorkingDirectoryModified;
            SystemNavigationManager.GetForCurrentView().BackRequested -= ModernShellPage_BackRequested;
            Clipboard.ContentChanged -= Clipboard_ContentChanged;
            App.Current.Suspending -= Current_Suspending;
            AppSettings.DrivesManager.PropertyChanged -= DrivesManager_PropertyChanged;
            NavigationToolbar.EditModeEnabled -= NavigationToolbar_EditModeEnabled;
            NavigationToolbar.QuerySubmitted -= NavigationToolbar_QuerySubmitted;

            if ((NavigationToolbar as ModernNavigationToolbar) != null)
            {
                (NavigationToolbar as ModernNavigationToolbar).ToolbarFlyoutItemInvoked -= ModernShellPage_NavigationRequested;
                (NavigationToolbar as ModernNavigationToolbar).ToolbarPathItemInvoked -= ModernShellPage_NavigationRequested;
                (NavigationToolbar as ModernNavigationToolbar).ToolbarFlyoutOpened -= ModernShellPage_ToolbarFlyoutOpened;
                (NavigationToolbar as ModernNavigationToolbar).ToolbarPathItemLoaded -= ModernShellPage_ToolbarPathItemLoaded;
                (NavigationToolbar as ModernNavigationToolbar).AddressBarTextEntered -= ModernShellPage_AddressBarTextEntered;
                (NavigationToolbar as ModernNavigationToolbar).PathBoxItemDropped -= ModernShellPage_PathBoxItemDropped;
            }

            NavigationToolbar.ItemDraggedOverPathItem -= ModernShellPage_NavigationRequested;
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

    public class NavigationArguments
    {
        public string NavPathParam { get; set; } = null;
        public IShellPage AssociatedTabInstance { get; set; }
        public AppServiceConnection ServiceConnection { get; set; }
    }
}