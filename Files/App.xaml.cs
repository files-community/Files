using Files.CommandLine;
using Files.Common;
using Files.Controllers;
using Files.Filesystem;
using Files.Filesystem.FilesystemHistory;
using Files.Helpers;
using Files.SettingsInterfaces;
using Files.UserControls.MultitaskingControl;
using Files.ViewModels;
using Files.Views;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    sealed partial class App : Application
    {
        private static bool ShowErrorNotification = false;

        public static SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
        public static StorageHistoryWrapper HistoryWrapper = new StorageHistoryWrapper();
        public static IBundlesSettings BundlesSettings = new BundlesSettingsViewModel();
        public static SettingsViewModel AppSettings { get; private set; }
        public static InteractionViewModel InteractionViewModel { get; private set; }
        public static JumpListManager JumpList { get; } = new JumpListManager();
        public static SidebarPinnedController SidebarPinnedController { get; private set; }
        public static CloudDrivesManager CloudDrivesManager { get; private set; }
        public static NetworkDrivesManager NetworkDrivesManager { get; private set; }
        public static DrivesManager DrivesManager { get; private set; }
        public static WSLDistroManager WSLDistroManager { get; private set; }
        public static LibraryManager LibraryManager { get; private set; }
        public static ExternalResourcesHelper ExternalResourcesHelper { get; private set; }
        public static OptionalPackageManager OptionalPackageManager { get; private set; } = new OptionalPackageManager();

        public static Logger Logger { get; private set; }
        private static readonly UniversalLogWriter logWriter = new UniversalLogWriter();

        public static StatusCenterViewModel StatusCenterViewModel { get; } = new StatusCenterViewModel();

        public static SecondaryTileHelper SecondaryTileHelper { get; private set; } = new SecondaryTileHelper();

        public static class AppData
        {
            // Get the extensions that are available for this host.
            // Extensions that declare the same contract string as the host will be recognized.
            internal static ExtensionManager FilePreviewExtensionManager { get; set; } = new ExtensionManager("com.files.filepreview");
        }

        public App()
        {
            // Initialize logger
            Logger = new Logger(logWriter);

            UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedException;
            InitializeComponent();
            Suspending += OnSuspending;
            LeavingBackground += OnLeavingBackground;

            //LogManager.Configuration.Variables["LogPath"] = storageFolder.Path;
            AppData.FilePreviewExtensionManager.Initialize(); // The extension manager can update UI, so pass it the UI dispatcher to use for UI updates

            StartAppCenter();
        }

        private static async Task EnsureSettingsAndConfigurationAreBootstrapped()
        {
            if (AppSettings == null)
            {
                AppSettings = await SettingsViewModel.CreateInstance();
            }

            ExternalResourcesHelper ??= new ExternalResourcesHelper();
            await ExternalResourcesHelper.LoadSelectedTheme();

            InteractionViewModel ??= new InteractionViewModel();
            SidebarPinnedController ??= await SidebarPinnedController.CreateInstance();
            LibraryManager ??= new LibraryManager();
            DrivesManager ??= new DrivesManager();
            NetworkDrivesManager ??= new NetworkDrivesManager();
            CloudDrivesManager ??= new CloudDrivesManager();
            WSLDistroManager ??= new WSLDistroManager();

            // Start off a list of tasks we need to run before we can continue startup
            _ = Task.Factory.StartNew(async () =>
            {
                await LibraryManager.EnumerateLibrariesAsync();
                await DrivesManager.EnumerateDrivesAsync();
                await CloudDrivesManager.EnumerateDrivesAsync();
                await NetworkDrivesManager.EnumerateDrivesAsync();
                await WSLDistroManager.EnumerateDrivesAsync();
            });
        }

        private async void StartAppCenter()
        {
            JObject obj;
            try
            {
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(@"ms-appx:///Resources/AppCenterKey.txt"));
                var lines = await FileIO.ReadTextAsync(file);
                obj = JObject.Parse(lines);
            }
            catch
            {
                return;
            }

            AppCenter.Start((string)obj.SelectToken("key"), typeof(Analytics), typeof(Crashes));
        }

        private void OnLeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            DrivesManager?.ResumeDeviceWatcher();
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
            await logWriter.InitializeAsync("debug.log");
            //start tracking app usage
            SystemInformation.Instance.TrackAppUse(e);

            Logger.Info("App launched");

            bool canEnablePrelaunch = ApiInformation.IsMethodPresent("Windows.ApplicationModel.Core.CoreApplication", "EnablePrelaunch");

            await EnsureSettingsAndConfigurationAreBootstrapped();

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
                    if (!(string.IsNullOrEmpty(e.Arguments) && MainPageViewModel.AppInstances.Count > 0))
                    {
                        await MainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), e.Arguments);
                    }
                }

                // Ensure the current window is active
                Window.Current.Activate();
                Window.Current.CoreWindow.Activated += CoreWindow_Activated;
            }
        }

        private void CoreWindow_Activated(CoreWindow sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == CoreWindowActivationState.CodeActivated ||
                args.WindowActivationState == CoreWindowActivationState.PointerActivated)
            {
                ShowErrorNotification = true;
                ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = Process.GetCurrentProcess().Id;
                if (App.InteractionViewModel != null)
                {
                    App.InteractionViewModel.Clipboard_ContentChanged(null, null);
                }
            }
        }


        protected override async void OnActivated(IActivatedEventArgs args)
        {
            Logger.Info("App activated");

            await EnsureSettingsAndConfigurationAreBootstrapped();

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
                        var parsedArgs = eventArgs.Uri.Query.TrimStart('?').Split('=');
                        var unescapedValue = Uri.UnescapeDataString(parsedArgs[1]);
                        switch (parsedArgs[0])
                        {
                            case "tab":
                                rootFrame.Navigate(typeof(MainPage), TabItemArguments.Deserialize(unescapedValue), new SuppressNavigationTransitionInfo());
                                break;

                            case "folder":
                                rootFrame.Navigate(typeof(MainPage), unescapedValue, new SuppressNavigationTransitionInfo());
                                break;
                        }
                    }

                    // Ensure the current window is active.
                    Window.Current.Activate();
                    Window.Current.CoreWindow.Activated += CoreWindow_Activated;
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
                                    Window.Current.CoreWindow.Activated += CoreWindow_Activated;
                                    return;

                                case ParsedCommandType.OpenPath:

                                    try
                                    {
                                        var det = await StorageFolder.GetFolderFromPathAsync(command.Payload);

                                        rootFrame.Navigate(typeof(MainPage), command.Payload, new SuppressNavigationTransitionInfo());

                                        // Ensure the current window is active.
                                        Window.Current.Activate();
                                        Window.Current.CoreWindow.Activated += CoreWindow_Activated;

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
                                    if (command.Payload.Equals("."))
                                    {
                                        rootFrame.Navigate(typeof(MainPage), activationPath, new SuppressNavigationTransitionInfo());
                                    }
                                    else
                                    {
                                        var target = Path.GetFullPath(Path.Combine(activationPath, command.Payload));
                                        if (!string.IsNullOrEmpty(command.Payload))
                                        {
                                            rootFrame.Navigate(typeof(MainPage), target, new SuppressNavigationTransitionInfo());
                                        }
                                        else
                                        {
                                            rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
                                        }
                                    }

                                    // Ensure the current window is active.
                                    Window.Current.Activate();
                                    Window.Current.CoreWindow.Activated += CoreWindow_Activated;

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
            Window.Current.CoreWindow.Activated += CoreWindow_Activated;
        }

        private void TryEnablePrelaunch()
        {
            CoreApplication.EnablePrelaunch(true);
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

            LibraryManager?.Dispose();
            DrivesManager?.Dispose();
            deferral.Complete();

#if DEBUG
            Current.Exit();
#endif
        }

        public static void SaveSessionTabs() // Enumerates through all tabs and gets the Path property and saves it to AppSettings.LastSessionPages
        {
            if (AppSettings != null)
            {
                AppSettings.LastSessionPages = MainPageViewModel.AppInstances.DefaultIfEmpty().Select(tab =>
                {
                    if (tab != null && tab.TabItemArguments != null)
                    {
                        return tab.TabItemArguments.Serialize();
                    }
                    else
                    {
                        var defaultArg = new TabItemArguments() { InitialPageType = typeof(PaneHolderPage), NavigationArg = "NewTab".GetLocalized() };
                        return defaultArg.Serialize();
                    }
                }).ToArray();
            }
        }

        // Occurs when an exception is not handled on the UI thread.
        private static void OnUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e) => AppUnhandledException(e.Exception);

        // Occurs when an exception is not handled on a background thread.
        // ie. A task is fired and forgotten Task.Run(() => {...})
        private static void OnUnobservedException(object sender, UnobservedTaskExceptionEventArgs e) => AppUnhandledException(e.Exception);

        private static void AppUnhandledException(Exception ex)
        {
            string formattedException = string.Empty;

            formattedException += "--------- UNHANDLED EXCEPTION ---------";
            if (ex != null)
            {
                formattedException += $"\n>>>> HRESULT: {ex.HResult}\n";
                if (ex.Message != null)
                {
                    formattedException += "\n--- MESSAGE ---";
                    formattedException += ex.Message;
                }
                if (ex.StackTrace != null)
                {
                    formattedException += "\n--- STACKTRACE ---";
                    formattedException += ex.StackTrace;
                }
                if (ex.Source != null)
                {
                    formattedException += "\n--- SOURCE ---";
                    formattedException += ex.Source;
                }
                if (ex.InnerException != null)
                {
                    formattedException += "\n--- INNER ---";
                    formattedException += ex.InnerException;
                }
            }
            else
            {
                formattedException += "\nException is null!\n";
            }

            formattedException += "---------------------------------------";

            Debug.WriteLine(formattedException);

            Debugger.Break(); // Please check "Output Window" for exception details (View -> Output Window) (CTRL + ALT + O)

            SaveSessionTabs();
            Logger.Error(ex, formattedException);
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
                                    Text = "ExceptionNotificationHeader".GetLocalized()
                                },
                                new AdaptiveText()
                                {
                                    Text = "ExceptionNotificationBody".GetLocalized()
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
                            new ToastButton("ExceptionNotificationReportButton".GetLocalized(), "report")
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
            if (!await ApplicationView.GetForCurrentView().TryConsolidateAsync())
            {
                Application.Current.Exit();
            }
        }
    }
}