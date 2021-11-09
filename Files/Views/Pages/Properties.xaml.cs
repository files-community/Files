using Files.DataModels.NavigationControlItems;
using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using Files.Helpers.XamlHelpers;
using Files.UserControls.Settings;
using Files.ViewModels;
using Files.ViewModels.Properties;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Navigation;

namespace Files.Views
{
    public sealed partial class Properties : Page
    {
        private static ApplicationViewTitleBar TitleBar;

        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private ContentDialog propertiesDialog;

        private object navParameterItem;
        private IShellPage AppInstance;

        private ListedItem listedItem;

        public SettingsViewModel AppSettings => App.AppSettings;

        public Properties()
        {
            InitializeComponent();

            var flowDirectionSetting = ResourceContext.GetForCurrentView().QualifierValues["LayoutDirection"];

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
            TabCustomization.Visibility = listedItem != null && (
                (listedItem.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder && !listedItem.IsZipItem) ||
                (listedItem.IsShortcutItem && !listedItem.IsLinkItem)) ? Visibility.Visible : Visibility.Collapsed;
            TabCompatibility.Visibility = listedItem != null && (
                    ".exe".Equals(listedItem is ShortcutItem sht ? System.IO.Path.GetExtension(sht.TargetPath) : listedItem.FileExtension, StringComparison.OrdinalIgnoreCase)
                ) ? Visibility.Visible : Visibility.Collapsed;
            base.OnNavigatedTo(e);
        }

        private async void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.UI.Xaml.Controls.BackdropMaterial.SetApplyToRootOrPageBackground(sender as Control, true);

            AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                // Set window size in the loaded event to prevent flickering
                TitleBar = ApplicationView.GetForCurrentView().TitleBar;
                TitleBar.ButtonBackgroundColor = Colors.Transparent;
                TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => AppSettings.UpdateThemeElements.Execute(null));
            }
            else
            {
                propertiesDialog = DependencyObjectHelpers.FindParent<ContentDialog>(this);
                propertiesDialog.Closed += PropertiesDialog_Closed;
            }
        }

        private void PropertiesDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
            sender.Closed -= PropertiesDialog_Closed;
            this.FindDescendants().Where(x => x is SettingsBlockControl).Cast<SettingsBlockControl>().Select(x => (x.ExpandableContent as Frame).Content as PropertiesTab).Where(x => x != null).ForEach(tab => tab.Dispose());
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
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RequestedTheme = selectedTheme;
                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
                {
                    switch (RequestedTheme)
                    {
                        case ElementTheme.Default:
                            TitleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
                            TitleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
                            break;

                        case ElementTheme.Light:
                            TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
                            TitleBar.ButtonForegroundColor = Colors.Black;
                            break;

                        case ElementTheme.Dark:
                            TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
                            TitleBar.ButtonForegroundColor = Colors.White;
                            break;
                    }
                }
            });
        }

        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var saveTaks = this.FindDescendants().Where(x => x is SettingsBlockControl).Cast<SettingsBlockControl>().Select(x => (x.ExpandableContent as Frame).Content as PropertiesTab).Where(x => x != null).Select(async (tab) =>
            {
                return await tab.SaveChangesAsync(listedItem);
            });
            if (!(await Task.WhenAll(saveTaks)).All(x => x))
            {
                return;
            }

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                await ApplicationView.GetForCurrentView().TryConsolidateAsync();
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
                await ApplicationView.GetForCurrentView().TryConsolidateAsync();
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
                    await ApplicationView.GetForCurrentView().TryConsolidateAsync();
                }
                else
                {
                    propertiesDialog?.Hide();
                }
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

        private void SettingsBlockControl_Click(object sender, bool isExpanding)
        {
            var tag = (sender as Control).Tag as string;
            var contentFrame = (sender as SettingsBlockControl).ExpandableContent as Frame;
            if (contentFrame.Content != null || !isExpanding)
            {
                return;
            }

            var navParam = new PropertyNavParam()
            {
                tokenSource = tokenSource,
                navParameter = navParameterItem,
                AppInstanceArgument = AppInstance
            };

            switch (tag)
            {
                case "General":
                    contentFrame.Navigate(typeof(PropertiesGeneral), navParam);
                    break;

                case "Shortcut":
                    contentFrame.Navigate(typeof(PropertiesShortcut), navParam);
                    break;

                case "Library":
                    contentFrame.Navigate(typeof(PropertiesLibrary), navParam);
                    break;

                case "Details":
                    contentFrame.Navigate(typeof(PropertiesDetails), navParam);
                    break;

                case "Security":
                    contentFrame.Navigate(typeof(PropertiesSecurity), navParam);
                    break;

                case "Customization":
                    contentFrame.Navigate(typeof(PropertiesCustomization), navParam);
                    break;

                case "Compatibility":
                    contentFrame.Navigate(typeof(PropertiesCompatibility), navParam);
                    break;
            }
        }
    }
}