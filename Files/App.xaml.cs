using Files.CommandLine;
using Files.Controls;
using Files.Filesystem;
using Files.Interacts;
using Files.View_Models;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Uwp.Helpers;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    sealed partial class App : Application
    {

        private static IShellPage currentInstance;
        public static IShellPage CurrentInstance
        {
            get
            {
                return currentInstance;
            }
            set
            {
                if (value != currentInstance && value != null)
                {
                    currentInstance = value;
                }
            }
        }
        public static Dialogs.ExceptionDialog exceptionDialog { get; set; }
        public static Dialogs.ConsentDialog consentDialog { get; set; }
        public static Dialogs.PropertiesDialog propertiesDialog { get; set; }
        public static Dialogs.LayoutDialog layoutDialog { get; set; }
        public static Dialogs.AddItemDialog addItemDialog { get; set; }
        public static ObservableCollection<INavigationControlItem> sideBarItems = new ObservableCollection<INavigationControlItem>();
        public static ObservableCollection<LocationItem> locationItems = new ObservableCollection<LocationItem>();
        public static ObservableCollection<WSLDistroItem> linuxDistroItems = new ObservableCollection<WSLDistroItem>();
        public static SettingsViewModel AppSettings { get; set; }
        public static InteractionViewModel InteractionViewModel { get; set; }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            // Initialize NLog
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            NLog.LogManager.Configuration.Variables["LogPath"] = storageFolder.Path;

            RegisterUncaughtExceptionLogger();

            consentDialog = new Dialogs.ConsentDialog();
            propertiesDialog = new Dialogs.PropertiesDialog();
            layoutDialog = new Dialogs.LayoutDialog();
            addItemDialog = new Dialogs.AddItemDialog();
            exceptionDialog = new Dialogs.ExceptionDialog();
            // this.UnhandledException += App_UnhandledException;
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            Clipboard_ContentChanged(null, null);
            AppCenter.Start("682666d1-51d3-4e4a-93d0-d028d43baaa0", typeof(Analytics), typeof(Crashes));

            AppSettings = new SettingsViewModel();
            InteractionViewModel = new InteractionViewModel();
        }

        private void RegisterUncaughtExceptionLogger()
        {
            UnhandledException += (sender, args) =>
            {
                Logger.Error(args.Exception, args.Message);
            };
        }

        private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            if (args.CurrentPoint.Properties.IsXButton1Pressed)
            {
                NavigationActions.Back_Click(null, null);
            }
            else if (args.CurrentPoint.Properties.IsXButton1Pressed)
            {
                NavigationActions.Forward_Click(null, null);
            }
        }

        public static INavigationControlItem rightClickedItem;

        public static async void FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            var ListFile = await cacheFolder.GetFileAsync("PinnedItems.txt");
            var ListFileLines = await FileIO.ReadLinesAsync(ListFile);
            foreach (string path in ListFileLines)
            {
                if (path == App.rightClickedItem.Path.ToString())
                {
                    App.AppSettings.LinesToRemoveFromFile.Add(path);
                    App.AppSettings.RemoveStaleSidebarItems();
                    return;
                }
            }
        }

        public static void Clipboard_ContentChanged(object sender, object e)
        {
            try
            {
                if (App.CurrentInstance != null)
                {
                    DataPackageView packageView = Clipboard.GetContent();
                    if (packageView.Contains(StandardDataFormats.StorageItems) && App.CurrentInstance.CurrentPageType != typeof(YourHome))
                    {
                        App.PS.isEnabled = true;
                    }
                    else
                    {
                        App.PS.isEnabled = false;
                    }
                }
                else
                {
                    App.PS.isEnabled = false;
                }
            }
            catch (Exception)
            {
                App.PS.isEnabled = false;
            }

        }

        public static Windows.UI.Xaml.UnhandledExceptionEventArgs exceptionInfo { get; set; }
        public static string exceptionStackTrace { get; set; }

        private async void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            exceptionInfo = e;
            exceptionStackTrace = e.Exception.StackTrace;
            await exceptionDialog.ShowAsync(ContentDialogPlacement.Popup);
        }

        public static IReadOnlyList<ContentDialog> FindDisplayedContentDialogs<T>()
        {
            var popupElements = VisualTreeHelper.GetOpenPopupsForXamlRoot(Window.Current.Content.XamlRoot);
            List<ContentDialog> dialogs = new List<ContentDialog>();
            List<ContentDialog> openDialogs = new List<ContentDialog>();
            Interaction.FindChildren<ContentDialog>(dialogs, Window.Current.Content.XamlRoot.Content as DependencyObject);
            foreach (var dialog in dialogs)
            {
                var popups = new List<Popup>();
                Interaction.FindChildren<Popup>(popups, dialog);
                if (popups.First().IsOpen && popups.First() is T)
                {
                    openDialogs.Add(dialog);
                }
            }
            return openDialogs;
        }

        public static PasteState PS { get; set; } = new PasteState();
        public static List<string> pathsToDeleteAfterPaste = new List<string>();

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            //start tracking app usage
            SystemInformation.TrackAppUse(e);

            if (SystemInformation.IsAppUpdated)
            {
                var dialog = new ContentDialog()
                {
                    Title = "What's new in v0.7.3",
                    Content = "• We are starting to test a brand new design, this is still in the early stages so make sure to send us any feedback on GitHub. \n• We fixed an issue where a swipe gesture was having unexpected side effects. \n• We started work on layout modes, it is not fully functional yet and we will improve it in future updates.",
                    PrimaryButtonText = "Lets go!"
                };

                dialog.ShowAsync();
            }

            Logger.Info("App launched");

            bool canEnablePrelaunch = Windows.Foundation.Metadata.ApiInformation.IsMethodPresent("Windows.ApplicationModel.Core.CoreApplication", "EnablePrelaunch");

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (canEnablePrelaunch)
                {
                    TryEnablePrelaunch();
                }

                if (rootFrame.Content == null)
                {

                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(InstanceTabsView), e.Arguments, new SuppressNavigationTransitionInfo());


                }

                // Ensure the current window is active
                Window.Current.Activate();
                Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
            }
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            Logger.Info("App activated");

            // Window management
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                Window.Current.Content = rootFrame;
            }

            switch (args.Kind)
            {
                case ActivationKind.Protocol:
                    var eventArgs = args as ProtocolActivatedEventArgs;

                    if (eventArgs.Uri.AbsoluteUri == "files-uwp:")
                    {
                        rootFrame.Navigate(typeof(InstanceTabsView), null, new SuppressNavigationTransitionInfo());
                    }
                    else
                    {
                        var trimmedPath = eventArgs.Uri.OriginalString.Split('=')[1];
                        rootFrame.Navigate(typeof(InstanceTabsView), @trimmedPath, new SuppressNavigationTransitionInfo());
                    }
                    // Ensure the current window is active.
                    Window.Current.Activate();
                    Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
                    return;

                case ActivationKind.CommandLineLaunch:
                    var cmdLineArgs = args as CommandLineActivatedEventArgs;
                    var operation = cmdLineArgs.Operation;
                    var cmdLineString = operation.Arguments;
                    var activationPath = operation.CurrentDirectoryPath;

                    var parsedCommands = CommandLineParser.ParseUntrustedCommands(cmdLineString);

                    if (parsedCommands != null && parsedCommands.Count > 0)
                    {
                        foreach (var command in parsedCommands)
                        {
                            switch (command.Type)
                            {
                                case ParsedCommandType.OpenDirectory:
                                    // TODO Open Directory

                                    rootFrame.Navigate(typeof(InstanceTabsView), command.Payload, new SuppressNavigationTransitionInfo());

                                    // Ensure the current window is active.
                                    Window.Current.Activate();
                                    Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
                                    return;
                                case ParsedCommandType.Unkwon:
                                    rootFrame.Navigate(typeof(InstanceTabsView), null, new SuppressNavigationTransitionInfo());
                                    // Ensure the current window is active.
                                    Window.Current.Activate();
                                    Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
                                    return;
                            }
                        }
                    }
                    break;
            }

            rootFrame.Navigate(typeof(InstanceTabsView), null, new SuppressNavigationTransitionInfo());

            // Ensure the current window is active.
            Window.Current.Activate();
            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
        }

        private void TryEnablePrelaunch()
        {
            Windows.ApplicationModel.Core.CoreApplication.EnablePrelaunch(true);
        }
        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            AppSettings.Dispose();
            deferral.Complete();
        }
    }

    public class WSLDistroItem : INavigationControlItem
    {
        public string DistroName { get; set; }
        public string Path { get; set; }
        public Uri Logo { get; set; }

        string INavigationControlItem.IconGlyph => null;

        string INavigationControlItem.Text => DistroName;

        string INavigationControlItem.Path => Path;

        NavigationControlItemType INavigationControlItem.ItemType => NavigationControlItemType.LinuxDistro;
    }
}
