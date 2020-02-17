using Files.Filesystem;
using Files.Interacts;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.IO;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;


namespace Files.Controls
{
    public sealed partial class RibbonArea : UserControl
    {
        public RibbonViewModel RibbonViewModel { get; } = new RibbonViewModel();
        public RibbonArea()
        {
            this.InitializeComponent();
            Window.Current.SizeChanged += Current_SizeChanged;
            Current_SizeChanged(null, null);
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            
            if(Window.Current.Bounds.Width >= 1050)
            {
                RibbonViewModel.ShowItemLabels();
            }
            else
            {
                RibbonViewModel.HideItemLabels();
            }

            if (Window.Current.Bounds.Width >= 700)
            {
                SearchReigon.Visibility = Visibility.Visible;
                ToolbarGrid.ColumnDefinitions[2].MinWidth = 285;
                SearchBoxResizer.Visibility = Visibility.Visible;
                ToolbarGrid.ColumnDefinitions[2].Width = GridLength.Auto;
                RibbonViewModel.ShowAppBarSeparator();
            }
            else
            {
                SearchReigon.Visibility = Visibility.Collapsed;
                ToolbarGrid.ColumnDefinitions[2].MinWidth = 0;
                SearchBoxResizer.Visibility = Visibility.Collapsed;
                ToolbarGrid.ColumnDefinitions[2].Width = new GridLength(0);
                RibbonViewModel.HideAppBarSeparator();
            }
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
                VisiblePath.Visibility = Visibility.Collapsed;
                ClickablePath.Visibility = Visibility.Visible;
            }
            else if (e.Key == VirtualKey.Escape)
            {
                VisiblePath.Visibility = Visibility.Collapsed;
                ClickablePath.Visibility = Visibility.Visible;
            }
        }

        public async void CheckPathInput(ItemViewModel instance, string CurrentInput)
        {
            if (CurrentInput != instance.Universal.path || App.CurrentInstance.ContentFrame.CurrentSourcePageType == typeof(YourHome))
            {
                RibbonViewModel.HomeItems.isEnabled = false;
                RibbonViewModel.ShareItems.isEnabled = false;

                if (CurrentInput == "Favorites" || CurrentInput.Equals("Home", StringComparison.OrdinalIgnoreCase) || CurrentInput == "favorites" || CurrentInput.Equals("New tab", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(YourHome), "New tab");
                    App.CurrentInstance.PathControlDisplayText = "New tab";
                    RibbonViewModel.LayoutItems.isEnabled = false;
                }
                else if (CurrentInput.Equals("Start", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(YourHome), "Start");
                    App.CurrentInstance.PathControlDisplayText = "Start";
                    RibbonViewModel.LayoutItems.isEnabled = false;
                }
                else if (CurrentInput.Equals("Desktop", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.DesktopPath);
                    App.CurrentInstance.PathControlDisplayText = "Desktop";
                    RibbonViewModel.LayoutItems.isEnabled = true;
                }
                else if (CurrentInput.Equals("Documents", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.DocumentsPath);
                    App.CurrentInstance.PathControlDisplayText = "Documents";
                    RibbonViewModel.LayoutItems.isEnabled = true;
                }
                else if (CurrentInput.Equals("Downloads", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.DownloadsPath);
                    App.CurrentInstance.PathControlDisplayText = "Downloads";
                    RibbonViewModel.LayoutItems.isEnabled = true;
                }
                else if (CurrentInput.Equals("Pictures", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(PhotoAlbum), App.AppSettings.PicturesPath);
                    App.CurrentInstance.PathControlDisplayText = "Pictures";
                    RibbonViewModel.LayoutItems.isEnabled = true;
                }
                else if (CurrentInput.Equals("Music", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.MusicPath);
                    App.CurrentInstance.PathControlDisplayText = "Music";
                    RibbonViewModel.LayoutItems.isEnabled = true;
                }
                else if (CurrentInput.Equals("Videos", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.VideosPath);
                    App.CurrentInstance.PathControlDisplayText = "Videos";
                    RibbonViewModel.LayoutItems.isEnabled = true;
                }
                else if (CurrentInput.Equals("OneDrive", StringComparison.OrdinalIgnoreCase))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.OneDrivePath);
                    App.CurrentInstance.PathControlDisplayText = "OneDrive";
                    RibbonViewModel.LayoutItems.isEnabled = true;
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

                                VisiblePath.Text = instance.Universal.path;
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
                                VisiblePath.Text = instance.Universal.path;
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
                            RibbonViewModel.LayoutItems.isEnabled = true;
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
            VisiblePath.Visibility = Visibility.Collapsed;
            ClickablePath.Visibility = Visibility.Visible;
        }

        private void ClickablePathView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PathViewInteract.SelectedIndex = -1;
        }

        private void PathViewInteract_ItemClick(object sender, ItemClickEventArgs e)
        {
            var itemTappedPath = (e.ClickedItem as PathBoxItem).Path.ToString();
            if (itemTappedPath == "Start" || itemTappedPath == "New tab") { return; }

            App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), itemTappedPath, new SuppressNavigationTransitionInfo());
        }

        private void ManualPathEntryItem_Click(object sender, RoutedEventArgs e)
        {
            VisiblePath.Visibility = Visibility.Visible;
            ClickablePath.Visibility = Visibility.Collapsed;
            VisiblePath.Focus(FocusState.Programmatic);
            VisiblePath.SelectAll();
        }

        private void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            (args.Element as AutoSuggestBox).Focus(FocusState.Programmatic);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            instanceTabsView.AddNewTab(typeof(Settings), "Settings");
        }

        private async void AddItem_Click(object sender, RoutedEventArgs e)
        {
            await App.addItemDialog.ShowAsync();
        }

        private async void LayoutButton_Click(object sender, RoutedEventArgs e)
        {
            await App.layoutDialog.ShowAsync();
        }

        public async void ShowPropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            App.propertiesDialog.propertiesFrame.Tag = App.propertiesDialog;
            App.propertiesDialog.propertiesFrame.Navigate(typeof(Properties), (App.CurrentInstance.ContentPage as BaseLayout).SelectedItem, new SuppressNavigationTransitionInfo());
            await App.propertiesDialog.ShowAsync(ContentDialogPlacement.Popup);
        }

        private async void NewWindowButton_Click(object sender, RoutedEventArgs e)
        {
            var filesUWPUri = new Uri("files-uwp:");
            var options = new LauncherOptions()
            {
                DisplayApplicationPicker = false
            };
            await Launcher.LaunchUriAsync(filesUWPUri);
        }

        private void TabViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as TabViewItem);
            //((sender as TabViewItem).Resources["FileClickFlyout"] as FlyoutPresenter).ShowAt((sender as TabViewItem));
        }

        private void MenuFlyout_Closed(object sender, object e)
        {
            HomeRibbonItem.IsSelected = true;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void RibbonItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var itemTapped = sender as TabViewItem;
            if(RibbonTabView.SelectedItem != null)
            {
                RibbonTabView.SelectedItem = null;
            }
            else
            {
                itemTapped.IsSelected = true;
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count > 0)
            {
                (sender as ListView).SelectedItem = null;
            }
        }
    }
}
