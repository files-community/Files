using Files.Filesystem;
using Files.Interacts;
using Files.Views.Pages;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;

namespace Files.UserControls
{
    public sealed partial class ModernNavigationToolbar : UserControl, INavigationToolbar, INotifyPropertyChanged
    {
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
                    NotifyPropertyChanged("ManualEntryBoxLoaded");
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
                    NotifyPropertyChanged("ClickablePathLoaded");
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
                }
                else
                {
                    ManualEntryBoxLoaded = false;
                    ClickablePathLoaded = true;
                }
            }
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
                NotifyPropertyChanged("PathText");
            }
        }

        private readonly ObservableCollection<PathBoxItem> pathComponents = new ObservableCollection<PathBoxItem>();

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        ObservableCollection<PathBoxItem> INavigationToolbar.PathComponents => pathComponents;

        private void ManualPathEntryItem_Click(object sender, RoutedEventArgs e)
        {
            (this as INavigationToolbar).IsEditModeEnabled = true;
            VisiblePath.Focus(FocusState.Programmatic);
            VisiblePath.SelectAll();
        }

        private void VisiblePath_TextChanged(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                var PathBox = (sender as TextBox);
                CheckPathInput(App.CurrentInstance.ViewModel, PathBox.Text);
                App.CurrentInstance.NavigationToolbar.IsEditModeEnabled = false;
            }
            else if (e.Key == VirtualKey.Escape)
            {
                App.CurrentInstance.NavigationToolbar.IsEditModeEnabled = false;
            }
        }

        public async void CheckPathInput(ItemViewModel instance, string CurrentInput)
        {
            if (CurrentInput != instance.WorkingDirectory || App.CurrentInstance.ContentFrame.CurrentSourcePageType == typeof(YourHome))
            {
                //(App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = false;
                //(App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = false;

                if (CurrentInput.Equals("Home", StringComparison.OrdinalIgnoreCase) || CurrentInput.Equals("New tab", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ViewModel.WorkingDirectory = "New tab";
                    App.CurrentInstance.ContentFrame.Navigate(typeof(YourHome), "New tab", new SuppressNavigationTransitionInfo());
                }
                else
                {
                    switch (CurrentInput.ToLower())
                    {
                        case "%temp%":
                            CurrentInput = App.AppSettings.TempPath;
                            break;

                        case "%appdata":
                            CurrentInput = App.AppSettings.AppDataPath;
                            break;

                        case "%homepath%":
                            CurrentInput = App.AppSettings.HomePath;
                            break;

                        case "%windir%":
                            CurrentInput = App.AppSettings.WinDirPath;
                            break;
                    }

                    try
                    {
                        await StorageFolder.GetFolderFromPathAsync(CurrentInput);

                        if (App.AppSettings.LayoutMode == 0) // List View
                        {
                            App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), CurrentInput); // navigate to folder
                        }
                        else
                        {
                            App.CurrentInstance.ContentFrame.Navigate(typeof(PhotoAlbum), CurrentInput); // navigate to folder
                        }
                    }
                    catch (Exception) // Not a folder or inaccessible
                    {
                        try
                        {
                            await StorageFile.GetFileFromPathAsync(CurrentInput);
                            await Interaction.InvokeWin32Component(CurrentInput);
                        }
                        catch (Exception ex) // Not a file or not accessible
                        {
                            // Launch terminal application if possible
                            var localSettings = ApplicationData.Current.LocalSettings;

                            foreach (var item in App.AppSettings.Terminals)
                            {
                                if (item.Path.Equals(CurrentInput, StringComparison.OrdinalIgnoreCase) || item.Path.Equals(CurrentInput + ".exe", StringComparison.OrdinalIgnoreCase))
                                {
                                    localSettings.Values["Application"] = item.Path;
                                    localSettings.Values["Arguments"] = String.Format(item.arguments, App.CurrentInstance.ViewModel.WorkingDirectory);

                                    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();

                                    return;
                                }
                            }

                            var dialog = new ContentDialog()
                            {
                                Title = "Invalid item",
                                Content = "The item referenced is either invalid or inaccessible.\nMessage:\n\n" + ex.Message,
                                CloseButtonText = "OK"
                            };

                            await dialog.ShowAsync();
                        }
                    }
                }

                App.CurrentInstance.NavigationToolbar.PathControlDisplayText = App.CurrentInstance.ViewModel.WorkingDirectory;
            }
        }

        private void VisiblePath_LostFocus(object sender, RoutedEventArgs e)
        {
            if (FocusManager.GetFocusedElement() is FlyoutBase || FocusManager.GetFocusedElement() is AppBarButton || FocusManager.GetFocusedElement() is Popup) { return; }

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

        private void PathViewInteract_ItemClick(object sender, ItemClickEventArgs e)
        {
            var itemTappedPath = (e.ClickedItem as PathBoxItem).Path.ToString();
            if (itemTappedPath == "Home" || itemTappedPath == "New tab")
                return;

            if (App.AppSettings.LayoutMode == 0) // List View
            {
                App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), itemTappedPath); // navigate to folder
            }
            else
            {
                App.CurrentInstance.ContentFrame.Navigate(typeof(PhotoAlbum), itemTappedPath); // navigate to folder
            }
        }
    }
}