using Files.Common;
using Files.Dialogs;
using Files.Filesystem;
using Files.Filesystem.FilesystemHistory;
using Files.Filesystem.Search;
using Files.Helpers;
using Files.Interacts;
using Files.UserControls;
using Files.ViewModels;
using Files.Views.LayoutModes;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Core;
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

namespace Files.Views
{
    public sealed partial class ModernShellPage : Page, IShellPage, INotifyPropertyChanged
    {
        private readonly StorageHistoryHelpers storageHistoryHelpers;

        private readonly IFilesystemHelpers filesystemHelpers;
        public SettingsViewModel AppSettings => App.AppSettings;
        public bool IsCurrentInstance { get; set; } = false;
        public StatusBarControl BottomStatusStripControl => StatusBarControl;
        public Frame ContentFrame => ItemDisplayFrame;
        private Interaction interactionOperations = null;

        public Interaction InteractionOperations
        {
            get
            {
                return interactionOperations;
            }
            private set
            {
                if (interactionOperations != value)
                {
                    interactionOperations = value;
                    NotifyPropertyChanged("InteractionOperations");
                }
            }
        }

        public ItemViewModel FilesystemViewModel { get; private set; } = null;
        public CurrentInstanceViewModel InstanceViewModel { get; }
        private BaseLayout contentPage = null;

        public BaseLayout ContentPage
        {
            get
            {
                return contentPage;
            }
            set
            {
                if (value != contentPage)
                {
                    contentPage = value;
                    NotifyPropertyChanged("ContentPage");
                }
            }
        }

        public bool IsPageMainPane
        {
            get { return (bool)GetValue(IsPageMainPaneProperty); }
            set { SetValue(IsPageMainPaneProperty, value); }
        }

        public static readonly DependencyProperty IsPageMainPaneProperty =
            DependencyProperty.Register("IsPageMainPane", typeof(bool), typeof(ModernShellPage), new PropertyMetadata(true));

        public SolidColorBrush CurrentInstanceBorderBrush
        {
            get { return (SolidColorBrush)GetValue(CurrentInstanceBorderBrushProperty); }
            set { SetValue(CurrentInstanceBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty CurrentInstanceBorderBrushProperty =
            DependencyProperty.Register("CurrentInstanceBorderBrush", typeof(SolidColorBrush), typeof(ModernShellPage), new PropertyMetadata(null));

        public GridLength SidebarWidth
        {
            get
            {
                return IsPageMainPane ? AppSettings.SidebarWidth : new GridLength(0);
            }
            set
            {
                if (IsPageMainPane && AppSettings.SidebarWidth != value)
                {
                    AppSettings.SidebarWidth = value;
                    NotifyPropertyChanged("SidebarWidth");
                }
            }
        }

        public Control OperationsControl => null;
        public Type CurrentPageType => ItemDisplayFrame.SourcePageType;
        public INavigationControlItem SidebarSelectedItem
        {
            get => SidebarControl?.SelectedSidebarItem;
            set
            {
                if (SidebarControl != null)
                {
                    SidebarControl.SelectedSidebarItem = value;
                }
            }
        }
        public INavigationToolbar NavigationToolbar => NavToolbar;

        public ModernShellPage()
        {
            InitializeComponent();

            InstanceViewModel = new CurrentInstanceViewModel(this);
            filesystemHelpers = new FilesystemHelpers(this, App.CancellationToken);
            storageHistoryHelpers = new StorageHistoryHelpers(new StorageHistoryOperations(this, App.CancellationToken));

            AppSettings.DrivesManager.PropertyChanged += DrivesManager_PropertyChanged;
            AppSettings.PropertyChanged += AppSettings_PropertyChanged;
            DisplayFilesystemConsentDialog();

            var flowDirectionSetting = ResourceContext.GetForCurrentView().QualifierValues["LayoutDirection"];

            if (flowDirectionSetting == "RTL")
            {
                FlowDirection = FlowDirection.RightToLeft;
            }

            NavigationToolbar.EditModeEnabled += NavigationToolbar_EditModeEnabled;
            NavigationToolbar.PathBoxQuerySubmitted += NavigationToolbar_QuerySubmitted;
            NavigationToolbar.SearchQuerySubmitted += ModernShellPage_SearchQuerySubmitted;
            NavigationToolbar.SearchTextChanged += ModernShellPage_SearchTextChanged;
            NavigationToolbar.SearchSuggestionChosen += ModernShellPage_SearchSuggestionChosen;
            NavigationToolbar.BackRequested += ModernShellPage_BackNavRequested;
            NavigationToolbar.ForwardRequested += ModernShellPage_ForwardNavRequested;
            NavigationToolbar.UpRequested += ModernShellPage_UpNavRequested;
            NavigationToolbar.RefreshRequested += ModernShellPage_RefreshRequested;
            NavigationToolbar.ItemDraggedOverPathItem += ModernShellPage_NavigationRequested;
            NavigationToolbar.PathControlDisplayText = "NewTab".GetLocalized();
            NavigationToolbar.CanGoBack = false;
            NavigationToolbar.CanGoForward = false;

            if (NavigationToolbar is NavigationToolbar navToolbar)
            {
                navToolbar.ToolbarPathItemInvoked += ModernShellPage_NavigationRequested;
                navToolbar.ToolbarFlyoutOpened += ModernShellPage_ToolbarFlyoutOpened;
                navToolbar.ToolbarPathItemLoaded += ModernShellPage_ToolbarPathItemLoaded;
                navToolbar.AddressBarTextEntered += ModernShellPage_AddressBarTextEntered;
                navToolbar.PathBoxItemDropped += ModernShellPage_PathBoxItemDropped;
            }

            InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated += AppSettings_SortDirectionPreferenceUpdated;
            InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated += AppSettings_SortOptionPreferenceUpdated;

            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
            SystemNavigationManager.GetForCurrentView().BackRequested += ModernShellPage_BackRequested;
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            Clipboard_ContentChanged(null, null);
        }

        private void AppSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppSettings.SidebarWidth))
            {
                NotifyPropertyChanged(nameof(SidebarWidth));
            }
        }

