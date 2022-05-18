using Files.Uwp.DataModels.NavigationControlItems;
using Files.Uwp.Filesystem;
using Files.Uwp.Helpers;
using Files.Uwp.Helpers.XamlHelpers;
using Files.Uwp.ViewModels;
using Files.Uwp.ViewModels.Properties;
using Microsoft.Toolkit.Uwp;
using System;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Files.Uwp.Views
{
    public sealed partial class Properties : Page
    {
        private static ApplicationViewTitleBar TitleBar;

        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private ContentDialog propertiesDialog;

        private object navParameterItem;
        private IShellPage AppInstance;

        private ListedItem listedItem;

        private Storyboard RectHoverAnim;
        private Storyboard RectUnHoverAnim;

        private Storyboard CrossHoverAnim;
        private Storyboard CrossUnHoverAnim;

        private XamlCompositionBrushBase micaBrush;

        public SettingsViewModel AppSettings => App.AppSettings;

        public AppWindow appWindow;

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
                // Set window size in the loaded event to prevent flickering
                if (WindowDecorationsHelper.IsWindowDecorationsAllowed)
                {
                    appWindow.TitleBar.SetPreferredVisibility(AppWindowTitleBarVisibility.AlwaysHidden);
                    appWindow.Frame.DragRegionVisuals.Add(TitleBarDragArea);

                    crossIcon.Foreground = ThemeHelper.RootTheme switch
                    {
                        ElementTheme.Default => new SolidColorBrush((Color)Application.Current.Resources["SystemBaseHighColor"]),
                        ElementTheme.Light => new SolidColorBrush(Colors.Black),
                        ElementTheme.Dark => new SolidColorBrush(Colors.White),
                        _ => new SolidColorBrush((Color)Application.Current.Resources["SystemBaseHighColor"])
                    };

                    var micaIsSupported = ApiInformation.IsMethodPresent("Windows.UI.Composition.Compositor", "TryCreateBlurredWallpaperBackdropBrush");
                    if (micaIsSupported)
                    {
                        micaBrush = new Brushes.MicaBrush(false);
                        (micaBrush as Brushes.MicaBrush).SetAppWindow(appWindow);
                        Frame.Background = micaBrush;
                    }
                    else
                    {
                        Microsoft.UI.Xaml.Controls.BackdropMaterial.SetApplyToRootOrPageBackground(sender as Control, true);
                    }

                    var duration = new Duration(TimeSpan.FromMilliseconds(280));

                    RectHoverAnim = new Storyboard();
                    var RectHoverColorAnim = new ColorAnimation();
                    RectHoverColorAnim.Duration = duration;
                    RectHoverColorAnim.From = Colors.Transparent;
                    RectHoverColorAnim.To = Color.FromArgb(255, 232, 17, 35);
                    RectHoverColorAnim.EasingFunction = new SineEase();
                    Storyboard.SetTarget(RectHoverColorAnim, CloseRect);
                    Storyboard.SetTargetProperty(RectHoverColorAnim, "(Rectangle.Fill).(SolidColorBrush.Color)");
                    RectHoverAnim.Children.Add(RectHoverColorAnim);

                    RectUnHoverAnim = new Storyboard();
                    var RectUnHoverColorAnim = new ColorAnimation();
                    RectUnHoverColorAnim.Duration = duration;
                    RectUnHoverColorAnim.To = Colors.Transparent;
                    RectUnHoverColorAnim.From = Color.FromArgb(255, 232, 17, 35);
                    RectUnHoverColorAnim.EasingFunction = new SineEase();
                    Storyboard.SetTarget(RectUnHoverColorAnim, CloseRect);
                    Storyboard.SetTargetProperty(RectUnHoverColorAnim, "(Rectangle.Fill).(SolidColorBrush.Color)");
                    RectUnHoverAnim.Children.Add(RectUnHoverColorAnim);

                    CrossHoverAnim = new Storyboard();
                    var CrossHoverColorAnim = new ColorAnimation();
                    CrossHoverColorAnim.Duration = duration;
                    CrossHoverColorAnim.From = ((SolidColorBrush)crossIcon.Foreground).Color;
                    CrossHoverColorAnim.To = Colors.White;
                    CrossHoverColorAnim.EasingFunction = new SineEase();
                    Storyboard.SetTarget(CrossHoverColorAnim, crossIcon);
                    Storyboard.SetTargetProperty(CrossHoverColorAnim, "(PathIcon.Foreground).(SolidColorBrush.Color)");
                    CrossHoverAnim.Children.Add(CrossHoverColorAnim);

                    CrossUnHoverAnim = new Storyboard();
                    var CrossUnHoverColorAnim = new ColorAnimation();
                    CrossUnHoverColorAnim.Duration = duration;
                    CrossUnHoverColorAnim.To = ((SolidColorBrush)crossIcon.Foreground).Color;
                    CrossUnHoverColorAnim.From = Colors.White;
                    CrossUnHoverColorAnim.EasingFunction = new SineEase();
                    Storyboard.SetTarget(CrossUnHoverColorAnim, crossIcon);
                    Storyboard.SetTargetProperty(CrossUnHoverColorAnim, "(PathIcon.Foreground).(SolidColorBrush.Color)");
                    CrossUnHoverAnim.Children.Add(CrossUnHoverColorAnim);
                }
                else
                {
                    Microsoft.UI.Xaml.Controls.BackdropMaterial.SetApplyToRootOrPageBackground(sender as Control, true);

                    TitleBar = ApplicationView.GetForCurrentView().TitleBar;
                    TitleBar.ButtonBackgroundColor = Colors.Transparent;
                    TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                    Window.Current.SetTitleBar(TitleBarDragArea);
                }
                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => AppSettings.UpdateThemeElements.Execute(null));
            }
            else
            {
                Microsoft.UI.Xaml.Controls.BackdropMaterial.SetApplyToRootOrPageBackground(sender as Control, true);
                propertiesDialog = DependencyObjectHelpers.FindParent<ContentDialog>(this);
                propertiesDialog.Closed += PropertiesDialog_Closed;
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
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RequestedTheme = selectedTheme;
                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
                {
                    switch (RequestedTheme)
                    {
                        case ElementTheme.Default:
                            if (WindowDecorationsHelper.IsWindowDecorationsAllowed)
                            {
                                crossIcon.Foreground = new SolidColorBrush((Color)Application.Current.Resources["SystemBaseHighColor"]);
                                ((ColorAnimation)CrossHoverAnim.Children[0]).From = (Color)Application.Current.Resources["SystemBaseHighColor"];
                                ((ColorAnimation)CrossUnHoverAnim.Children[0]).To = (Color)Application.Current.Resources["SystemBaseHighColor"];
                            }
                            else
                            {
                                TitleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
                                TitleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
                            }
                            break;

                        case ElementTheme.Light:
                            if (WindowDecorationsHelper.IsWindowDecorationsAllowed)
                            {
                                crossIcon.Foreground = new SolidColorBrush(Colors.Black);
                                ((ColorAnimation)CrossHoverAnim.Children[0]).From = Colors.Black;
                                ((ColorAnimation)CrossUnHoverAnim.Children[0]).To = Colors.Black;
                            }
                            else
                            {
                                TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
                                TitleBar.ButtonForegroundColor = Colors.Black;
                            }
                            break;

                        case ElementTheme.Dark:
                            if (WindowDecorationsHelper.IsWindowDecorationsAllowed)
                            {
                                crossIcon.Foreground = new SolidColorBrush(Colors.White);
                                ((ColorAnimation)CrossHoverAnim.Children[0]).From = Colors.White;
                                ((ColorAnimation)CrossUnHoverAnim.Children[0]).To = Colors.White;
                            }
                            else
                            {
                                TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
                                TitleBar.ButtonForegroundColor = Colors.White;
                            }
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
                if (WindowDecorationsHelper.IsWindowDecorationsAllowed)
                {
                    await appWindow.CloseAsync();
                }
                else
                {
                    await ApplicationView.GetForCurrentView().TryConsolidateAsync();
                }
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
                if (WindowDecorationsHelper.IsWindowDecorationsAllowed)
                {
                    await appWindow.CloseAsync();
                }
                else
                {
                    await ApplicationView.GetForCurrentView().TryConsolidateAsync();
                }
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
                    if (WindowDecorationsHelper.IsWindowDecorationsAllowed)
                    {
                        await appWindow.CloseAsync();
                    }
                    else
                    {
                        await ApplicationView.GetForCurrentView().TryConsolidateAsync();
                    }
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

        private async void CloseRect_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            CrossUnHoverAnim.Stop();
            RectUnHoverAnim.Stop();
            CrossHoverAnim.Stop();
            RectHoverAnim.Stop();

            await appWindow.CloseAsync();
        }

        private void CloseRect_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            CrossUnHoverAnim.Stop();
            RectUnHoverAnim.Stop();

            CrossHoverAnim.Begin();
            RectHoverAnim.Begin();
        }

        private void CloseRect_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            CrossHoverAnim.Stop();
            RectHoverAnim.Stop();

            CrossUnHoverAnim.Begin();
            RectUnHoverAnim.Begin();
        }
    }
}