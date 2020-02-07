using Files.Filesystem;
using Files.Interacts;
using Files.Navigation;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Files.Enums;
using System.Drawing;
using Files.View_Models;
using Files.Controls;

namespace Files
{
    public sealed partial class ProHome : Page, IShellPage
    {
        Frame IShellPage.ContentFrame => ItemDisplayFrame;

        Interaction IShellPage.InteractionOperations => interactionOperation;

        ItemViewModel IShellPage.ViewModel => viewModel;

        BaseLayout IShellPage.ContentPage => GetContentOrNull();

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

        Control IShellPage.OperationsControl => RibbonArea;

        bool IShellPage.CanRefresh {
            get
            {
                return RibbonArea.Refresh.IsEnabled;
            }
            set
            {
                RibbonArea.Refresh.IsEnabled = value;
            }
        }
        bool IShellPage.CanNavigateToParent
        {
            get
            {
                return RibbonArea.Up.IsEnabled;
            }
            set
            {
                RibbonArea.Up.IsEnabled = value;
            }

        }
        bool IShellPage.CanGoBack
        {
            get
            {
                return RibbonArea.Back.IsEnabled;
            }
            set
            {
                RibbonArea.Back.IsEnabled = value;
            }
        }
        bool IShellPage.CanGoForward
        {
            get
            {
                return RibbonArea.Forward.IsEnabled;
            }
            set
            {
                RibbonArea.Forward.IsEnabled = value;
            }
        }
        string IShellPage.PathControlDisplayText
        {
            get
            {
                return RibbonArea.VisiblePath.Text;
            }
            set
            {
                RibbonArea.VisiblePath.Text = value;
            }
        }
        private ObservableCollection<PathBoxItem> pathComponents = new ObservableCollection<PathBoxItem>();
        ObservableCollection<PathBoxItem> IShellPage.PathComponents => pathComponents;
        Type IShellPage.CurrentPageType => ItemDisplayFrame.SourcePageType;

        public ProHome()
        {
            this.InitializeComponent();
            RibbonArea.VisiblePath.Text = "New tab";
            
            if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.ThemeShadow"))
            {
                themeShadow.Receivers.Add(ShadowReceiver);
            }

            // Acrylic sidebar
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["acrylicSidebar"] != null && localSettings.Values["acrylicSidebar"].Equals(true))
            {
                splitView.PaneBackground = (Brush)Application.Current.Resources["BackgroundAcrylicBrush"];
                Application.Current.Resources["NavigationViewExpandedPaneBackground"] = Application.Current.Resources["BackgroundAcrylicBrush"];
            }

            if (App.AppSettings.DrivesManager.ShowUserConsentOnInit)
            {
                App.AppSettings.DrivesManager.ShowUserConsentOnInit = false;
                DisplayFilesystemConsentDialog();
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
            if (App.CurrentInstance == null && ItemViewModel.GetCurrentSelectedTabInstance<ProHome>().Equals(this))
            {
                App.CurrentInstance = this as IShellPage;
            }

            viewModel = new ItemViewModel();
            interactionOperation = new Interaction();

            switch (NavParams)
            {
                case "Start":
                    ItemDisplayFrame.Navigate(typeof(YourHome), NavParams, new SuppressNavigationTransitionInfo());
                    SidebarControl.SidebarNavView.SelectedItem = App.sideBarItems[0];
                    break;
                case "New tab":
                    ItemDisplayFrame.Navigate(typeof(YourHome), NavParams, new SuppressNavigationTransitionInfo());
                    SidebarControl.SidebarNavView.SelectedItem = App.sideBarItems[0];
                    break;
                case "Desktop":
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.DesktopPath, new SuppressNavigationTransitionInfo());
                    SidebarControl.SidebarNavView.SelectedItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase));
                    break;
                case "Downloads":
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.DownloadsPath, new SuppressNavigationTransitionInfo());
                    SidebarControl.SidebarNavView.SelectedItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase));
                    break;
                case "Documents":
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.DocumentsPath, new SuppressNavigationTransitionInfo());
                    SidebarControl.SidebarNavView.SelectedItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase));
                    break;
                case "Pictures":
                    ItemDisplayFrame.Navigate(typeof(PhotoAlbum), App.AppSettings.PicturesPath, new SuppressNavigationTransitionInfo());
                    SidebarControl.SidebarNavView.SelectedItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase));
                    break;
                case "Music":
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.MusicPath, new SuppressNavigationTransitionInfo());
                    SidebarControl.SidebarNavView.SelectedItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase));
                    break;
                case "Videos":
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.VideosPath, new SuppressNavigationTransitionInfo());
                    SidebarControl.SidebarNavView.SelectedItem = App.sideBarItems.First(x => x.Path.Equals(App.AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase));
                    break;

                default:
                    if (NavParams[0] >= 'A' && NavParams[0] <= 'Z' && NavParams[1] == ':')
                    {
                        ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), NavParams, new SuppressNavigationTransitionInfo());
                        SidebarControl.SidebarNavView.SelectedItem = SettingsViewModel.foundDrives.First(x => x.tag.ToString().Equals($"{NavParams[0]}:\\", StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        SidebarControl.SidebarNavView.SelectedItem = null;
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
            RibbonArea.Focus(FocusState.Programmatic);
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
