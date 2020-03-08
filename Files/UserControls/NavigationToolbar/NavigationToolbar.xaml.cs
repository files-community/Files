using Files.Controls;
using Files.Filesystem;
using Files.Interacts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;


namespace Files.UserControls
{
    public sealed partial class NavigationToolbar : UserControl, INavigationToolbar, INotifyPropertyChanged
    {
        private bool manualEntryBoxLoaded = false;
        private bool ManualEntryBoxLoaded
        {
            get
            {
                return manualEntryBoxLoaded;
            }
            set
            {
                if(value != manualEntryBoxLoaded)
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
                if(value != clickablePathLoaded)
                {
                    clickablePathLoaded = value;
                    NotifyPropertyChanged("ClickablePathLoaded");
                }
            }
        }

        private bool SearchBoxLoaded { get; set; }
        private string PathText { get; set; }

        public NavigationToolbar()
        {
            this.InitializeComponent();
            if (Window.Current.Bounds.Width >= 800)
            {
                (this as INavigationToolbar).IsSearchReigonVisible = true;
            }
            else
            {
                (this as INavigationToolbar).IsSearchReigonVisible = false;
            }
        }

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
                    SearchBoxResizer.Visibility = Visibility.Visible;
                    ToolbarGrid.ColumnDefinitions[2].Width = GridLength.Auto;
                    SearchBoxLoaded = true;
                }
                else
                {
                    ToolbarGrid.ColumnDefinitions[2].MinWidth = 0;
                    SearchBoxResizer.Visibility = Visibility.Collapsed;
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
        private ObservableCollection<PathBoxItem> pathComponents = new ObservableCollection<PathBoxItem>();

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
                var CurrentInput = PathBox.Text;
                if (App.CurrentInstance.ContentPage != null)
                {
                    var contentInstance = App.CurrentInstance.ViewModel;
                    CheckPathInput(contentInstance, CurrentInput);
                }
                else if (App.CurrentInstance.CurrentPageType == typeof(YourHome))
                {
                    //if (App.CurrentInstance.ViewModel == null && App.CurrentInstance.InteractionOperations == null)
                    //{
                    //    App.CurrentInstance.ViewModel = new ItemViewModel();
                    //    App.CurrentInstance.InteractionOperations = new Interaction();
                    //}

                    var contentInstance = App.CurrentInstance.ViewModel;
                    CheckPathInput(contentInstance, CurrentInput);
                }
                App.CurrentInstance.NavigationControl.IsEditModeEnabled = true;

            }
            else if (e.Key == VirtualKey.Escape)
            {
                App.CurrentInstance.NavigationControl.IsEditModeEnabled = false;
            }
        }

        public async void CheckPathInput(ItemViewModel instance, string CurrentInput)
        {
            if (CurrentInput != instance.Universal.path || App.CurrentInstance.ContentFrame.CurrentSourcePageType == typeof(YourHome))
            {
                (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = false;
                (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = false;

                if (CurrentInput == "Favorites" || CurrentInput.Equals("Home", StringComparison.OrdinalIgnoreCase) || CurrentInput == "favorites" || CurrentInput.Equals("New tab", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(YourHome), "New tab");
                    App.CurrentInstance.NavigationControl.PathControlDisplayText = "New tab";
                    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = false;
                }
                else if (CurrentInput.Equals("Start", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(YourHome), "Start");
                    App.CurrentInstance.NavigationControl.PathControlDisplayText = "Start";
                    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = false;
                }
                else if (CurrentInput.Equals("Desktop", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.DesktopPath);
                    App.CurrentInstance.NavigationControl.PathControlDisplayText = "Desktop";
                    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                }
                else if (CurrentInput.Equals("Documents", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.DocumentsPath);
                    App.CurrentInstance.NavigationControl.PathControlDisplayText = "Documents";
                    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                }
                else if (CurrentInput.Equals("Downloads", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.DownloadsPath);
                    App.CurrentInstance.NavigationControl.PathControlDisplayText = "Downloads";
                    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                }
                else if (CurrentInput.Equals("Pictures", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(PhotoAlbum), App.AppSettings.PicturesPath);
                    App.CurrentInstance.NavigationControl.PathControlDisplayText = "Pictures";
                    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                }
                else if (CurrentInput.Equals("Music", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.MusicPath);
                    App.CurrentInstance.NavigationControl.PathControlDisplayText = "Music";
                    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                }
                else if (CurrentInput.Equals("Videos", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.VideosPath);
                    App.CurrentInstance.NavigationControl.PathControlDisplayText = "Videos";
                    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                }
                else if (CurrentInput.Equals("OneDrive", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.OneDrivePath);
                    App.CurrentInstance.NavigationControl.PathControlDisplayText = "OneDrive";
                    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                }
                else if (CurrentInput.Equals("%temp%", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), (string)Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Environment", "TEMP", null));
                    App.CurrentInstance.NavigationControl.PathControlDisplayText = instance.Universal.path;
                }
                else if (CurrentInput.Equals("%AppData%", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
                    App.CurrentInstance.NavigationControl.PathControlDisplayText = instance.Universal.path;
                }
                else if (CurrentInput.Equals("%HOMEPATH%", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                    App.CurrentInstance.NavigationControl.PathControlDisplayText = instance.Universal.path;
                }
                else if (CurrentInput.Equals("%WinDir%", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), Environment.GetFolderPath(Environment.SpecialFolder.Windows));
                    App.CurrentInstance.NavigationControl.PathControlDisplayText = instance.Universal.path;
                }
                else
                {
                    if (CurrentInput.Contains("."))
                    {
                        if (CurrentInput.Contains(".exe") || CurrentInput.Contains(".EXE"))
                        {
                            if (StorageFile.GetFileFromPathAsync(CurrentInput) != null)
                            {
                                if (App.CurrentInstance.ContentFrame.SourcePageType == typeof(GenericFileBrowser))
                                {
                                    await Interaction.InvokeWin32Component(CurrentInput);
                                }
                                else if (App.CurrentInstance.ContentFrame.SourcePageType == typeof(PhotoAlbum))
                                {
                                    await Interaction.InvokeWin32Component(CurrentInput);
                                }

                                App.CurrentInstance.NavigationControl.PathControlDisplayText = instance.Universal.path;
                            }
                            else
                            {
                                MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                await dialog.ShowAsync();
                            }
                        }
                        else if (StorageFolder.GetFolderFromPathAsync(CurrentInput) != null)
                        {
                            try
                            {
                                await StorageFolder.GetFolderFromPathAsync(CurrentInput);
                                App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), CurrentInput);
                            }
                            catch (ArgumentException)
                            {
                                MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                await dialog.ShowAsync();
                            }
                            catch (FileNotFoundException)
                            {
                                MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                await dialog.ShowAsync();
                            }
                            catch (Exception)
                            {
                                MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                await dialog.ShowAsync();
                            }

                        }
                        else
                        {
                            try
                            {
                                await StorageFile.GetFileFromPathAsync(CurrentInput);
                                StorageFile file = await StorageFile.GetFileFromPathAsync(CurrentInput);
                                var options = new LauncherOptions
                                {
                                    DisplayApplicationPicker = false

                                };
                                await Launcher.LaunchFileAsync(file, options);
                                App.CurrentInstance.NavigationControl.PathControlDisplayText = instance.Universal.path;
                            }
                            catch (ArgumentException)
                            {
                                MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                await dialog.ShowAsync();
                            }
                            catch (FileNotFoundException)
                            {
                                MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                await dialog.ShowAsync();
                            }
                            catch (Exception)
                            {
                                MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                await dialog.ShowAsync();
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            await StorageFolder.GetFolderFromPathAsync(CurrentInput);
                            App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), CurrentInput);
                            (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                        }
                        catch (ArgumentException)
                        {
                            MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                            await dialog.ShowAsync();
                        }
                        catch (FileNotFoundException)
                        {
                            MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                            await dialog.ShowAsync();
                        }
                        catch (System.Exception)
                        {
                            MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                            await dialog.ShowAsync();
                        }

                    }

                }
            }
        }
        private void VisiblePath_LostFocus(object sender, RoutedEventArgs e)
        {
            var element = FocusManager.GetFocusedElement() as Control;
            if(element.FocusState != FocusState.Programmatic && element.FocusState != FocusState.Keyboard)
            {
                App.CurrentInstance.NavigationControl.IsEditModeEnabled = false;
            }
            else
            {
                this.VisiblePath.Focus(FocusState.Programmatic);
            }
        }

        private void PathViewInteract_ItemClick(object sender, ItemClickEventArgs e)
        {
            var itemTappedPath = (e.ClickedItem as PathBoxItem).Path.ToString();
            if (itemTappedPath == "Start" || itemTappedPath == "New tab") { return; }

            App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), itemTappedPath, new SuppressNavigationTransitionInfo());
        }
    }
}
