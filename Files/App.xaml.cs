using Files.CommandLine;
using Files.Controls;
using Files.Filesystem;
using Files.Helpers;
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
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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

        public static Dialogs.ExceptionDialog ExceptionDialogDisplay { get; set; }
        public static Dialogs.ConsentDialog ConsentDialogDisplay { get; set; }
        public static Dialogs.PropertiesDialog PropertiesDialogDisplay { get; set; }
        public static Dialogs.LayoutDialog LayoutDialogDisplay { get; set; }
        public static Dialogs.AddItemDialog AddItemDialogDisplay { get; set; }
        public static ObservableCollection<INavigationControlItem> sideBarItems = new ObservableCollection<INavigationControlItem>();
        public static ObservableCollection<LocationItem> locationItems = new ObservableCollection<LocationItem>();
        public static ObservableCollection<WSLDistroItem> linuxDistroItems = new ObservableCollection<WSLDistroItem>();
        public static SettingsViewModel AppSettings { get; set; }
        public static InteractionViewModel InteractionViewModel { get; set; }
        public static SelectedItemPropertiesViewModel SelectedItemPropertiesViewModel { get; set; }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;

            // Initialize NLog
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            NLog.LogManager.Configuration.Variables["LogPath"] = storageFolder.Path;

            RegisterUncaughtExceptionLogger();

            ConsentDialogDisplay = new Dialogs.ConsentDialog();
            PropertiesDialogDisplay = new Dialogs.PropertiesDialog();
            LayoutDialogDisplay = new Dialogs.LayoutDialog();
            AddItemDialogDisplay = new Dialogs.AddItemDialog();
            ExceptionDialogDisplay = new Dialogs.ExceptionDialog();
            // this.UnhandledException += App_UnhandledException;
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            Clipboard_ContentChanged(null, null);
            AppCenter.Start("682666d1-51d3-4e4a-93d0-d028d43baaa0", typeof(Analytics), typeof(Crashes));

            AppSettings = new SettingsViewModel();
            InteractionViewModel = new InteractionViewModel();
            SelectedItemPropertiesViewModel = new SelectedItemPropertiesViewModel();
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
            else if (args.CurrentPoint.Properties.IsXButton2Pressed)
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
                        App.PS.IsEnabled = true;
                    }
                    else
                    {
                        App.PS.IsEnabled = false;
                    }
                }
                else
                {
                    App.PS.IsEnabled = false;
                }
            }
            catch (Exception)
            {
                App.PS.IsEnabled = false;
            }
        }

        public static Windows.UI.Xaml.UnhandledExceptionEventArgs ExceptionInfo { get; set; }
        public static string ExceptionStackTrace { get; set; }
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

            Logger.Info("App launched");

            bool canEnablePrelaunch = Windows.Foundation.Metadata.ApiInformation.IsMethodPresent("Windows.ApplicationModel.Core.CoreApplication", "EnablePrelaunch");

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (!(Window.Current.Content is Frame rootFrame))
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

                ThemeHelper.Initialize();

                // Ensure the current window is active
                Window.Current.Activate();
                Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
            }
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            Logger.Info("App activated");

            // Window management
            if (!(Window.Current.Content is Frame rootFrame))
            {
                rootFrame = new Frame();
                Window.Current.Content = rootFrame;
            }

            ThemeHelper.Initialize();

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
                                    rootFrame.Navigate(typeof(InstanceTabsView), command.Payload, new SuppressNavigationTransitionInfo());

                                    // Ensure the current window is active.
                                    Window.Current.Activate();
                                    Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
                                    return;

                                case ParsedCommandType.OpenPath:

                                    try
                                    {
                                        var det = await StorageFolder.GetFolderFromPathAsync(command.Payload);

                                        rootFrame.Navigate(typeof(InstanceTabsView), command.Payload, new SuppressNavigationTransitionInfo());

                                        // Ensure the current window is active.
                                        Window.Current.Activate();
                                        Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;

                                        return;
                                    }
                                    catch (System.IO.FileNotFoundException ex)
                                    {
                                        //Not a folder
                                        Debug.WriteLine($"File not found exception App.xaml.cs\\OnActivated with message: {ex.Message}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Exception in App.xaml.cs\\OnActivated with message: {ex.Message}");
                                    }

                                    break;

                                case ParsedCommandType.Unknown:
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
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
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