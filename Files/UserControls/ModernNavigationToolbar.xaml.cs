using Files.Common;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.View_Models;
using Files.Views.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Files.UserControls
{
    public sealed partial class ModernNavigationToolbar : UserControl, INavigationToolbar, INotifyPropertyChanged
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public ModernNavigationToolbar()
        {
            this.InitializeComponent();
        }

        private bool manualEntryBoxLoaded = false;

        private bool ManualEntryBoxLoaded
        {
            get
            {
                return manualEntryBoxLoaded;
            }
            set
            {
                if (value != manualEntryBoxLoaded)
                {
                    manualEntryBoxLoaded = value;
                    NotifyPropertyChanged(nameof(ManualEntryBoxLoaded));
                }
            }
        }

        private bool clickablePathLoaded = true;

        private bool ClickablePathLoaded
        {
            get
            {
                return clickablePathLoaded;
            }
            set
            {
                if (value != clickablePathLoaded)
                {
                    clickablePathLoaded = value;
                    NotifyPropertyChanged(nameof(ClickablePathLoaded));
                }
            }
        }

        private bool SearchBoxLoaded { get; set; } = false;
        private string PathText { get; set; }

        bool INavigationToolbar.IsSearchReigonVisible
        {
            get
            {
                return SearchBoxLoaded;
            }
            set
            {
                if (value)
                {
                    ToolbarGrid.ColumnDefinitions[2].MinWidth = 285;
                    ToolbarGrid.ColumnDefinitions[2].Width = GridLength.Auto;
                    SearchBoxLoaded = true;
                }
                else
                {
                    ToolbarGrid.ColumnDefinitions[2].MinWidth = 0;
                    ToolbarGrid.ColumnDefinitions[2].Width = new GridLength(0);
                    SearchBoxLoaded = false;
                }
            }
        }

        bool INavigationToolbar.IsEditModeEnabled
        {
            get
            {
                return ManualEntryBoxLoaded;
            }
            set
            {
                if (value)
                {
                    ManualEntryBoxLoaded = true;
                    ClickablePathLoaded = false;
                    VisiblePath.Focus(FocusState.Programmatic);
                    VisiblePath.Text = string.IsNullOrEmpty(App.CurrentInstance.FilesystemViewModel.WorkingDirectory)
                        ? AppSettings.HomePath
                        : App.CurrentInstance.FilesystemViewModel.WorkingDirectory;
                    Interaction.FindChild<TextBox>(VisiblePath)?.SelectAll();
                }
                else
                {
                    ManualEntryBoxLoaded = false;
                    ClickablePathLoaded = true;
                }
            }
        }

        public ObservableCollection<ListedItem> NavigationBarSuggestions = new ObservableCollection<ListedItem>();

        private void VisiblePath_Loaded(object sender, RoutedEventArgs e)
        {
            // AutoSuggestBox won't receive focus unless it's fully loaded
            VisiblePath.Focus(FocusState.Programmatic);
            Interaction.FindChild<TextBox>(VisiblePath)?.SelectAll();
        }

        bool INavigationToolbar.CanRefresh
        {
            get
            {
                return Refresh.IsEnabled;
            }
            set
            {
                Refresh.IsEnabled = value;
            }
        }

        bool INavigationToolbar.CanNavigateToParent
        {
            get
            {
                return Up.IsEnabled;
            }
            set
            {
                Up.IsEnabled = value;
            }
        }

        bool INavigationToolbar.CanGoBack
        {
            get
            {
                return Back.IsEnabled;
            }
            set
            {
                Back.IsEnabled = value;
            }
        }

        bool INavigationToolbar.CanGoForward
        {
            get
            {
                return Forward.IsEnabled;
            }
            set
            {
                Forward.IsEnabled = value;
            }
        }

        string INavigationToolbar.PathControlDisplayText
        {
            get
            {
                return PathText;
            }
            set
            {
                PathText = value;
                NotifyPropertyChanged(nameof(PathText));
            }
        }

        private readonly ObservableCollection<PathBoxItem> pathComponents = new ObservableCollection<PathBoxItem>();

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        ObservableCollection<PathBoxItem> INavigationToolbar.PathComponents => pathComponents;

        public UserControl MultiTaskingControl => VerticalTabs;

        private void ManualPathEntryItem_Click(object sender, RoutedEventArgs e)
        {
            (this as INavigationToolbar).IsEditModeEnabled = true;
        }

        private void VisiblePath_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                App.CurrentInstance.NavigationToolbar.IsEditModeEnabled = false;
            }
        }

        public async void CheckPathInput(ItemViewModel instance, string currentInput, string currentSelectedPath)
        {
            if (currentSelectedPath == currentInput) return;

            if (currentInput != instance.WorkingDirectory || App.CurrentInstance.ContentFrame.CurrentSourcePageType == typeof(YourHome))
            {
                //(App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = false;
                //(App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = false;

                if (currentInput.Equals("Home", StringComparison.OrdinalIgnoreCase) || currentInput.Equals(ResourceController.GetTranslation("NewTab"), StringComparison.OrdinalIgnoreCase))
                {
                    await App.CurrentInstance.FilesystemViewModel.SetWorkingDirectory(ResourceController.GetTranslation("NewTab"));
                    App.CurrentInstance.ContentFrame.Navigate(typeof(YourHome), ResourceController.GetTranslation("NewTab"), new SuppressNavigationTransitionInfo());
                }
                else
                {
                    var workingDir = string.IsNullOrEmpty(App.CurrentInstance.FilesystemViewModel.WorkingDirectory)
                        ? AppSettings.HomePath
                        : App.CurrentInstance.FilesystemViewModel.WorkingDirectory;

                    currentInput = StorageFileExtensions.GetPathWithoutEnvironmentVariable(currentInput);
                    if (currentSelectedPath == currentInput) return;
                    var item = await DrivesManager.GetRootFromPath(currentInput);

                    try
                    {
                        var pathToNavigate = (await StorageFileExtensions.GetFolderWithPathFromPathAsync(currentInput, item)).Path;
                        App.CurrentInstance.ContentFrame.Navigate(AppSettings.GetLayoutType(), pathToNavigate); // navigate to folder
                    }
                    catch (Exception) // Not a folder or inaccessible
                    {
                        try
                        {
                            var pathToInvoke = (await StorageFileExtensions.GetFileWithPathFromPathAsync(currentInput, item)).Path;
                            await Interaction.InvokeWin32Component(pathToInvoke);
                        }
                        catch (Exception ex) // Not a file or not accessible
                        {
                            // Launch terminal application if possible
                            foreach (var terminal in AppSettings.TerminalController.Model.Terminals)
                            {
                                if (terminal.Path.Equals(currentInput, StringComparison.OrdinalIgnoreCase) || terminal.Path.Equals(currentInput + ".exe", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (App.Connection != null)
                                    {
                                        var value = new ValueSet
                                        {
                                            { "WorkingDirectory", workingDir },
                                            { "Application", terminal.Path },
                                            { "Arguments", string.Format(terminal.Arguments,
                                            Helpers.PathNormalization.NormalizePath(App.CurrentInstance.FilesystemViewModel.WorkingDirectory)) }
                                        };
                                        await App.Connection.SendMessageAsync(value);
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

                App.CurrentInstance.NavigationToolbar.PathControlDisplayText = App.CurrentInstance.FilesystemViewModel.WorkingDirectory;
            }
        }

        private void VisiblePath_LostFocus(object sender, RoutedEventArgs e)
        {
            if (FocusManager.GetFocusedElement() is FlyoutBase ||
                FocusManager.GetFocusedElement() is AppBarButton ||
                FocusManager.GetFocusedElement() is Popup)
            {
                return;
            }

            var element = FocusManager.GetFocusedElement();
            var elementAsControl = element as Control;

            if (elementAsControl.FocusState != FocusState.Programmatic && elementAsControl.FocusState != FocusState.Keyboard)
            {
                App.CurrentInstance.NavigationToolbar.IsEditModeEnabled = false;
            }
            else
            {
                if (App.CurrentInstance.NavigationToolbar.IsEditModeEnabled)
                {
                    this.VisiblePath.Focus(FocusState.Programmatic);
                }
            }
        }

        private async void Button_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                e.Handled = true;
                cancelFlyoutOpen = false;
                await Task.Delay(1000);
                if (!cancelFlyoutOpen)
                {
                    (sender as Button).Flyout.ShowAt(sender as Button);
                    cancelFlyoutOpen = false;
                }
                else
                {
                    cancelFlyoutOpen = false;
                }
            }
        }

        private bool cancelFlyoutOpen = false;

        private void VerticalTabStripInvokeButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                e.Handled = true;
                if (!(sender as Button).Flyout.IsOpen)
                {
                    cancelFlyoutOpen = true;
                }
            }
        }

        private void Button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            (sender as Button).Flyout.ShowAt(sender as Button);
        }

        private void Flyout_Opened(object sender, object e)
        {
            VisualStateManager.GoToState(VerticalTabStripInvokeButton, "PointerOver", false);
        }

        private void Flyout_Closed(object sender, object e)
        {
            VisualStateManager.GoToState(VerticalTabStripInvokeButton, "Normal", false);
        }

        private void VerticalTabStripInvokeButton_DragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;
            (sender as Button).Flyout.ShowAt(sender as Button);
        }

        private bool cancelFlyoutAutoClose = false;

        private async void VerticalTabs_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                e.Handled = true;
                cancelFlyoutAutoClose = false;
                VerticalTabs.PointerEntered += VerticalTabs_PointerEntered;
                await Task.Delay(1000);
                VerticalTabs.PointerEntered -= VerticalTabs_PointerEntered;
                if (!cancelFlyoutAutoClose)
                {
                    VerticalTabViewFlyout.Hide();
                }
                cancelFlyoutAutoClose = false;
            }
        }

        private void VerticalTabs_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                e.Handled = true;
                cancelFlyoutAutoClose = true;
            }
        }

        private async void PathBoxItem_DragOver(object sender, DragEventArgs e)
        {
            if (!((sender as Grid).DataContext is PathBoxItem pathBoxItem) ||
                pathBoxItem.Path == "Home" || pathBoxItem.Path == ResourceController.GetTranslation("NewTab"))
            {
                return;
            }

            if (!e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.None;
                return;
            }

            e.Handled = true;
            var deferral = e.GetDeferral();

            var storageItems = await e.DataView.GetStorageItemsAsync();
            if (!storageItems.Any(storageItem =>
            storageItem.Path.Replace(pathBoxItem.Path, string.Empty).
            Trim(Path.DirectorySeparatorChar).
            Contains(Path.DirectorySeparatorChar)))
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            }
            else
            {
                e.DragUIOverride.IsCaptionVisible = true;
                e.DragUIOverride.Caption = string.Format(ResourceController.GetTranslation("MoveToFolderCaptionText"), pathBoxItem.Title);
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            }

            deferral.Complete();
        }

        private async void PathBoxItem_Drop(object sender, DragEventArgs e)
        {
            if (!((sender as Grid).DataContext is PathBoxItem pathBoxItem) ||
                pathBoxItem.Path == "Home" || pathBoxItem.Path == ResourceController.GetTranslation("NewTab"))
            {
                return;
            }

            var deferral = e.GetDeferral();
            await App.CurrentInstance.InteractionOperations.PasteItems(e.DataView, pathBoxItem.Path, e.AcceptedOperation);
            deferral.Complete();
        }

        private void VisiblePath_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                SetAddressBarSuggestions(sender);
            }
        }

        private async void SetAddressBarSuggestions(AutoSuggestBox sender, int maxSuggestions = 7)
        {
            try
            {
                IList<ListedItem> suggestions = null;
                var expandedPath = StorageFileExtensions.GetPathWithoutEnvironmentVariable(sender.Text);
                var folderPath = Path.GetDirectoryName(expandedPath) ?? expandedPath;
                var folder = await ItemViewModel.GetFolderWithPathFromPathAsync(folderPath);
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
                        ItemPath = App.CurrentInstance.FilesystemViewModel.WorkingDirectory,
                        ItemName = ResourceController.GetTranslation("NavigationToolbarVisiblePathNoResults") } };
                }

                // NavigationBarSuggestions becoming empty causes flickering of the suggestion box
                // Here we check whether at least an element is in common between old and new list
                if (!NavigationBarSuggestions.IntersectBy(suggestions, x => x.ItemName).Any())
                {
                    // No elemets in common, update the list in-place
                    for (int si = 0; si < suggestions.Count; si++)
                    {
                        if (si < NavigationBarSuggestions.Count)
                        {
                            NavigationBarSuggestions[si].ItemName = suggestions[si].ItemName;
                            NavigationBarSuggestions[si].ItemPath = suggestions[si].ItemPath;
                        }
                        else
                        {
                            NavigationBarSuggestions.Add(suggestions[si]);
                        }
                    }
                    while (NavigationBarSuggestions.Count > suggestions.Count)
                    {
                        NavigationBarSuggestions.RemoveAt(NavigationBarSuggestions.Count - 1);
                    }
                }
                else
                {
                    // At least an element in common, show animation
                    foreach (var s in NavigationBarSuggestions.ExceptBy(suggestions, x => x.ItemName).ToList())
                    {
                        NavigationBarSuggestions.Remove(s);
                    }
                    foreach (var s in suggestions.ExceptBy(NavigationBarSuggestions, x => x.ItemName).ToList())
                    {
                        NavigationBarSuggestions.Insert(suggestions.IndexOf(s), s);
                    }
                }
            }
            catch
            {
                NavigationBarSuggestions.Clear();
                NavigationBarSuggestions.Add(new ListedItem(null)
                {
                    ItemPath = App.CurrentInstance.FilesystemViewModel.WorkingDirectory,
                    ItemName = ResourceController.GetTranslation("NavigationToolbarVisiblePathNoResults")
                });
            }
        }

        private void VisiblePath_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            CheckPathInput(App.CurrentInstance.FilesystemViewModel, args.QueryText,
                App.CurrentInstance.NavigationToolbar.PathComponents[App.CurrentInstance.NavigationToolbar.PathComponents.Count - 1].Path);
            App.CurrentInstance.NavigationToolbar.IsEditModeEnabled = false;
        }

        private void PathBoxItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var itemTappedPath = ((sender as TextBlock).DataContext as PathBoxItem).Path;

            App.CurrentInstance.ContentFrame.Navigate(AppSettings.GetLayoutType(), itemTappedPath); // navigate to folder
        }

        private async void PathItemSeparator_Loaded(object sender, RoutedEventArgs e)
        {
            var pathSeparatorIcon = sender as FontIcon;
            await SetPathBoxDropDownFlyout(pathSeparatorIcon.ContextFlyout as MenuFlyout, pathSeparatorIcon.DataContext as PathBoxItem);

            pathSeparatorIcon.Tapped += (s, e) => pathSeparatorIcon.ContextFlyout.ShowAt(pathSeparatorIcon);
            pathSeparatorIcon.ContextFlyout.Opened += (s, e) => { pathSeparatorIcon.Glyph = "\uE9A5"; };
            pathSeparatorIcon.ContextFlyout.Closed += (s, e) => { pathSeparatorIcon.Glyph = "\uE9A8"; };
        }

        private async void PathboxItemFlyout_Opened(object sender, object e)
        {
            var flyout = sender as MenuFlyout;
            await SetPathBoxDropDownFlyout(flyout, (flyout.Target as FontIcon).DataContext as PathBoxItem);
        }

        private async Task SetPathBoxDropDownFlyout(MenuFlyout flyout, PathBoxItem pathItem)
        {
            var nextPathItemTitle = App.CurrentInstance.NavigationToolbar.PathComponents
                [App.CurrentInstance.NavigationToolbar.PathComponents.IndexOf(pathItem) + 1].Title;
            IList<StorageFolderWithPath> childFolders = new List<StorageFolderWithPath>();

            try
            {
                var folder = await ItemViewModel.GetFolderWithPathFromPathAsync(pathItem.Path);
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

            var workingPath = App.CurrentInstance.NavigationToolbar.PathComponents
                    [App.CurrentInstance.NavigationToolbar.PathComponents.Count - 1].
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
                    flyoutItem.Click += (sender, args) => App.CurrentInstance.ContentFrame.Navigate(AppSettings.GetLayoutType(), childFolder.Path);
                }

                flyout.Items.Add(flyoutItem);
            }
        }

        private void VerticalTabStripInvokeButton_Loaded(object sender, RoutedEventArgs e)
        {
            App.MultitaskingControl = VerticalTabs;
        }
    }
}