        private async void ModernShellPage_SearchSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var invokedItem = (args.SelectedItem as ListedItem);
            if (invokedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
            {
                ContentFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(invokedItem.ItemPath), new NavigationArguments()
                {
                    NavPathParam = invokedItem.ItemPath,
                    AssociatedTabInstance = this
                });
            }
            else
            {
                // TODO: Add fancy file launch options similar to Interactions.cs OpenSelectedItems()
                await InteractionOperations.InvokeWin32ComponentAsync(invokedItem.ItemPath);
            }
        }

        private async void ModernShellPage_SearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (!string.IsNullOrWhiteSpace(sender.Text))
                {
                    sender.ItemsSource = await FolderSearch.SearchForUserQueryTextAsync(sender.Text, FilesystemViewModel.WorkingDirectory, this);
                }
                else
                {
                    sender.ItemsSource = null;
                }
            }
        }

        private async void ModernShellPage_SearchQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion == null && !string.IsNullOrWhiteSpace(args.QueryText))
            {
                FilesystemViewModel.IsLoadingItems = true;
                ContentFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(FilesystemViewModel.WorkingDirectory), new NavigationArguments()
                {
                    AssociatedTabInstance = this,
                    IsSearchResultPage = true,
                    SearchPathParam = FilesystemViewModel.WorkingDirectory,
                    SearchResults = await FolderSearch.SearchForUserQueryTextAsync(args.QueryText, FilesystemViewModel.WorkingDirectory, this, -1)
                });
                FilesystemViewModel.IsLoadingItems = false;
            }
        }

        private void ModernShellPage_RefreshRequested(object sender, EventArgs e)
        {
            Refresh_Click();
        }

        private void ModernShellPage_UpNavRequested(object sender, EventArgs e)
        {
            Up_Click();
        }

        private void ModernShellPage_ForwardNavRequested(object sender, EventArgs e)
        {
            Forward_Click();
        }

        private void ModernShellPage_BackNavRequested(object sender, EventArgs e)
        {
            Back_Click();
        }

        private async void SidebarControl_RecycleBinItemRightTapped(object sender, EventArgs e)
        {
            var recycleBinHasItems = false;
            if (ServiceConnection != null)
            {
                var value = new ValueSet
                {
                    { "Arguments", "RecycleBin" },
                    { "action", "Query" }
                };
                var response = await ServiceConnection.SendMessageAsync(value);
                if (response.Status == AppServiceResponseStatus.Success && response.Message.TryGetValue("NumItems", out var numItems))
                {
                    recycleBinHasItems = (long)numItems > 0;
                }
            }
            SidebarControl.RecycleBinHasItems = recycleBinHasItems;
        }

        private async void SidebarControl_SidebarItemDropped(object sender, SidebarItemDroppedEventArgs e)
        {
            await filesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.Package, e.ItemPath, true);
        }

        private async void SidebarControl_SidebarItemPropertiesInvoked(object sender, SidebarItemPropertiesInvokedEventArgs e)
        {
            if (e.InvokedItemDataContext is DriveItem)
            {
                await InteractionOperations.OpenPropertiesWindowAsync(e.InvokedItemDataContext);
            }
            else if (e.InvokedItemDataContext is LocationItem)
            {
                ListedItem listedItem = new ListedItem(null)
                {
                    ItemPath = (e.InvokedItemDataContext as LocationItem).Path,
                    ItemName = (e.InvokedItemDataContext as LocationItem).Text,
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemType = "FileFolderListItem".GetLocalized(),
                    LoadFolderGlyph = true
                };
                await InteractionOperations.OpenPropertiesWindowAsync(listedItem);
            }
        }

        private void SidebarControl_SidebarItemInvoked(object sender, SidebarItemInvokedEventArgs e)
        {
            var invokedItemContainer = e.InvokedItemContainer;

            // All items must have DataContext except Settings item
            if (invokedItemContainer.DataContext is null)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(Settings));

                return;
            }

            string navigationPath; // path to navigate
            Type sourcePageType = null; // type of page to navigate

            switch ((invokedItemContainer.DataContext as INavigationControlItem).ItemType)
            {
                case NavigationControlItemType.Location:
                    {
                        var ItemPath = (invokedItemContainer.DataContext as INavigationControlItem).Path; // Get the path of the invoked item

                        if (ItemPath.Equals("Home", StringComparison.OrdinalIgnoreCase)) // Home item
                        {
                            if (ItemPath.Equals(SidebarSelectedItem?.Path, StringComparison.OrdinalIgnoreCase))
                            {
                                return; // return if already selected
                            }

                            navigationPath = "NewTab".GetLocalized();
                            sourcePageType = typeof(YourHome);
                        }
                        else // Any other item
                        {
                            navigationPath = invokedItemContainer.Tag.ToString();
                        }

                        break;
                    }
                default:
                    {
                        navigationPath = invokedItemContainer.Tag.ToString();
                        break;
                    }
            }

            if (string.IsNullOrEmpty(navigationPath) ||
                string.IsNullOrEmpty(FilesystemViewModel?.WorkingDirectory) ||
                navigationPath.TrimEnd(Path.DirectorySeparatorChar).Equals(
                    FilesystemViewModel.WorkingDirectory.TrimEnd(Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase)) // return if already selected
            {
                return;
            }

            ContentFrame.Navigate(
                sourcePageType == null ? InstanceViewModel.FolderSettings.GetLayoutType(navigationPath) : sourcePageType,
                new NavigationArguments()
                {
                    NavPathParam = navigationPath,
                    AssociatedTabInstance = this
                },
                new SuppressNavigationTransitionInfo());

            NavigationToolbar.PathControlDisplayText = FilesystemViewModel.WorkingDirectory;
        }

        private void AppSettings_SortDirectionPreferenceUpdated(object sender, EventArgs e)
        {
            FilesystemViewModel?.UpdateSortDirectionStatus();
        }

        private void AppSettings_SortOptionPreferenceUpdated(object sender, EventArgs e)
        {
            FilesystemViewModel?.UpdateSortOptionStatus();
        }

        private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            if (args.CurrentPoint.Properties.IsXButton1Pressed)
            {
                Back_Click();
            }
            else if (args.CurrentPoint.Properties.IsXButton2Pressed)
            {
                Forward_Click();
            }
        }

        private async void ModernShellPage_PathBoxItemDropped(object sender, PathBoxItemDroppedEventArgs e)
        {
            await filesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.Package, e.Path, true);
        }

        private void ModernShellPage_AddressBarTextEntered(object sender, AddressBarTextEnteredEventArgs e)
        {
            SetAddressBarSuggestions(e.AddressBarTextField);
        }

        private async void SetAddressBarSuggestions(AutoSuggestBox sender, int maxSuggestions = 7)
        {
            var mNavToolbar = (NavigationToolbar as NavigationToolbar);
            if (mNavToolbar != null)
            {
                try
                {
                    IList<ListedItem> suggestions = null;
                    var expandedPath = StorageFileExtensions.GetPathWithoutEnvironmentVariable(sender.Text);
                    var folderPath = Path.GetDirectoryName(expandedPath) ?? expandedPath;
                    var folder = await FilesystemViewModel.GetFolderWithPathFromPathAsync(folderPath);
                    var currPath = await folder.Result.GetFoldersWithPathAsync(Path.GetFileName(expandedPath), (uint)maxSuggestions);
                    if (currPath.Count() >= maxSuggestions)
                    {
                        suggestions = currPath.Select(x => new ListedItem(null)
                        {
                            ItemPath = x.Path,
                            ItemName = x.Folder.Name
                        }).ToList();
                    }
                    else if (currPath.Any())
                    {
                        var subPath = await currPath.First().GetFoldersWithPathAsync((uint)(maxSuggestions - currPath.Count()));
                        suggestions = currPath.Select(x => new ListedItem(null)
                        {
                            ItemPath = x.Path,
                            ItemName = x.Folder.Name
                        }).Concat(
                            subPath.Select(x => new ListedItem(null)
                            {
                                ItemPath = x.Path,
                                ItemName = Path.Combine(currPath.First().Folder.Name, x.Folder.Name)
                            })).ToList();
                    }
                    else
                    {
                        suggestions = new List<ListedItem>() { new ListedItem(null) {
                        ItemPath = FilesystemViewModel.WorkingDirectory,
                        ItemName = "NavigationToolbarVisiblePathNoResults".GetLocalized() } };
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
                        ItemName = "NavigationToolbarVisiblePathNoResults".GetLocalized()
                    });
                }
            }
        }

        private async void ModernShellPage_ToolbarPathItemLoaded(object sender, ToolbarPathItemLoadedEventArgs e)
        {
            await SetPathBoxDropDownFlyoutAsync(e.OpenedFlyout, e.Item);
        }

        private async void ModernShellPage_ToolbarFlyoutOpened(object sender, ToolbarFlyoutOpenedEventArgs e)
        {
            await SetPathBoxDropDownFlyoutAsync(e.OpenedFlyout, (e.OpenedFlyout.Target as FontIcon).DataContext as PathBoxItem);
        }

        private async Task SetPathBoxDropDownFlyoutAsync(MenuFlyout flyout, PathBoxItem pathItem)
        {
            var nextPathItemTitle = NavigationToolbar.PathComponents
                [NavigationToolbar.PathComponents.IndexOf(pathItem) + 1].Title;
            IList<StorageFolderWithPath> childFolders = null;

            StorageFolderWithPath folder = await FilesystemViewModel.GetFolderWithPathFromPathAsync(pathItem.Path);
            if (folder != null)
            {
                childFolders = (await FilesystemTasks.Wrap(() => folder.GetFoldersWithPathAsync(string.Empty))).Result;
            }
            flyout.Items?.Clear();

            if (childFolders == null || childFolders.Count == 0)
            {
                var flyoutItem = new MenuFlyoutItem
                {
                    Icon = new FontIcon { FontFamily = Application.Current.Resources["FluentUIGlyphs"] as FontFamily, Glyph = "\uEC17" },
                    Text = "SubDirectoryAccessDenied".GetLocalized(),
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
                    flyoutItem.Click += (sender, args) =>
                    {
                        ContentFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(childFolder.Path),
                                              new NavigationArguments()
                                              {
                                                  NavPathParam = childFolder.Path,
                                                  AssociatedTabInstance = this
                                              });
                    };
                }

                flyout.Items.Add(flyoutItem);
            }
        }

        private void ModernShellPage_NavigationRequested(object sender, PathNavigationEventArgs e)
        {
            ContentFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(e.ItemPath), new NavigationArguments()
            {
                NavPathParam = e.ItemPath,
                AssociatedTabInstance = this
            });
        }

        private void NavigationToolbar_QuerySubmitted(object sender, ToolbarQuerySubmittedEventArgs e)
        {
            CheckPathInput(FilesystemViewModel, e.QueryText,
                            NavigationToolbar.PathComponents[NavigationToolbar.PathComponents.Count - 1].Path);
        }

        public async void CheckPathInput(ItemViewModel instance, string currentInput, string currentSelectedPath)
        {
            if (currentSelectedPath == currentInput)
            {
                return;
            }

            if (currentInput != instance.WorkingDirectory || ContentFrame.CurrentSourcePageType == typeof(YourHome))
            {
                if (currentInput.Equals("Home", StringComparison.OrdinalIgnoreCase)
                    || currentInput.Equals("NewTab".GetLocalized(), StringComparison.OrdinalIgnoreCase))
                {
                    ContentFrame.Navigate(typeof(YourHome),
                                          new NavigationArguments()
                                          {
                                              NavPathParam = "NewTab".GetLocalized(),
                                              AssociatedTabInstance = this
                                          },
                                          new SuppressNavigationTransitionInfo());
                }
                else
                {
                    currentInput = StorageFileExtensions.GetPathWithoutEnvironmentVariable(currentInput);
                    if (currentSelectedPath == currentInput)
                    {
                        return;
                    }

                    var item = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(currentInput));

                    var resFolder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(currentInput, item));
                    if (resFolder || FilesystemViewModel.CheckFolderAccessWithWin32(currentInput))
                    {
                        var pathToNavigate = resFolder.Result?.Path ?? currentInput;
                        ContentFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(pathToNavigate),
                                              new NavigationArguments()
                                              {
                                                  NavPathParam = pathToNavigate,
                                                  AssociatedTabInstance = this
                                              }); // navigate to folder
                    }
                    else // Not a folder or inaccessible
                    {
                        var resFile = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileWithPathFromPathAsync(currentInput, item));
                        if (resFile)
                        {
                            var pathToInvoke = resFile.Result.Path;
                            await InteractionOperations.InvokeWin32ComponentAsync(pathToInvoke);
                        }
                        else // Not a file or not accessible
                        {
                            var workingDir = string.IsNullOrEmpty(FilesystemViewModel.WorkingDirectory)
                                    || CurrentPageType == typeof(YourHome)
                                ? AppSettings.HomePath
                                : FilesystemViewModel.WorkingDirectory;

                            // Launch terminal application if possible
                            foreach (var terminal in AppSettings.TerminalController.Model.Terminals)
                            {
                                if (terminal.Path.Equals(currentInput, StringComparison.OrdinalIgnoreCase)
                                    || terminal.Path.Equals(currentInput + ".exe", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (ServiceConnection != null)
                                    {
                                        var value = new ValueSet
                                        {
                                            { "WorkingDirectory", workingDir },
                                            { "Application", terminal.Path },
                                            { "Arguments", string.Format(terminal.Arguments, workingDir) }
                                        };
                                        await ServiceConnection.SendMessageAsync(value);
                                    }
                                    return;
                                }
                            }

                            try
                            {
                                if (!await Launcher.LaunchUriAsync(new Uri(currentInput)))
                                {
                                    await DialogDisplayHelper.ShowDialogAsync("InvalidItemDialogTitle".GetLocalized(),
                                        string.Format("InvalidItemDialogContent".GetLocalized(), Environment.NewLine, resFolder.ErrorCode.ToString()));
                                }
                            }
                            catch (Exception ex) when (ex is UriFormatException || ex is ArgumentException)
                            {
                                await DialogDisplayHelper.ShowDialogAsync("InvalidItemDialogTitle".GetLocalized(),
                                    string.Format("InvalidItemDialogContent".GetLocalized(), Environment.NewLine, resFolder.ErrorCode.ToString()));
                            }
                        }
                    }
                }

                NavigationToolbar.PathControlDisplayText = FilesystemViewModel.WorkingDirectory;
            }
        }

        private void NavigationToolbar_EditModeEnabled(object sender, EventArgs e)
        {
            if (NavigationToolbar is NavigationToolbar)
            {
                var mNavToolbar = NavigationToolbar as NavigationToolbar;
                mNavToolbar.ManualEntryBoxLoaded = true;
                mNavToolbar.ClickablePathLoaded = false;
                mNavToolbar.PathText = string.IsNullOrEmpty(FilesystemViewModel?.WorkingDirectory)
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
                    Back_Click();
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

        private async Task<BaseLayout> GetContentOrNullAsync()
        {
            BaseLayout FrameContent = null;
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                FrameContent = (ItemDisplayFrame.Content as BaseLayout);
            });
            return FrameContent;
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

        public string NavParams
        {
            get { return (string)GetValue(NavParamsProperty); }
            set { SetValue(NavParamsProperty, value); }
        }

        public static readonly DependencyProperty NavParamsProperty =
            DependencyProperty.Register("NavParams", typeof(string), typeof(ModernShellPage), new PropertyMetadata(null));

        public AppServiceConnection ServiceConnection { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeAppServiceConnection();
            FilesystemViewModel = new ItemViewModel(this);
            FilesystemViewModel.OnAppServiceConnectionChanged();
            InteractionOperations = new Interaction(this);
            App.Current.Suspending += Current_Suspending;
            App.Current.LeavingBackground += OnLeavingBackground;
            FilesystemViewModel.WorkingDirectoryModified += ViewModel_WorkingDirectoryModified;
            string NavigationPath = ""; // path to navigate

            switch (NavParams)
            {
                case "Start":
                    ItemDisplayFrame.Navigate(typeof(YourHome),
                                              new NavigationArguments()
                                              {
                                                  NavPathParam = NavParams,
                                                  AssociatedTabInstance = this
                                              },
                                              new SuppressNavigationTransitionInfo());
                    SidebarSelectedItem = MainPage.SideBarItems.FirstOrDefault();
                    break;

                case "Desktop":
                    NavigationPath = AppSettings.DesktopPath;
                    SidebarSelectedItem =
                        MainPage.SideBarItems.FirstOrDefault(x => x.Path.Equals(AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "Downloads":
                    NavigationPath = AppSettings.DownloadsPath;
                    SidebarSelectedItem =
                        MainPage.SideBarItems.FirstOrDefault(x => x.Path.Equals(AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "Documents":
                    NavigationPath = AppSettings.DocumentsPath;
                    SidebarSelectedItem =
                        MainPage.SideBarItems.FirstOrDefault(x => x.Path.Equals(AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "Pictures":
                    NavigationPath = AppSettings.PicturesPath;
                    SidebarSelectedItem =
                        MainPage.SideBarItems.FirstOrDefault(x => x.Path.Equals(AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "Music":
                    NavigationPath = AppSettings.MusicPath;
                    SidebarSelectedItem =
                        MainPage.SideBarItems.FirstOrDefault(x => x.Path.Equals(AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "Videos":
                    NavigationPath = AppSettings.VideosPath;
                    SidebarSelectedItem =
                        MainPage.SideBarItems.FirstOrDefault(x => x.Path.Equals(AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase));
                    break;

                case "RecycleBin":
                    NavigationPath = AppSettings.RecycleBinPath;
                    SidebarSelectedItem =
                        MainPage.SideBarItems.FirstOrDefault(x => x.Path.Equals(AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase));
                    break;

                default:
                    if (NavParams == "NewTab".GetLocalized())
                    {
                        ItemDisplayFrame.Navigate(typeof(YourHome),
                                                  new NavigationArguments()
                                                  {
                                                      NavPathParam = NavParams,
                                                      AssociatedTabInstance = this
                                                  },
                                                  new SuppressNavigationTransitionInfo());
                        SidebarSelectedItem = MainPage.SideBarItems[0];
                    }
                    else if (((NavParams[0] >= 'A' && NavParams[0] <= 'Z') || (NavParams[0] >= 'a' && NavParams[0] <= 'z'))
                        && NavParams[1] == ':')
                    {
                        NavigationPath = NavParams;
                        SidebarSelectedItem = AppSettings.DrivesManager.Drives
                            .FirstOrDefault(x => x.Path.ToString().Equals($"{NavParams[0]}:\\", StringComparison.OrdinalIgnoreCase));
                    }
                    else if (NavParams.StartsWith("\\\\?\\")) // USB device
                    {
                        NavigationPath = NavParams;
                        SidebarSelectedItem = App.AppSettings.DrivesManager.Drives
                            .FirstOrDefault(x => x.Path.ToString().Equals($"{Path.GetPathRoot(NavParams)}", StringComparison.OrdinalIgnoreCase));
                    }
                    else if (NavParams.StartsWith("\\\\")) // Network share
                    {
                        NavigationPath = NavParams;
                    }
                    else if (NavParams.StartsWith(AppSettings.RecycleBinPath))
                    {
                        NavigationPath = NavParams;
                    }
                    else
                    {
                        SidebarSelectedItem = null;
                    }
                    break;
            }

            if (NavigationPath != "")
            {
                ContentFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(NavigationPath),
                                      new NavigationArguments()
                                      {
                                          NavPathParam = NavigationPath,
                                          AssociatedTabInstance = this
                                      },
                                      new SuppressNavigationTransitionInfo());
            }

            this.Loaded -= Page_Loaded;
        }

        private async void OnLeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            if (this.ServiceConnection == null)
            {
                // Need to reinitialize AppService when app is resuming
                await InitializeAppServiceConnection();
                FilesystemViewModel?.OnAppServiceConnectionChanged();
            }
        }

        private void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            ServiceConnection?.Dispose();
            ServiceConnection = null;
        }

        public async Task InitializeAppServiceConnection()
        {
            ServiceConnection = new AppServiceConnection();
            ServiceConnection.AppServiceName = "FilesInteropService";
            ServiceConnection.PackageFamilyName = Package.Current.Id.FamilyName;
            ServiceConnection.ServiceClosed += Connection_ServiceClosed;
            AppServiceConnectionStatus status = await ServiceConnection.OpenAsync();
            if (status != AppServiceConnectionStatus.Success)
            {
                // TODO: error handling
                ServiceConnection?.Dispose();
                ServiceConnection = null;
            }

            // Launch fulltrust process
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            ServiceConnection?.Dispose();
            ServiceConnection = null;
        }

        private void ViewModel_WorkingDirectoryModified(object sender, WorkingDirectoryModifiedEventArgs e)
        {
            string value = e.Path;

            INavigationControlItem item = null;
            List<INavigationControlItem> sidebarItems = MainPage.SideBarItems.Where(x => !string.IsNullOrWhiteSpace(x.Path)).ToList();

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

        private async void ItemDisplayFrame_Navigated(object sender, NavigationEventArgs e)
        {
            ContentPage = await GetContentOrNullAsync();
            NavigationToolbar.ClearSearchBoxQueryText(true);
            if (ItemDisplayFrame.CurrentSourcePageType == typeof(GenericFileBrowser)
                || ItemDisplayFrame.CurrentSourcePageType == typeof(GridViewBrowser))
            {
                // Reset DataGrid Rows that may be in "cut" command mode
                ContentPage.ResetItemOpacity();
            }
            
            // Update tab header
            var parameters = e.Parameter as NavigationArguments;
            if (e.SourcePageType == typeof(YourHome))
            {
                MainPage.MultitaskingControl?.UpdateSelectedTab(parameters.NavPathParam, null, false);
            }
            else
            {
                if (parameters.IsSearchResultPage)
                {
                    MainPage.MultitaskingControl?.UpdateSelectedTab(null, null, true);
                }
                else
                {
                    MainPage.MultitaskingControl?.UpdateSelectedTab(new DirectoryInfo(parameters.NavPathParam).Name, parameters.NavPathParam, false);
                }
            }
        }

        public void Clipboard_ContentChanged(object sender, object e)
        {
            if (IsCurrentInstance)
            {
                try
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
                catch (Exception)
                {
                    App.InteractionViewModel.IsPasteEnabled = false;
                }
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
                case (true, false, false, true, VirtualKey.Z): // ctrl + z, undo
                    if (!InstanceViewModel.IsPageTypeSearchResults)
                    {
                        await storageHistoryHelpers.TryUndo();
                    }
                    break;

                case (true, false, false, true, VirtualKey.Y): // ctrl + y, redo
                    if (!InstanceViewModel.IsPageTypeSearchResults)
                    {
                        await storageHistoryHelpers.TryRedo();
                    }
                    break;

                case (true, true, false, _, VirtualKey.T): // ctrl + shif + t, restore recently closed tab
                    {
                        if (!MainPage.MultitaskingControl.RestoredRecentlyClosedTab && MainPage.MultitaskingControl.Items.Count > 0)
                        {
                            await MainPage.AddNewTabByPathAsync(typeof(ModernShellPage), MainPage.MultitaskingControl.RecentlyClosedTabs.Last().Path);
                            MainPage.MultitaskingControl.RestoredRecentlyClosedTab = true;
                        }
                    }
                    break;

                case (true, true, false, true, VirtualKey.N): // ctrl + shift + n, new item
                    if (InstanceViewModel.CanCreateFileInPage)
                    {
                        var addItemDialog = new AddItemDialog();
                        await addItemDialog.ShowAsync();
                        if (addItemDialog.ResultType.ItemType != AddItemType.Cancel)
                        {
                            InteractionOperations.CreateFileFromDialogResultType(
                                addItemDialog.ResultType.ItemType,
                                addItemDialog.ResultType.ItemInfo);
                        }
                    }
                    break;

                case (false, true, false, true, VirtualKey.Delete): // shift + delete, PermanentDelete
                    if (!NavigationToolbar.IsEditModeEnabled && !InstanceViewModel.IsPageTypeSearchResults)
                    {
                        await filesystemHelpers.DeleteItemsAsync(
                            ContentPage.SelectedItems.Select((item) => StorageItemHelpers.FromPathAndType(
                                item.ItemPath,
                                item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory)).ToList(),
                            true, true, true);
                    }

                    break;

                case (true, false, false, true, VirtualKey.C): // ctrl + c, copy
                    if (!NavigationToolbar.IsEditModeEnabled && !ContentPage.IsRenamingItem)
                    {
                        InteractionOperations.CopyItem_ClickAsync(null, null);
                    }

                    break;

                case (true, false, false, true, VirtualKey.V): // ctrl + v, paste
                    if (!NavigationToolbar.IsEditModeEnabled && !ContentPage.IsRenamingItem && !InstanceViewModel.IsPageTypeSearchResults)
                    {
                        await InteractionOperations.PasteItemAsync();
                    }

                    break;

                case (true, false, false, true, VirtualKey.X): // ctrl + x, cut
                    if (!NavigationToolbar.IsEditModeEnabled && !ContentPage.IsRenamingItem)
                    {
                        InteractionOperations.CutItem_Click(null, null);
                    }

                    break;

                case (true, false, false, true, VirtualKey.A): // ctrl + a, select all
                    if (!NavigationToolbar.IsEditModeEnabled && !ContentPage.IsRenamingItem)
                    {
                        InteractionOperations.SelectAllItems();
                    }

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
                    if (ContentPage.IsItemSelected && !ContentPage.IsRenamingItem && !InstanceViewModel.IsPageTypeSearchResults)
                    {
                        await filesystemHelpers.DeleteItemsAsync(
                            ContentPage.SelectedItems.Select((item) => StorageItemHelpers.FromPathAndType(
                                item.ItemPath,
                                item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory)).ToList(),
                            true, false, true);
                    }

                    break;

                case (false, false, false, true, VirtualKey.Space): // space, quick look
                    if (!NavigationToolbar.IsEditModeEnabled && !NavigationToolbar.IsSearchReigonVisible)
                    {
                        if (ContentPage.IsQuickLookEnabled)
                        {
                            InteractionOperations.ToggleQuickLook();
                        }
                    }
                    break;

                case (true, false, false, true, VirtualKey.R): // ctrl + r, refresh
                    if (!InstanceViewModel.IsPageTypeSearchResults)
                    {
                        Refresh_Click();
                    }
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

        public async void Refresh_Click()
        {
            NavigationToolbar.CanRefresh = false;
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var ContentOwnedViewModelInstance = FilesystemViewModel;
                ContentOwnedViewModelInstance.RefreshItems(null);
            });
        }

        public void Back_Click()
        {
            NavigationToolbar.CanGoBack = false;
            Frame instanceContentFrame = ContentFrame;
            if (instanceContentFrame.CanGoBack)
            {
                var previousPageContent = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1];
                var previousPageNavPath = previousPageContent.Parameter as NavigationArguments;
                if (previousPageContent.SourcePageType != typeof(YourHome))
                {
                    // Update layout type
                    InstanceViewModel.FolderSettings.GetLayoutType(previousPageNavPath.IsSearchResultPage ? previousPageNavPath.SearchPathParam : previousPageNavPath.NavPathParam);
                }
                SelectSidebarItemFromPath(previousPageContent.SourcePageType);
                instanceContentFrame.GoBack();
            }
        }

        public void Forward_Click()
        {
            NavigationToolbar.CanGoForward = false;
            Frame instanceContentFrame = ContentFrame;
            if (instanceContentFrame.CanGoForward)
            {
                var incomingPageContent = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1];
                var incomingPageNavPath = incomingPageContent.Parameter as NavigationArguments;
                if (incomingPageContent.SourcePageType != typeof(YourHome))
                {
                    // Update layout type
                    InstanceViewModel.FolderSettings.GetLayoutType(incomingPageNavPath.IsSearchResultPage ? incomingPageNavPath.SearchPathParam : incomingPageNavPath.NavPathParam);
                }
                SelectSidebarItemFromPath(incomingPageContent.SourcePageType);
                instanceContentFrame.GoForward();
            }
        }

        public void Up_Click()
        {
            NavigationToolbar.CanNavigateToParent = false;
            Frame instanceContentFrame = ContentFrame;
            var instance = FilesystemViewModel;
            string parentDirectoryOfPath = instance.WorkingDirectory.TrimEnd('\\');
            var lastSlashIndex = parentDirectoryOfPath.LastIndexOf("\\");
            if (lastSlashIndex != -1)
            {
                parentDirectoryOfPath = instance.WorkingDirectory.Remove(lastSlashIndex);
            }

            SelectSidebarItemFromPath();
            instanceContentFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(parentDirectoryOfPath),
                                          new NavigationArguments()
                                          {
                                              NavPathParam = parentDirectoryOfPath,
                                              AssociatedTabInstance = this
                                          },
                                          new SuppressNavigationTransitionInfo());
        }

        private void SelectSidebarItemFromPath(Type incomingSourcePageType = null)
        {
            if (incomingSourcePageType == typeof(YourHome) && incomingSourcePageType != null)
            {
                SidebarSelectedItem = MainPage.SideBarItems.First(x => x.Path.Equals("Home"));
                NavigationToolbar.PathControlDisplayText = "NewTab".GetLocalized();
            }
        }

        private void SmallWindowTitlebar_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SetTitleBar(SmallWindowTitlebar);
        }

        public void Dispose()
        {
            Window.Current.CoreWindow.PointerPressed -= CoreWindow_PointerPressed;
            SystemNavigationManager.GetForCurrentView().BackRequested -= ModernShellPage_BackRequested;
            Clipboard.ContentChanged -= Clipboard_ContentChanged;
            App.Current.Suspending -= Current_Suspending;
            App.Current.LeavingBackground -= OnLeavingBackground;
            AppSettings.DrivesManager.PropertyChanged -= DrivesManager_PropertyChanged;
            AppSettings.PropertyChanged -= AppSettings_PropertyChanged;
            NavigationToolbar.EditModeEnabled -= NavigationToolbar_EditModeEnabled;
            NavigationToolbar.PathBoxQuerySubmitted -= NavigationToolbar_QuerySubmitted;
            if (SidebarControl != null)
            {
                SidebarControl.SidebarItemInvoked -= SidebarControl_SidebarItemInvoked;
                SidebarControl.SidebarItemPropertiesInvoked -= SidebarControl_SidebarItemPropertiesInvoked;
                SidebarControl.SidebarItemDropped -= SidebarControl_SidebarItemDropped;
                SidebarControl.RecycleBinItemRightTapped -= SidebarControl_RecycleBinItemRightTapped;
            }
            NavigationToolbar.SearchQuerySubmitted -= ModernShellPage_SearchQuerySubmitted;
            NavigationToolbar.SearchTextChanged -= ModernShellPage_SearchTextChanged;
            NavigationToolbar.SearchSuggestionChosen -= ModernShellPage_SearchSuggestionChosen;
            NavigationToolbar.BackRequested -= ModernShellPage_BackNavRequested;
            NavigationToolbar.ForwardRequested -= ModernShellPage_ForwardNavRequested;
            NavigationToolbar.UpRequested -= ModernShellPage_UpNavRequested;
            NavigationToolbar.RefreshRequested -= ModernShellPage_RefreshRequested;
            NavigationToolbar.ItemDraggedOverPathItem -= ModernShellPage_NavigationRequested;

            if (NavigationToolbar is NavigationToolbar navToolbar)
            {
                navToolbar.ToolbarPathItemInvoked -= ModernShellPage_NavigationRequested;
                navToolbar.ToolbarFlyoutOpened -= ModernShellPage_ToolbarFlyoutOpened;
                navToolbar.ToolbarPathItemLoaded -= ModernShellPage_ToolbarPathItemLoaded;
                navToolbar.AddressBarTextEntered -= ModernShellPage_AddressBarTextEntered;
                navToolbar.PathBoxItemDropped -= ModernShellPage_PathBoxItemDropped;
            }

            InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated -= AppSettings_SortDirectionPreferenceUpdated;
            InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated -= AppSettings_SortOptionPreferenceUpdated;

            if (FilesystemViewModel != null)    // Prevent weird case of this being null when many tabs are opened/closed quickly
            {
                FilesystemViewModel.WorkingDirectoryModified -= ViewModel_WorkingDirectoryModified;
                FilesystemViewModel.Dispose();
            }

            ServiceConnection?.Dispose();
            ServiceConnection = null;
        }

        private void SidebarControl_Loaded(object sender, RoutedEventArgs e)
        {
            SidebarControl.SidebarItemInvoked += SidebarControl_SidebarItemInvoked;
            SidebarControl.SidebarItemPropertiesInvoked += SidebarControl_SidebarItemPropertiesInvoked;
            SidebarControl.SidebarItemDropped += SidebarControl_SidebarItemDropped;
            SidebarControl.RecycleBinItemRightTapped += SidebarControl_RecycleBinItemRightTapped;
        }
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
        public bool IsSearchResultPage { get; set; } = false;
        public ObservableCollection<ListedItem> SearchResults { get; set; } = new ObservableCollection<ListedItem>();
        public string SearchPathParam { get; set; } = null;
    }
}