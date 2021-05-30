using Files.Common;
using Files.DataModels;
using Files.Dialogs;
using Files.EventArguments;
using Files.Extensions;
using Files.Filesystem;
using Files.Filesystem.FilesystemHistory;
using Files.Filesystem.Search;
using Files.Helpers;
using Files.Interacts;
using Files.UserControls;
using Files.UserControls.MultitaskingControl;
using Files.ViewModels;
using Files.Views.LayoutModes;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
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
        public IBaseLayout SlimContentPage => ContentPage;
        public IFilesystemHelpers FilesystemHelpers { get; private set; }
        private CancellationTokenSource cancellationTokenSource;
        public SettingsViewModel AppSettings => App.AppSettings;
        public bool CanNavigateBackward => ItemDisplayFrame.CanGoBack;
        public bool CanNavigateForward => ItemDisplayFrame.CanGoForward;
        public FolderSettingsViewModel FolderSettings => InstanceViewModel?.FolderSettings;
        public MainViewModel MainViewModel => App.MainViewModel;
        private bool isCurrentInstance { get; set; } = false;

        public bool IsCurrentInstance
        {
            get
            {
                return isCurrentInstance;
            }
            set
            {
                if (isCurrentInstance != value)
                {
                    isCurrentInstance = value;
                    if (isCurrentInstance)
                    {
                        ContentPage?.ItemManipulationModel.FocusFileList();
                    }
                    else
                    {
                        NavigationToolbar.IsEditModeEnabled = false;
                    }
                    NotifyPropertyChanged(nameof(IsCurrentInstance));
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
                    NotifyPropertyChanged(nameof(ContentPage));
                    NotifyPropertyChanged(nameof(SlimContentPage));
                }
            }
        }

        private bool isPageMainPane;

        public bool IsPageMainPane
        {
            get => isPageMainPane;
            set
            {
                if (value != isPageMainPane)
                {
                    isPageMainPane = value;
                    NotifyPropertyChanged(nameof(IsPageMainPane));
                    NotifyPropertyChanged(nameof(IsPageSecondaryPane));
                }
            }
        }

        public ICommand SelectAllContentPageItemsCommand => new RelayCommand(() => SlimContentPage?.ItemManipulationModel.SelectAllItems());

        public ICommand InvertContentPageSelctionCommand => new RelayCommand(() => SlimContentPage?.ItemManipulationModel.InvertSelection());

        public ICommand ClearContentPageSelectionCommand => new RelayCommand(() => SlimContentPage?.ItemManipulationModel.ClearSelection());

        public ICommand PasteItemsFromClipboardCommand => new RelayCommand(async () => await UIFilesystemHelpers.PasteItemAsync(FilesystemViewModel.WorkingDirectory, this));

        public ICommand CopyPathOfWorkingDirectoryCommand => new RelayCommand(CopyWorkingLocation);

        public ICommand OpenNewWindowCommand => new RelayCommand(NavigationHelpers.LaunchNewWindow);

        public ICommand OpenNewPaneCommand => new RelayCommand(() => PaneHolder?.OpenPathInNewPane("NewTab".GetLocalized()));

        public ICommand OpenDirectoryInDefaultTerminalCommand => new RelayCommand(() => NavigationHelpers.OpenDirectoryInTerminal(this.FilesystemViewModel.WorkingDirectory, this));

        public ICommand AddNewTabToMultitaskingControlCommand => new RelayCommand(async () => await MainPageViewModel.AddNewTabAsync());

        public ICommand CreateNewFileCommand => new RelayCommand<ShellNewEntry>(x => UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemType.File, x, this));

        public ICommand CreateNewFolderCommand => new RelayCommand(() => UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemType.Folder, null, this));

        public static readonly DependencyProperty IsPageMainPaneProperty =
            DependencyProperty.Register("IsPageMainPane", typeof(bool), typeof(ModernShellPage), new PropertyMetadata(true));

        public SolidColorBrush CurrentInstanceBorderBrush
        {
            get { return (SolidColorBrush)GetValue(CurrentInstanceBorderBrushProperty); }
            set { SetValue(CurrentInstanceBorderBrushProperty, value); }
        }

        public Thickness CurrentInstanceBorderThickness
        {
            get { return (Thickness)GetValue(CurrentInstanceBorderThicknessProperty); }
            set { SetValue(CurrentInstanceBorderThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentInstanceBorderThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentInstanceBorderThicknessProperty =
            DependencyProperty.Register("CurrentInstanceBorderThickness", typeof(Thickness), typeof(ModernShellPage), new PropertyMetadata(null));



        public static readonly DependencyProperty CurrentInstanceBorderBrushProperty =
            DependencyProperty.Register("CurrentInstanceBorderBrush", typeof(SolidColorBrush), typeof(ModernShellPage), new PropertyMetadata(null));

        public bool IsPageSecondaryPane => !IsMultiPaneActive || !IsPageMainPane;

        public Type CurrentPageType => ItemDisplayFrame.SourcePageType;

        public INavigationToolbar NavigationToolbar => NavToolbar;

        public ModernShellPage()
        {
            InitializeComponent();

            InstanceViewModel = new CurrentInstanceViewModel();
            InstanceViewModel.FolderSettings.LayoutPreferencesUpdateRequired += FolderSettings_LayoutPreferencesUpdateRequired;
            cancellationTokenSource = new CancellationTokenSource();
            FilesystemHelpers = new FilesystemHelpers(this, cancellationTokenSource.Token);
            storageHistoryHelpers = new StorageHistoryHelpers(new StorageHistoryOperations(this, cancellationTokenSource.Token));

            NavToolbar.SearchBox.QueryChanged += ModernShellPage_QueryChanged;
            NavToolbar.SearchBox.QuerySubmitted += ModernShellPage_QuerySubmitted;
            NavToolbar.SearchBox.SuggestionChosen += ModernShellPage_SuggestionChosen;

            DisplayFilesystemConsentDialog();

            var flowDirectionSetting = ResourceContext.GetForCurrentView().QualifierValues["LayoutDirection"];

            if (flowDirectionSetting == "RTL")
            {
                FlowDirection = FlowDirection.RightToLeft;
            }

            NavigationToolbar.PathControlDisplayText = "NewTab".GetLocalized();

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

            App.DrivesManager.PropertyChanged += DrivesManager_PropertyChanged;

            AppServiceConnectionHelper.ConnectionChanged += AppServiceConnectionHelper_ConnectionChanged;
        }

        private void ModernShellPage_RefreshWidgetsRequested(object sender, EventArgs e)
        {
            WidgetsPage currentPage = ItemDisplayFrame?.Content as WidgetsPage;
            currentPage.RefreshWidgetList();
        }

        private void CopyWorkingLocation()
        {
            try
            {
                if (this.SlimContentPage != null)
                {
                    DataPackage data = new DataPackage();
                    data.SetText(this.FilesystemViewModel.WorkingDirectory);
                    Clipboard.SetContent(data);
                    Clipboard.Flush();
                }
            }
            catch
            {
            }
        }

        private void FolderSettings_LayoutPreferencesUpdateRequired(object sender, LayoutPreferenceEventArgs e)
        {
            if (FilesystemViewModel != null)
            {
                (sender as FolderSettingsViewModel).UpdateLayoutPreferencesForPath(FilesystemViewModel.WorkingDirectory, e.LayoutPreference);
                if (e.IsAdaptiveLayoutUpdateRequired)
                {
                    AdaptiveLayoutHelpers.PredictLayoutMode(InstanceViewModel.FolderSettings, FilesystemViewModel);
                }
            }
        }

        /*
         * Ensure that the path bar gets updated for user interaction
         * whenever the path changes. We will get the individual directories from
         * the updated, most-current path and add them to the UI.
         */

        public void UpdatePathUIToWorkingDirectory(string newWorkingDir, string singleItemOverride = null)
        {
            if (string.IsNullOrWhiteSpace(singleItemOverride))
            {
                var components = StorageFileExtensions.GetDirectoryPathComponents(newWorkingDir);
                var lastCommonItemIndex = NavigationToolbar.PathComponents
                    .Select((value, index) => new { value, index })
                    .LastOrDefault(x => x.index < components.Count && x.value.Path == components[x.index].Path)?.index ?? 0;
                while (NavigationToolbar.PathComponents.Count > lastCommonItemIndex)
                {
                    NavigationToolbar.PathComponents.RemoveAt(lastCommonItemIndex);
                }
                foreach (var component in components.Skip(lastCommonItemIndex))
                {
                    NavigationToolbar.PathComponents.Add(component);
                }
            }
            else
            {
                NavigationToolbar.PathComponents.Clear(); // Clear the path UI
                NavigationToolbar.PathComponents.Add(new Views.PathBoxItem() { Path = null, Title = singleItemOverride });
            }
        }

        private async void ModernShellPage_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var invokedItem = (args.SelectedItem as ListedItem);
            if (invokedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
            {
                ItemDisplayFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(invokedItem.ItemPath), new NavigationArguments()
                {
                    NavPathParam = invokedItem.ItemPath,
                    AssociatedTabInstance = this
                });
            }
            else
            {
                // TODO: Add fancy file launch options similar to Interactions.cs OpenSelectedItems()
                await Win32Helpers.InvokeWin32ComponentAsync(invokedItem.ItemPath, this);
            }
        }

        private async void ModernShellPage_QueryChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (!string.IsNullOrWhiteSpace(sender.Text))
                {
                    var search = new FolderSearch
                    {
                        Query = sender.Text,
                        Folder = FilesystemViewModel.WorkingDirectory,
                        MaxItemCount = 10,
                        SearchUnindexedItems = App.AppSettings.SearchUnindexedItems
                    };
                    sender.ItemsSource = await search.SearchAsync();
                }
                else
                {
                    sender.ItemsSource = null;
                }
            }
        }

        private void ModernShellPage_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion == null && !string.IsNullOrWhiteSpace(args.QueryText))
            {
                SubmitSearch(args.QueryText, AppSettings.SearchUnindexedItems);
            }
        }

        public async void SubmitSearch(string query, bool searchUnindexedItems)
        {
            var search = new FolderSearch
            {
                Query = query,
                Folder = FilesystemViewModel.WorkingDirectory,
                ThumbnailSize = InstanceViewModel.FolderSettings.GetIconSize(),
                SearchUnindexedItems = searchUnindexedItems
            };

            InstanceViewModel.CurrentSearchQuery = query;
            FilesystemViewModel.IsLoadingIndicatorActive = true;
            InstanceViewModel.SearchedUnindexedItems = searchUnindexedItems;
            ItemDisplayFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(FilesystemViewModel.WorkingDirectory), new NavigationArguments()
            {
                AssociatedTabInstance = this,
                IsSearchResultPage = true,
                SearchPathParam = FilesystemViewModel.WorkingDirectory,
                SearchResults = await search.SearchAsync(),
            });

            FilesystemViewModel.IsLoadingIndicatorActive = false;
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

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            NavParams = eventArgs.Parameter.ToString();
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
            if (IsCurrentInstance)
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
        }

        private async void ModernShellPage_PathBoxItemDropped(object sender, PathBoxItemDroppedEventArgs e)
        {
            await FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.Package, e.Path, false, true);
        }

        private void ModernShellPage_AddressBarTextEntered(object sender, AddressBarTextEnteredEventArgs e)
        {
            SetAddressBarSuggestions(e.AddressBarTextField);
        }

        private async void SetAddressBarSuggestions(AutoSuggestBox sender, int maxSuggestions = 7)
        {
            var mNavToolbar = (NavigationToolbar as NavigationToolbar);
            if (mNavToolbar != null && !string.IsNullOrWhiteSpace(sender.Text))
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
                            ItemName = x.Folder.DisplayName
                        }).ToList();
                    }
                    else if (currPath.Any())
                    {
                        var subPath = await currPath.First().GetFoldersWithPathAsync((uint)(maxSuggestions - currPath.Count()));
                        suggestions = currPath.Select(x => new ListedItem(null)
                        {
                            ItemPath = x.Path,
                            ItemName = x.Folder.DisplayName
                        }).Concat(
                            subPath.Select(x => new ListedItem(null)
                            {
                                ItemPath = x.Path,
                                ItemName = Path.Combine(currPath.First().Folder.DisplayName, x.Folder.DisplayName)
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
                        // No elements in common, update the list in-place
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
                    Icon = new FontIcon { Glyph = "\uE7BA" },
                    Text = "SubDirectoryAccessDenied".GetLocalized(),
                    //Foreground = (SolidColorBrush)Application.Current.Resources["SystemControlErrorTextForegroundBrush"],
                    FontSize = 12
                };
                flyout.Items.Add(flyoutItem);
                return;
            }

            var boldFontWeight = new FontWeight { Weight = 800 };
            var normalFontWeight = new FontWeight { Weight = 400 };

            var workingPath = NavigationToolbar.PathComponents
                    [NavigationToolbar.PathComponents.Count - 1].
                    Path?.TrimEnd(Path.DirectorySeparatorChar);
            foreach (var childFolder in childFolders)
            {
                var isPathItemFocused = childFolder.Item.Name == nextPathItemTitle;

                var flyoutItem = new MenuFlyoutItem
                {
                    Icon = new FontIcon
                    {
                        Glyph = "\uED25",
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
                        ItemDisplayFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(childFolder.Path),
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
            ItemDisplayFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(e.ItemPath), new NavigationArguments()
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
            currentInput = currentInput.Replace("\\\\", "\\");

            if (currentInput.StartsWith("\\") && !currentInput.StartsWith("\\\\"))
            {
                currentInput = currentInput.Insert(0, "\\");
            }

            if (currentSelectedPath == currentInput || string.IsNullOrWhiteSpace(currentInput))
            {
                return;
            }

            if (currentInput != instance.WorkingDirectory || CurrentPageType == typeof(WidgetsPage))
            {
                if (currentInput.Equals("Home", StringComparison.OrdinalIgnoreCase)
                    || currentInput.Equals("NewTab".GetLocalized(), StringComparison.OrdinalIgnoreCase))
                {
                    ItemDisplayFrame.Navigate(typeof(WidgetsPage),
                                          new NavigationArguments()
                                          {
                                              NavPathParam = "NewTab".GetLocalized(),
                                              AssociatedTabInstance = this
                                          },
                                          new EntranceNavigationTransitionInfo());
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
                    if (resFolder || FolderHelpers.CheckFolderAccessWithWin32(currentInput))
                    {
                        var pathToNavigate = resFolder.Result?.Path ?? currentInput;
                        ItemDisplayFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(pathToNavigate),
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
                            await Win32Helpers.InvokeWin32ComponentAsync(pathToInvoke, this);
                        }
                        else // Not a file or not accessible
                        {
                            var workingDir = string.IsNullOrEmpty(FilesystemViewModel.WorkingDirectory)
                                    || CurrentPageType == typeof(WidgetsPage)
                                ? AppSettings.HomePath
                                : FilesystemViewModel.WorkingDirectory;

                            // Launch terminal application if possible
                            foreach (var terminal in AppSettings.TerminalController.Model.Terminals)
                            {
                                if (terminal.Path.Equals(currentInput, StringComparison.OrdinalIgnoreCase)
                                    || terminal.Path.Equals(currentInput + ".exe", StringComparison.OrdinalIgnoreCase) || terminal.Name.Equals(currentInput, StringComparison.OrdinalIgnoreCase))
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
                if (ItemDisplayFrame.CanGoBack)
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
            if (App.DrivesManager?.ShowUserConsentOnInit ?? false)
            {
                App.DrivesManager.ShowUserConsentOnInit = false;
                DynamicDialog dialog = DynamicDialogFactory.GetFor_ConsentDialog();
                await dialog.ShowAsync(ContentDialogPlacement.Popup);
            }
        }

        private string navParams;

        public string NavParams
        {
            get => navParams;
            set
            {
                if (value != navParams)
                {
                    navParams = value;
                    if (IsLoaded)
                    {
                        OnNavigationParamsChanged();
                    }
                }
            }
        }

        private void OnNavigationParamsChanged()
        {
            if (string.IsNullOrEmpty(NavParams) || NavParams == "NewTab".GetLocalized() || NavParams == "Home")
            {
                ItemDisplayFrame.Navigate(typeof(WidgetsPage),
                    new NavigationArguments()
                    {
                        NavPathParam = NavParams,
                        AssociatedTabInstance = this
                    }, new EntranceNavigationTransitionInfo());
            }
            else
            {
                ItemDisplayFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(NavParams),
                    new NavigationArguments()
                    {
                        NavPathParam = NavParams,
                        AssociatedTabInstance = this
                    });
            }
        }

        public static readonly DependencyProperty NavParamsProperty =
            DependencyProperty.Register("NavParams", typeof(string), typeof(ModernShellPage), new PropertyMetadata(null));

        public NamedPipeAsAppServiceConnection ServiceConnection { get; private set; }

        private TabItemArguments tabItemArguments;

        public TabItemArguments TabItemArguments
        {
            get => tabItemArguments;
            set
            {
                if (tabItemArguments != value)
                {
                    tabItemArguments = value;
                    ContentChanged?.Invoke(this, value);
                }
            }
        }

        private bool isMultiPaneActive;

        public bool IsMultiPaneActive
        {
            get => isMultiPaneActive;
            set
            {
                if (value != isMultiPaneActive)
                {
                    isMultiPaneActive = value;
                    NotifyPropertyChanged(nameof(IsMultiPaneActive));
                    NotifyPropertyChanged(nameof(IsPageSecondaryPane));
                }
            }
        }

        private bool isMultiPaneEnabled;

        public bool IsMultiPaneEnabled
        {
            get => isMultiPaneEnabled;
            set
            {
                if (value != isMultiPaneEnabled)
                {
                    isMultiPaneEnabled = value;
                    NotifyPropertyChanged(nameof(IsMultiPaneEnabled));
                }
            }
        }

        private IPaneHolder paneHolder;

        public IPaneHolder PaneHolder
        {
            get => paneHolder;
            set
            {
                if (value != paneHolder)
                {
                    paneHolder = value;
                    NotifyPropertyChanged(nameof(PaneHolder));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<TabItemArguments> ContentChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            FilesystemViewModel = new ItemViewModel(InstanceViewModel?.FolderSettings);
            FilesystemViewModel.WorkingDirectoryModified += ViewModel_WorkingDirectoryModified;
            FilesystemViewModel.ItemLoadStatusChanged += FilesystemViewModel_ItemLoadStatusChanged;
            FilesystemViewModel.DirectoryInfoUpdated += FilesystemViewModel_DirectoryInfoUpdated;
            FilesystemViewModel.PageTypeUpdated += FilesystemViewModel_PageTypeUpdated;
            OnNavigationParamsChanged();
            ServiceConnection = await AppServiceConnectionHelper.Instance;
            FilesystemViewModel.OnAppServiceConnectionChanged(ServiceConnection);
            this.Loaded -= Page_Loaded;
        }

        private void FilesystemViewModel_PageTypeUpdated(object sender, PageTypeUpdatedEventArgs e)
        {
            InstanceViewModel.IsPageTypeCloudDrive = e.IsTypeCloudDrive;
        }

        private void FilesystemViewModel_DirectoryInfoUpdated(object sender, EventArgs e)
        {
            if (ContentPage != null)
            {
                if (FilesystemViewModel.FilesAndFolders.Count == 1)
                {
                    ContentPage.DirectoryPropertiesViewModel.DirectoryItemCount = $"{FilesystemViewModel.FilesAndFolders.Count} {"ItemCount/Text".GetLocalized()}";
                }
                else
                {
                    ContentPage.DirectoryPropertiesViewModel.DirectoryItemCount = $"{FilesystemViewModel.FilesAndFolders.Count} {"ItemsCount/Text".GetLocalized()}";
                }
            }
        }

        private void ViewModel_WorkingDirectoryModified(object sender, WorkingDirectoryModifiedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Path))
            {
                if (e.IsLibrary)
                {
                    UpdatePathUIToWorkingDirectory(null, e.Name);
                }
                else
                {
                    UpdatePathUIToWorkingDirectory(e.Path);
                }
            }
        }

        private async void ItemDisplayFrame_Navigated(object sender, NavigationEventArgs e)
        {
            ContentPage = await GetContentOrNullAsync();
            NavigationToolbar.SearchBox.Query = string.Empty;
            NavigationToolbar.IsSearchBoxVisible = false;
            if (ItemDisplayFrame.CurrentSourcePageType == (typeof(DetailsLayoutBrowser))
                || ItemDisplayFrame.CurrentSourcePageType == typeof(GridViewBrowser))
            {
                // Reset DataGrid Rows that may be in "cut" command mode
                ContentPage.ResetItemOpacity();
            }
            var parameters = e.Parameter as NavigationArguments;
            TabItemArguments = new TabItemArguments()
            {
                InitialPageType = typeof(ModernShellPage),
                NavigationArg = parameters.IsSearchResultPage ? parameters.SearchPathParam : parameters.NavPathParam
            };
        }

        private async void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            var ctrl = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Control);
            var alt = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Menu);
            var shift = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
            var tabInstance = CurrentPageType == typeof(DetailsLayoutBrowser)
                || CurrentPageType == typeof(DetailsLayoutBrowser)
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

                case (true, false, false, true, VirtualKey.F): // ctrl + f
                    NavigationToolbar.SwitchSearchBoxVisibility();
                    break;

                case (true, true, false, true, VirtualKey.N): // ctrl + shift + n, new item
                    if (InstanceViewModel.CanCreateFileInPage)
                    {
                        var addItemDialog = new AddItemDialog();
                        await addItemDialog.ShowAsync();
                        if (addItemDialog.ResultType.ItemType != AddItemType.Cancel)
                        {
                            UIFilesystemHelpers.CreateFileFromDialogResultType(
                                addItemDialog.ResultType.ItemType,
                                addItemDialog.ResultType.ItemInfo,
                                this);
                        }
                    }
                    break;

                case (false, true, false, true, VirtualKey.Delete): // shift + delete, PermanentDelete
                    if (ContentPage.IsItemSelected && !NavigationToolbar.IsEditModeEnabled && !InstanceViewModel.IsPageTypeSearchResults)
                    {
                        await FilesystemHelpers.DeleteItemsAsync(
                            ContentPage.SelectedItems.Select((item) => StorageItemHelpers.FromPathAndType(
                                item.ItemPath,
                                item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory)).ToList(),
                            true, true, true);
                    }

                    break;

                case (true, false, false, true, VirtualKey.C): // ctrl + c, copy
                    if (!NavigationToolbar.IsEditModeEnabled && !ContentPage.IsRenamingItem)
                    {
                        UIFilesystemHelpers.CopyItem(this);
                    }

                    break;

                case (true, false, false, true, VirtualKey.V): // ctrl + v, paste
                    if (!NavigationToolbar.IsEditModeEnabled && !ContentPage.IsRenamingItem && !InstanceViewModel.IsPageTypeSearchResults)
                    {
                        await UIFilesystemHelpers.PasteItemAsync(FilesystemViewModel.WorkingDirectory, this);
                    }

                    break;

                case (true, false, false, true, VirtualKey.X): // ctrl + x, cut
                    if (!NavigationToolbar.IsEditModeEnabled && !ContentPage.IsRenamingItem)
                    {
                        UIFilesystemHelpers.CutItem(this);
                    }

                    break;

                case (true, false, false, true, VirtualKey.A): // ctrl + a, select all
                    if (!NavigationToolbar.IsEditModeEnabled && !ContentPage.IsRenamingItem)
                    {
                        this.SlimContentPage.ItemManipulationModel.SelectAllItems();
                    }

                    break;

                case (false, false, false, true, VirtualKey.Delete): // delete, delete item
                    if (ContentPage.IsItemSelected && !ContentPage.IsRenamingItem && !InstanceViewModel.IsPageTypeSearchResults)
                    {
                        await FilesystemHelpers.DeleteItemsAsync(
                            ContentPage.SelectedItems.Select((item) => StorageItemHelpers.FromPathAndType(
                                item.ItemPath,
                                item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory)).ToList(),
                            true, false, true);
                    }

                    break;

                case (false, false, false, true, VirtualKey.Space): // space, quick look
                    if (!NavigationToolbar.IsEditModeEnabled && !NavigationToolbar.IsSearchBoxVisible)
                    {
                        if (MainViewModel.IsQuickLookEnabled)
                        {
                            QuickLookHelpers.ToggleQuickLook(this);
                        }
                    }
                    break;

                case (true, false, false, true, VirtualKey.P):
                    PreviewPaneEnabled = !PreviewPaneEnabled;
                    break;

                case (true, false, false, true, VirtualKey.R): // ctrl + r, refresh
                    if (!InstanceViewModel.IsPageTypeSearchResults)
                    {
                        Refresh_Click();
                    }
                    break;

                case (false, false, true, _, VirtualKey.D): // alt + d, select address bar (english)
                case (true, false, false, _, VirtualKey.L): // ctrl + l, select address bar
                    NavigationToolbar.IsEditModeEnabled = true;
                    break;

                case (false, false, false, _, VirtualKey.F1): // F1, open Files wiki
                    await Launcher.LaunchUriAsync(new Uri(@"https://files-community.github.io/docs"));
                    break;

                case (true, true, false, _, VirtualKey.Number1): // ctrl+shift+1, details view
                    InstanceViewModel.FolderSettings.ToggleLayoutModeDetailsView.Execute(true);
                    break;
                case (true, true, false, _, VirtualKey.Number2): // ctrl+shift+2, tiles view
                    InstanceViewModel.FolderSettings.ToggleLayoutModeTiles.Execute(true);
                    break;
                case (true, true, false, _, VirtualKey.Number3): // ctrl+shift+3, grid small view
                    InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewSmall.Execute(true);
                    break;
                case (true, true, false, _, VirtualKey.Number4): // ctrl+shift+4, grid medium view
                    InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewMedium.Execute(true);
                    break;
                case (true, true, false, _, VirtualKey.Number5): // ctrl+shift+5, grid large view
                    InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewLarge.Execute(true);
                    break;
                case (true, true, false, _, VirtualKey.Number6): // ctrl+shift+6, column view
                    InstanceViewModel.FolderSettings.ToggleLayoutModeColumnView.Execute(true);
                    break;
            };

            switch (args.KeyboardAccelerator.Key)
            {
                case VirtualKey.F2: //F2, rename
                    if (CurrentPageType == typeof(DetailsLayoutBrowser) || CurrentPageType == typeof(GridViewBrowser))
                    {
                        if (ContentPage.IsItemSelected)
                        {
                            ContentPage.ItemManipulationModel.StartRenameItem();
                        }
                    }
                    break;
            }
        }

        public async void Refresh_Click()
        {
            NavigationToolbar.CanRefresh = false;
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var ContentOwnedViewModelInstance = FilesystemViewModel;
                ContentOwnedViewModelInstance?.RefreshItems(null);
            });
        }

        public void Back_Click()
        {
            NavigationToolbar.CanGoBack = false;
            if (ItemDisplayFrame.CanGoBack)
            {
                var previousPageContent = ItemDisplayFrame.BackStack[ItemDisplayFrame.BackStack.Count - 1];
                var previousPageNavPath = previousPageContent.Parameter as NavigationArguments;
                previousPageNavPath.IsLayoutSwitch = false;
                if (previousPageContent.SourcePageType != typeof(WidgetsPage))
                {
                    // Update layout type
                    InstanceViewModel.FolderSettings.GetLayoutType(previousPageNavPath.IsSearchResultPage ? previousPageNavPath.SearchPathParam : previousPageNavPath.NavPathParam);
                }
                SelectSidebarItemFromPath(previousPageContent.SourcePageType);

                if (previousPageContent.SourcePageType == typeof(WidgetsPage))
                {
                    ItemDisplayFrame.GoBack(new EntranceNavigationTransitionInfo());
                }
                else
                {
                    ItemDisplayFrame.GoBack();
                }
            }
        }

        public void Forward_Click()
        {
            NavigationToolbar.CanGoForward = false;
            if (ItemDisplayFrame.CanGoForward)
            {
                var incomingPageContent = ItemDisplayFrame.ForwardStack[ItemDisplayFrame.ForwardStack.Count - 1];
                var incomingPageNavPath = incomingPageContent.Parameter as NavigationArguments;
                incomingPageNavPath.IsLayoutSwitch = false;
                if (incomingPageContent.SourcePageType != typeof(WidgetsPage))
                {
                    // Update layout type
                    InstanceViewModel.FolderSettings.GetLayoutType(incomingPageNavPath.IsSearchResultPage ? incomingPageNavPath.SearchPathParam : incomingPageNavPath.NavPathParam);
                }
                SelectSidebarItemFromPath(incomingPageContent.SourcePageType);
                ItemDisplayFrame.GoForward();
            }
        }

        public void Up_Click()
        {
            NavigationToolbar.CanNavigateToParent = false;
            if (string.IsNullOrEmpty(FilesystemViewModel?.WorkingDirectory))
            {
                return;
            }

            bool isPathRooted = FilesystemViewModel.WorkingDirectory == PathNormalization.GetPathRoot(FilesystemViewModel.WorkingDirectory);

            if (isPathRooted)
            {
                ItemDisplayFrame.Navigate(typeof(WidgetsPage),
                                          new NavigationArguments()
                                          {
                                              NavPathParam = "NewTab".GetLocalized(),
                                              AssociatedTabInstance = this
                                          },
                                          new SuppressNavigationTransitionInfo());
            }
            else
            {
                string parentDirectoryOfPath = FilesystemViewModel.WorkingDirectory.TrimEnd('\\', '/');

                var lastSlashIndex = parentDirectoryOfPath.LastIndexOf("\\");
                if (lastSlashIndex == -1)
                {
                    lastSlashIndex = parentDirectoryOfPath.LastIndexOf("/");
                }
                if (lastSlashIndex != -1)
                {
                    parentDirectoryOfPath = FilesystemViewModel.WorkingDirectory.Remove(lastSlashIndex);
                }

                SelectSidebarItemFromPath();
                ItemDisplayFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(parentDirectoryOfPath),
                                              new NavigationArguments()
                                              {
                                                  NavPathParam = parentDirectoryOfPath,
                                                  AssociatedTabInstance = this
                                              },
                                              new SuppressNavigationTransitionInfo());
            }
        }

        private void SelectSidebarItemFromPath(Type incomingSourcePageType = null)
        {
            if (incomingSourcePageType == typeof(WidgetsPage) && incomingSourcePageType != null)
            {
                NavigationToolbar.PathControlDisplayText = "NewTab".GetLocalized();
            }
        }

        public void Dispose()
        {
            Window.Current.CoreWindow.PointerPressed -= CoreWindow_PointerPressed;
            SystemNavigationManager.GetForCurrentView().BackRequested -= ModernShellPage_BackRequested;
            App.DrivesManager.PropertyChanged -= DrivesManager_PropertyChanged;
            NavigationToolbar.EditModeEnabled -= NavigationToolbar_EditModeEnabled;
            NavigationToolbar.PathBoxQuerySubmitted -= NavigationToolbar_QuerySubmitted;
            NavigationToolbar.BackRequested -= ModernShellPage_BackNavRequested;
            NavigationToolbar.ForwardRequested -= ModernShellPage_ForwardNavRequested;
            NavigationToolbar.UpRequested -= ModernShellPage_UpNavRequested;
            NavigationToolbar.RefreshRequested -= ModernShellPage_RefreshRequested;
            NavigationToolbar.RefreshWidgetsRequested += ModernShellPage_RefreshWidgetsRequested;
            NavigationToolbar.ItemDraggedOverPathItem -= ModernShellPage_NavigationRequested;
            NavigationToolbar.SearchBox.QueryChanged -= ModernShellPage_QueryChanged;
            NavigationToolbar.SearchBox.QuerySubmitted -= ModernShellPage_QuerySubmitted;
            NavigationToolbar.SearchBox.SuggestionChosen -= ModernShellPage_SuggestionChosen;
            if (NavigationToolbar is NavigationToolbar navToolbar)
            {
                navToolbar.ToolbarPathItemInvoked -= ModernShellPage_NavigationRequested;
                navToolbar.ToolbarFlyoutOpened -= ModernShellPage_ToolbarFlyoutOpened;
                navToolbar.ToolbarPathItemLoaded -= ModernShellPage_ToolbarPathItemLoaded;
                navToolbar.AddressBarTextEntered -= ModernShellPage_AddressBarTextEntered;
                navToolbar.PathBoxItemDropped -= ModernShellPage_PathBoxItemDropped;
            }

            InstanceViewModel.FolderSettings.LayoutPreferencesUpdateRequired -= FolderSettings_LayoutPreferencesUpdateRequired;
            InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated -= AppSettings_SortDirectionPreferenceUpdated;
            InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated -= AppSettings_SortOptionPreferenceUpdated;

            if (FilesystemViewModel != null)    // Prevent weird case of this being null when many tabs are opened/closed quickly
            {
                FilesystemViewModel.WorkingDirectoryModified -= ViewModel_WorkingDirectoryModified;
                FilesystemViewModel.ItemLoadStatusChanged -= FilesystemViewModel_ItemLoadStatusChanged;
                FilesystemViewModel.DirectoryInfoUpdated -= FilesystemViewModel_DirectoryInfoUpdated;
                FilesystemViewModel.PageTypeUpdated -= FilesystemViewModel_PageTypeUpdated;
                FilesystemViewModel.Dispose();
            }
            AppServiceConnectionHelper.ConnectionChanged -= AppServiceConnectionHelper_ConnectionChanged;
        }

        private async void AppServiceConnectionHelper_ConnectionChanged(object sender, Task<NamedPipeAsAppServiceConnection> e)
        {
            ServiceConnection = await e;
            FilesystemViewModel?.OnAppServiceConnectionChanged(ServiceConnection);
        }

        private void FilesystemViewModel_ItemLoadStatusChanged(object sender, ItemLoadStatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case ItemLoadStatusChangedEventArgs.ItemLoadStatus.Starting:
                    NavigationToolbar.CanRefresh = false;
                    break;

                case ItemLoadStatusChangedEventArgs.ItemLoadStatus.InProgress:
                    NavigationToolbar.CanGoBack = ItemDisplayFrame.CanGoBack;
                    NavigationToolbar.CanGoForward = ItemDisplayFrame.CanGoForward;
                    break;

                case ItemLoadStatusChangedEventArgs.ItemLoadStatus.Complete:
                    NavigationToolbar.CanRefresh = true;
                    // Select previous directory
                    if (!string.IsNullOrWhiteSpace(e.PreviousDirectory))
                    {
                        if (e.PreviousDirectory.Contains(e.Path) && !e.PreviousDirectory.Contains("Shell:RecycleBinFolder"))
                        {
                            // Remove the WorkingDir from previous dir
                            e.PreviousDirectory = e.PreviousDirectory.Replace(e.Path, string.Empty);

                            // Get previous dir name
                            if (e.PreviousDirectory.StartsWith('\\'))
                            {
                                e.PreviousDirectory = e.PreviousDirectory.Remove(0, 1);
                            }
                            if (e.PreviousDirectory.Contains('\\'))
                            {
                                e.PreviousDirectory = e.PreviousDirectory.Split('\\')[0];
                            }

                            // Get the first folder and combine it with WorkingDir
                            string folderToSelect = string.Format("{0}\\{1}", e.Path, e.PreviousDirectory);

                            // Make sure we don't get double \\ in the e.Path
                            folderToSelect = folderToSelect.Replace("\\\\", "\\");

                            if (folderToSelect.EndsWith('\\'))
                            {
                                folderToSelect = folderToSelect.Remove(folderToSelect.Length - 1, 1);
                            }

                            ListedItem itemToSelect = FilesystemViewModel.FilesAndFolders.Where((item) => item.ItemPath == folderToSelect).FirstOrDefault();

                            if (itemToSelect != null && ContentPage != null)
                            {
                                ContentPage.ItemManipulationModel.SetSelectedItem(itemToSelect);
                                ContentPage.ItemManipulationModel.ScrollIntoView(itemToSelect);
                            }
                        }
                    }
                    break;
            }
        }

        public DataPackageOperation TabItemDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.AvailableFormats.Contains(StandardDataFormats.StorageItems))
            {
                if (!InstanceViewModel.IsPageTypeSearchResults)
                {
                    return DataPackageOperation.Move;
                }
            }
            return DataPackageOperation.None;
        }

        public async Task<DataPackageOperation> TabItemDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.AvailableFormats.Contains(StandardDataFormats.StorageItems))
            {
                if (InstanceViewModel.IsPageTypeNotHome && !InstanceViewModel.IsPageTypeSearchResults)
                {
                    await FilesystemHelpers.PerformOperationTypeAsync(
                        DataPackageOperation.Move,
                        e.DataView,
                        FilesystemViewModel.WorkingDirectory,
                        false,
                        true);
                    return DataPackageOperation.Move;
                }
            }
            return DataPackageOperation.None;
        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePositioning();
        }

        /// <summary>
        /// Call this function to update the positioning of the preview pane.
        /// This is a workaround as the VisualStateManager causes problems.
        /// </summary>
        private void UpdatePositioning()
        {
            if (!LoadPreviewPane || PreviewPaneDropShadowPanel is null || PreviewPane is null)
            {
                PreviewPaneRow.Height = new GridLength(0);
                PreviewPaneColumn.Width = new GridLength(0);
            }
            else if (RootGrid.ActualWidth > 800)
            {
                PreviewPaneDropShadowPanel.SetValue(Grid.RowProperty, 2);
                PreviewPaneDropShadowPanel.SetValue(Grid.ColumnProperty, 2);

                PreviewPaneDropShadowPanel.OffsetX = -2;
                PreviewPaneDropShadowPanel.OffsetY = 0;
                PreviewPaneDropShadowPanel.ShadowOpacity = 0.04;

                PreviewPaneGridSplitter.SetValue(Grid.RowProperty, 2);
                PreviewPaneGridSplitter.SetValue(Grid.ColumnProperty, 1);
                PreviewPaneGridSplitter.Width = 2;
                PreviewPaneGridSplitter.Height = RootGrid.ActualHeight;

                PreviewPaneRow.Height = new GridLength(0);
                PreviewPaneColumn.Width = AppSettings.PreviewPaneSizeVertical;
                PreviewPane.IsHorizontal = false;
            }
            else if (RootGrid.ActualWidth <= 800)
            {
                PreviewPaneRow.Height = AppSettings.PreviewPaneSizeHorizontal;
                PreviewPaneColumn.Width = new GridLength(0);

                PreviewPaneDropShadowPanel.SetValue(Grid.RowProperty, 4);
                PreviewPaneDropShadowPanel.SetValue(Grid.ColumnProperty, 0);

                PreviewPaneDropShadowPanel.OffsetX = 0;
                PreviewPaneDropShadowPanel.OffsetY = -2;
                PreviewPaneDropShadowPanel.ShadowOpacity = 0.04;

                PreviewPaneGridSplitter.SetValue(Grid.RowProperty, 3);
                PreviewPaneGridSplitter.SetValue(Grid.ColumnProperty, 0);
                PreviewPaneGridSplitter.Height = 2;
                PreviewPaneGridSplitter.Width = RootGrid.Width;
                PreviewPane.IsHorizontal = true;
            }
        }

        private void PreviewPaneGridSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (PreviewPane == null)
            {
                return;
            }

            if (PreviewPane.IsHorizontal)
            {
                AppSettings.PreviewPaneSizeHorizontal = new GridLength(PreviewPane.ActualHeight);
            }
            else
            {
                AppSettings.PreviewPaneSizeVertical = new GridLength(PreviewPane.ActualWidth);
            }
        }

        public void NavigateWithArguments(Type sourcePageType, NavigationArguments navArgs)
        {
            NavigateToPath(navArgs.NavPathParam, sourcePageType, navArgs);
        }

        public void NavigateToPath(string navigationPath, Type sourcePageType, NavigationArguments navArgs = null)
        {
            if (sourcePageType == null && !string.IsNullOrEmpty(navigationPath))
            {
                sourcePageType = InstanceViewModel.FolderSettings.GetLayoutType(navigationPath);
            }

            if (navArgs != null && navArgs.AssociatedTabInstance != null)
            {
                ItemDisplayFrame.Navigate(
                sourcePageType,
                navArgs,
                new SuppressNavigationTransitionInfo());
            }
            else
            {
                if (string.IsNullOrEmpty(navigationPath)
                    || string.IsNullOrEmpty(FilesystemViewModel?.WorkingDirectory)
                    || navigationPath.TrimEnd(Path.DirectorySeparatorChar).Equals(
                        FilesystemViewModel.WorkingDirectory.TrimEnd(Path.DirectorySeparatorChar),
                        StringComparison.OrdinalIgnoreCase)) // return if already selected
                {
                    return;
                }

                NavigationTransitionInfo transition = new SuppressNavigationTransitionInfo();

                if (sourcePageType == typeof(WidgetsPage)
                    || ItemDisplayFrame.Content.GetType() == typeof(WidgetsPage) &&
                    (sourcePageType == typeof(DetailsLayoutBrowser) || sourcePageType == typeof(GridViewBrowser)))
                {
                    transition = new EntranceNavigationTransitionInfo();
                }

                ItemDisplayFrame.Navigate(
                sourcePageType,
                new NavigationArguments()
                {
                    NavPathParam = navigationPath,
                    AssociatedTabInstance = this
                },
                transition);
            }

            NavigationToolbar.PathControlDisplayText = FilesystemViewModel.WorkingDirectory;
        }

        public void RemoveLastPageFromBackStack()
        {
            ItemDisplayFrame.BackStack.Remove(ItemDisplayFrame.BackStack.Last());
        }

        public bool LoadPreviewPane => PreviewPaneEnabled && InstanceViewModel.IsPageTypeNotHome;

        public void LoadPreviewPaneChanged()
        {
            NotifyPropertyChanged(nameof(LoadPreviewPane));
            UpdatePositioning();
        }

        private void PreviewPane_Loading(FrameworkElement sender, object args)
        {
            UpdatePositioning();
        }

        private bool previewPaneEnabled = App.AppSettings.PreviewPaneEnabled;
        // This is needed so the layout can be updated when the preview pane is opened
        public bool PreviewPaneEnabled
        {
            get => previewPaneEnabled;
            set
            {
                if(value != previewPaneEnabled)
                {
                    previewPaneEnabled = value;
                    AppSettings.PreviewPaneEnabled = value;
                    NotifyPropertyChanged(nameof(PreviewPaneEnabled));
                    NotifyPropertyChanged(nameof(LoadPreviewPane));
                    UpdatePositioning();
                }
            }
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
        public bool IsLayoutSwitch { get; set; } = false;
        public IEnumerable<string> SelectItems { get; set; }
    }
}