using Files.CommandLine;
using Files.Common;
using Files.Controllers;
using Files.Controls;
using Files.Filesystem;
using Files.Helpers;
using Files.View_Models;
using Files.Views;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using Files.UserControls.MultiTaskingControl;

namespace Files
{
    sealed partial class App : Application
    {
        public static IMultitaskingControl MultitaskingControl = null;

        private static IShellPage currentInstance;
        private static bool ShowErrorNotification = false;

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

        public static SettingsViewModel AppSettings { get; set; }
        public static InteractionViewModel InteractionViewModel { get; set; }
        public static JumpListManager JumpList { get; } = new JumpListManager();
        public static SidebarPinnedController SidebarPinnedController { get; set; }
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public App()
        {
            UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedException;

            InitializeComponent();
            Suspending += OnSuspending;
            LeavingBackground += OnLeavingBackground;
            // Initialize NLog
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            NLog.LogManager.Configuration.Variables["LogPath"] = storageFolder.Path;

#if !DEBUG
            AppCenter.Start("682666d1-51d3-4e4a-93d0-d028d43baaa0", typeof(Analytics), typeof(Crashes));
#endif
        }

        private void OnLeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            // Need to reinitialize AppService when app is resuming
            InitializeAppServiceConnection();
            AppSettings?.DrivesManager?.ResumeDeviceWatcher();
        }

        public static AppServiceConnection Connection;

        private async void InitializeAppServiceConnection()
        {
            Connection = new AppServiceConnection();
            Connection.AppServiceName = "FilesInteropService";
            Connection.PackageFamilyName = Package.Current.Id.FamilyName;
            Connection.RequestReceived += Connection_RequestReceived;
            Connection.ServiceClosed += Connection_ServiceClosed;

            AppServiceConnectionStatus status = await Connection.OpenAsync();
            if (status != AppServiceConnectionStatus.Success)
            {
                // TODO: error handling
                Connection.Dispose();
                Connection = null;
            }

            // Launch fulltrust process
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        private void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            Connection = null;
        }

        private async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            // Get a deferral because we use an awaitable API below to respond to the message
            // and we don't want this call to get cancelled while we are waiting.
            var messageDeferral = args.GetDeferral();

