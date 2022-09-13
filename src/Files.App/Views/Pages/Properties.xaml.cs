using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.XamlHelpers;
using Files.App.ViewModels;
using Files.App.ViewModels.Properties;
using CommunityToolkit.WinUI;
using System;
using System.Threading;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI;
using Microsoft.UI.Windowing;

namespace Files.App.Views
{
    public sealed partial class Properties : Page
    {
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private ContentDialog propertiesDialog;

        private object navParameterItem;
        private IShellPage AppInstance;

        private ListedItem listedItem;

        public SettingsViewModel AppSettings => App.AppSettings;

        public AppWindow appWindow;

        public Properties()
        {
            InitializeComponent();

            var flowDirectionSetting = /*
                TODO ResourceContext.GetForCurrentView and ResourceContext.GetForViewIndependentUse do not exist in Windows App SDK
                Use your ResourceManager instance to create a ResourceContext as below. If you already have a ResourceManager instance,
                replace the new instance created below with correct instance.
                Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/mrtcore
            */new Microsoft.Windows.ApplicationModel.Resources.ResourceManager().CreateResourceContext().QualifierValues["LayoutDirection"];

            if (flowDirectionSetting == "RTL")
            {
                FlowDirection = FlowDirection.RightToLeft;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var args = e.Parameter as PropertiesPageNavigationArguments;
            AppInstance = args.AppInstanceArgument;
            navParameterItem = args.Item;
            listedItem = args.Item as ListedItem;
            TabShorcut.Visibility = listedItem != null && listedItem.IsShortcutItem ? Visibility.Visible : Visibility.Collapsed;
            TabLibrary.Visibility = listedItem != null && listedItem.IsLibraryItem ? Visibility.Visible : Visibility.Collapsed;
            TabDetails.Visibility = listedItem != null && listedItem.FileExtension != null && !listedItem.IsShortcutItem && !listedItem.IsLibraryItem ? Visibility.Visible : Visibility.Collapsed;
            TabSecurity.Visibility = args.Item is DriveItem ||
                (listedItem != null && !listedItem.IsLibraryItem && !listedItem.IsRecycleBinItem) ? Visibility.Visible : Visibility.Collapsed;
            TabCustomization.Visibility = listedItem != null && !listedItem.IsLibraryItem && (
                (listedItem.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder && !listedItem.IsZipItem) ||
                (listedItem.IsShortcutItem && !listedItem.IsLinkItem)) ? Visibility.Visible : Visibility.Collapsed;
            TabCompatibility.Visibility = listedItem != null && (
                    ".exe".Equals(listedItem is ShortcutItem sht ? System.IO.Path.GetExtension(sht.TargetPath) : listedItem.FileExtension, StringComparison.OrdinalIgnoreCase)
                ) ? Visibility.Visible : Visibility.Collapsed;
            base.OnNavigatedTo(e);
        }

        private async void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                //Window.Current.SetTitleBar(TitleBarDragArea); //WINUI3, SetDragRectangles?
                appWindow.Destroying += AppWindow_Destroying;
                await App.Window.DispatcherQueue.EnqueueAsync(() => AppSettings.UpdateThemeElements.Execute(null));
            }
            else
            {
                propertiesDialog = DependencyObjectHelpers.FindParent<ContentDialog>(this);
                propertiesDialog.Closed += PropertiesDialog_Closed;
            }
        }

        private void AppWindow_Destroying(AppWindow sender, object args)
        {
            AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
            sender.Destroying -= AppWindow_Destroying;
            if (tokenSource != null && !tokenSource.IsCancellationRequested)
            {
                tokenSource.Cancel();
                tokenSource = null;
            }
        }

        private void PropertiesDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
            sender.Closed -= PropertiesDialog_Closed;
            if (tokenSource != null && !tokenSource.IsCancellationRequested)
            {
                tokenSource.Cancel();
                tokenSource = null;
            }
            propertiesDialog.Hide();
        }

        private void Properties_Unloaded(object sender, RoutedEventArgs e)
        {
            // Why is this not called? Are we cleaning up properly?
        }

        private async void AppSettings_ThemeModeChanged(object sender, EventArgs e)
        {
            var selectedTheme = ThemeHelper.RootTheme;
            await DispatcherQueue.EnqueueAsync(() =>
            {
                ((Frame)Parent).RequestedTheme = selectedTheme;
                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
                {
                    switch (selectedTheme)
                    {
                        case ElementTheme.Default:
                            appWindow.TitleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
                            appWindow.TitleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
                            break;

                        case ElementTheme.Light:
                            appWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
                            appWindow.TitleBar.ButtonForegroundColor = Colors.Black;
                            break;

                        case ElementTheme.Dark:
                            appWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
                            appWindow.TitleBar.ButtonForegroundColor = Colors.White;
                            break;
                    }
                }
            });
        }

        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (contentFrame.Content is PropertiesGeneral propertiesGeneral)
            {
                await propertiesGeneral.SaveChangesAsync(listedItem);
            }
            else
            {
                if (!await (contentFrame.Content as PropertiesTab).SaveChangesAsync(listedItem))
                {
                    return;
                }
            }

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                appWindow.Destroy();
            }
            else
            {
                propertiesDialog?.Hide();
            }
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                appWindow.Destroy();
            }
            else
            {
                propertiesDialog?.Hide();
            }
        }

        private async void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key.Equals(VirtualKey.Escape))
            {
                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
                {
                    appWindow.Destroy();
                }
                else
                {
                    propertiesDialog?.Hide();
                }
            }
        }

        private void NavigationView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            var navParam = new PropertyNavParam()
            {
                tokenSource = tokenSource,
                navParameter = navParameterItem,
                AppInstanceArgument = AppInstance
            };

            switch (args.SelectedItemContainer.Tag)
            {
                case "General":
                    contentFrame.Navigate(typeof(PropertiesGeneral), navParam, args.RecommendedNavigationTransitionInfo);
                    break;

                case "Shortcut":
                    contentFrame.Navigate(typeof(PropertiesShortcut), navParam, args.RecommendedNavigationTransitionInfo);
                    break;

                case "Library":
                    contentFrame.Navigate(typeof(PropertiesLibrary), navParam, args.RecommendedNavigationTransitionInfo);
                    break;

                case "Details":
                    contentFrame.Navigate(typeof(PropertiesDetails), navParam, args.RecommendedNavigationTransitionInfo);
                    break;

                case "Security":
                    contentFrame.Navigate(typeof(PropertiesSecurity), navParam, args.RecommendedNavigationTransitionInfo);
                    break;

                case "Customization":
                    contentFrame.Navigate(typeof(PropertiesCustomization), navParam, args.RecommendedNavigationTransitionInfo);
                    break;

                case "Compatibility":
                    contentFrame.Navigate(typeof(PropertiesCompatibility), navParam, args.RecommendedNavigationTransitionInfo);
                    break;
            }
        }

        public class PropertiesPageNavigationArguments
        {
            public object Item { get; set; }
            public IShellPage AppInstanceArgument { get; set; }
        }

        public class PropertyNavParam
        {
            public CancellationTokenSource tokenSource;
            public object navParameter;
            public IShellPage AppInstanceArgument { get; set; }
        }

        private void Page_Loading(FrameworkElement sender, object args)
        {
            // This manually adds the user's theme resources to the page
            // I was unable to get this to work any other way
            try
            {
                var xaml = XamlReader.Load(App.ExternalResourcesHelper.CurrentThemeResources) as ResourceDictionary;
                App.Current.Resources.MergedDictionaries.Add(xaml);
            }
            catch (Exception)
            {
            }
        }
    }
}