            // The fulltrust process signaled that something in the recycle bin folder has changed
            if (args.Request.Message.ContainsKey("FileSystem"))
            {
                var folderPath = (string)args.Request.Message["FileSystem"];
                var itemPath = (string)args.Request.Message["Path"];
                var changeType = (string)args.Request.Message["Type"];
                var newItem = JsonConvert.DeserializeObject<ShellFileItem>(args.Request.Message.Get("Item", ""));
                Debug.WriteLine("{0}: {1}", folderPath, changeType);
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    // If we are currently displaying the reycle bin lets refresh the items
                    if (CurrentInstance.FilesystemViewModel?.CurrentFolder?.ItemPath == folderPath)
                    {
                        switch (changeType)
                        {
                            case "Created":
                                CurrentInstance.FilesystemViewModel.AddFileOrFolderFromShellFile(newItem);
                                break;

                            case "Deleted":
                                CurrentInstance.FilesystemViewModel.RemoveFileOrFolder(itemPath);
                                break;

                            default:
                                CurrentInstance.FilesystemViewModel.RefreshItems();
                                break;
                        }
                    }
                });
            }

            // Complete the deferral so that the platform knows that we're done responding to the app service call.
            // Note for error handling: this must be called even if SendResponseAsync() throws an exception.
            messageDeferral.Complete();
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

        public static void UnpinItem_Click(object sender, RoutedEventArgs e)
        {
            if (rightClickedItem.Path.Equals(App.AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
            {
                AppSettings.PinRecycleBinToSideBar = false;
            }
            else
            {
                SidebarPinnedController.Model.RemoveItem(rightClickedItem.Path.ToString());
            }
        }

        public static Windows.UI.Xaml.UnhandledExceptionEventArgs ExceptionInfo { get; set; }
        public static string ExceptionStackTrace { get; set; }
        public static List<string> pathsToDeleteAfterPaste = new List<string>();

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
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
                rootFrame.CacheSize = 1;
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
                    rootFrame.Navigate(typeof(MainPage), e.Arguments, new SuppressNavigationTransitionInfo());
                }
                else
                {
                    await MainPage.AddNewTab(typeof(Views.Pages.ModernShellPage), e.Arguments);
                }

                // Ensure the current window is active
                Window.Current.Activate();
                Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
                Window.Current.CoreWindow.Activated += CoreWindow_Activated;
                var currentView = SystemNavigationManager.GetForCurrentView();
                currentView.BackRequested += Window_BackRequested;
            }
        }

        private void CoreWindow_Activated(CoreWindow sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == CoreWindowActivationState.CodeActivated ||
                args.WindowActivationState == CoreWindowActivationState.PointerActivated)
            {
                ShowErrorNotification = true;
                ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = Process.GetCurrentProcess().Id;
            }
        }

        private void Window_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (App.CurrentInstance.ContentFrame.CanGoBack)
            {
                e.Handled = true;
                NavigationActions.Back_Click(null, null);
            }
            else
            {
                e.Handled = false;
            }
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            Logger.Info("App activated");

            // Window management
            if (!(Window.Current.Content is Frame rootFrame))
            {
                rootFrame = new Frame();
                rootFrame.CacheSize = 1;
                Window.Current.Content = rootFrame;
            }

            var currentView = SystemNavigationManager.GetForCurrentView();
            switch (args.Kind)
            {
                case ActivationKind.Protocol:
                    var eventArgs = args as ProtocolActivatedEventArgs;

                    if (eventArgs.Uri.AbsoluteUri == "files-uwp:")
                    {
                        rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
                    }
                    else
                    {
                        var trimmedPath = eventArgs.Uri.OriginalString.Split('=')[1];
                        rootFrame.Navigate(typeof(MainPage), @trimmedPath, new SuppressNavigationTransitionInfo());
                    }

                    // Ensure the current window is active.
                    Window.Current.Activate();
                    Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
                    Window.Current.CoreWindow.Activated += CoreWindow_Activated;
                    currentView.BackRequested += Window_BackRequested;
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
                                    rootFrame.Navigate(typeof(MainPage), command.Payload, new SuppressNavigationTransitionInfo());

                                    // Ensure the current window is active.
                                    Window.Current.Activate();
                                    Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
                                    Window.Current.CoreWindow.Activated += CoreWindow_Activated;
                                    currentView.BackRequested += Window_BackRequested;
                                    return;

                                case ParsedCommandType.OpenPath:

                                    try
                                    {
                                        var det = await StorageFolder.GetFolderFromPathAsync(command.Payload);

                                        rootFrame.Navigate(typeof(MainPage), command.Payload, new SuppressNavigationTransitionInfo());

                                        // Ensure the current window is active.
                                        Window.Current.Activate();
                                        Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
                                        Window.Current.CoreWindow.Activated += CoreWindow_Activated;
                                        currentView.BackRequested += Window_BackRequested;

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
                                    rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
                                    // Ensure the current window is active.
                                    Window.Current.Activate();
                                    Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
                                    Window.Current.CoreWindow.Activated += CoreWindow_Activated;
                                    currentView.BackRequested += Window_BackRequested;
                                    return;
                            }
                        }
                    }
                    break;

                case ActivationKind.ToastNotification:
                    var eventArgsForNotification = args as ToastNotificationActivatedEventArgs;
                    if (eventArgsForNotification.Argument == "report")
                    {
                        // Launch the URI and open log files location
                        //SettingsViewModel.OpenLogLocation();
                        SettingsViewModel.ReportIssueOnGitHub();
                    }
                    break;
            }

            rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());

            // Ensure the current window is active.
            Window.Current.Activate();
            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
            Window.Current.CoreWindow.Activated += CoreWindow_Activated;
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
            SaveSessionTabs();

            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            if (Connection != null)
            {
                Connection.Dispose();
                Connection = null;
            }
            AppSettings?.Dispose();
            deferral.Complete();
        }

        public static void SaveSessionTabs() // Enumerates through all tabs and gets the Path property and saves it to AppSettings.LastSessionPages
        {
            AppSettings.LastSessionPages = MainPage.AppInstances.DefaultIfEmpty().Select(tab => tab != null ? tab.Path ?? ResourceController.GetTranslation("NewTab") : ResourceController.GetTranslation("NewTab")).ToArray();
        }

        // Occurs when an exception is not handled on the UI thread.
        private static void OnUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e) => AppUnhandledException(e.Exception);

        // Occurs when an exception is not handled on a background thread.
        // ie. A task is fired and forgotten Task.Run(() => {...})
        private static void OnUnobservedException(object sender, UnobservedTaskExceptionEventArgs e) => AppUnhandledException(e.Exception);

        private static void AppUnhandledException(Exception ex)
        {
            Logger.Error(ex, ex.Message);
            if (ShowErrorNotification)
            {
                var toastContent = new ToastContent()
                {
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = ResourceController.GetTranslation("ExceptionNotificationHeader")
                                },
                                new AdaptiveText()
                                {
                                    Text = ResourceController.GetTranslation("ExceptionNotificationBody")
                                }
                            },
                            AppLogoOverride = new ToastGenericAppLogo()
                            {
                                Source = "ms-appx:///Assets/error.png"
                            }
                        }
                    },
                    Actions = new ToastActionsCustom()
                    {
                        Buttons =
                        {
                            new ToastButton(ResourceController.GetTranslation("ExceptionNotificationReportButton"), "report")
                            {
                                ActivationType = ToastActivationType.Foreground
                            }
                        }
                    }
                };

                // Create the toast notification
                var toastNotif = new ToastNotification(toastContent.GetXml());

                // And send the notification
                ToastNotificationManager.CreateToastNotifier().Show(toastNotif);
            }
        }

        public static async void CloseApp()
        {
            if (!await ApplicationView.GetForCurrentView().TryConsolidateAsync()) Application.Current.Exit();
        }
    }

    public class WSLDistroItem : INavigationControlItem
    {
        public string Glyph { get; set; } = null;
        public string Text { get; set; }
        public string Path { get; set; }
        public NavigationControlItemType ItemType => NavigationControlItemType.LinuxDistro;
        public Uri Logo { get; set; }
    }
}