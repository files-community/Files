using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using Windows.ApplicationModel;
using Files.Uwp.Filesystem.FilesystemHistory;
using Files.Uwp.ViewModels;
using Files.Uwp.Helpers;
using Files.Uwp.Controllers;
using Files.Uwp.Filesystem;
using Files.Shared;
using Files.Uwp.Filesystem.Cloud;
using Microsoft.UI.Dispatching;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Uwp.ServicesImplementation.Settings;
using Files.Backend.Services;
using Files.Uwp.ServicesImplementation;
using Files.Uwp.ServicesImplementation.DateTimeFormatter;
using Files.Shared.Services.DateTimeFormatter;
using Files.Shared.Cloud;
using Files.Backend.Services.SizeProvider;
using Files.Uwp.ViewModels.SettingsViewModels;
using Microsoft.AppCenter;
using Windows.Storage;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.UI.Windowing;
using CommunityToolkit.WinUI.Helpers;
using System.Diagnostics;
using CommunityToolkit.WinUI.Notifications;
using Files.Uwp.Extensions;
using Windows.UI.Notifications;
using System.Linq;
using Files.Uwp.UserControls.MultitaskingControl;
using Files.Uwp.Views;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Files.Uwp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private static bool ShowErrorNotification = false;
        private static string OutputPath = null;

        public static StorageHistoryWrapper HistoryWrapper = new StorageHistoryWrapper();
        public static SettingsViewModel AppSettings { get; private set; }
        public static MainViewModel MainViewModel { get; private set; }
        public static PaneViewModel PaneViewModel { get; private set; }
        public static PreviewPaneViewModel PreviewPaneViewModel { get; private set; }
        public static JumpListManager JumpList { get; private set; }
        public static RecentItemsManager RecentItemsManager { get; private set; }
        public static SidebarPinnedController SidebarPinnedController { get; private set; }
        public static TerminalController TerminalController { get; private set; }
        public static CloudDrivesManager CloudDrivesManager { get; private set; }
        public static NetworkDrivesManager NetworkDrivesManager { get; private set; }
        public static DrivesManager DrivesManager { get; private set; }
        public static WSLDistroManager WSLDistroManager { get; private set; }
        public static LibraryManager LibraryManager { get; private set; }
        public static FileTagsManager FileTagsManager { get; private set; }
        public static ExternalResourcesHelper ExternalResourcesHelper { get; private set; }

        public static ILogger Logger { get; private set; }
        private static readonly UniversalLogWriter logWriter = new UniversalLogWriter();

        public static OngoingTasksViewModel OngoingTasksViewModel { get; } = new OngoingTasksViewModel();
        public static SecondaryTileHelper SecondaryTileHelper { get; private set; } = new SecondaryTileHelper();

        public static string AppVersion = $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}";

        public IServiceProvider Services { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            // Initialize logger
            Logger = new Logger(logWriter);

            UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedException;
            InitializeComponent();
            //Suspending += OnSuspending; //WINUI3
            //LeavingBackground += OnLeavingBackground; //WINUI3

            AppServiceConnectionHelper.Register();

            this.Services = ConfigureServices();
            Ioc.Default.ConfigureServices(Services);
        }

        private IServiceProvider ConfigureServices()
        {
            ServiceCollection services = new ServiceCollection();

            services
                // TODO: Loggers:

                // Settings:
                // Base IUserSettingsService as parent settings store (to get ISettingsSharingContext from)
                .AddSingleton<IUserSettingsService, UserSettingsService>()
                // Children settings (from IUserSettingsService)
                .AddSingleton<IMultitaskingSettingsService, MultitaskingSettingsService>((sp) => new MultitaskingSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
                .AddSingleton<IWidgetsSettingsService, WidgetsSettingsService>((sp) => new WidgetsSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
                .AddSingleton<IAppearanceSettingsService, AppearanceSettingsService>((sp) => new AppearanceSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
                .AddSingleton<IPreferencesSettingsService, PreferencesSettingsService>((sp) => new PreferencesSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
                .AddSingleton<IApplicationSettingsService, ApplicationSettingsService>((sp) => new ApplicationSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
                .AddSingleton<IPaneSettingsService, PaneSettingsService>((sp) => new PaneSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
                .AddSingleton<ILayoutSettingsService, LayoutSettingsService>((sp) => new LayoutSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
                // Settings not related to IUserSettingsService:
                .AddSingleton<IFileTagsSettingsService, FileTagsSettingsService>()
                .AddSingleton<IBundlesSettingsService, BundlesSettingsService>()

                // Other services
                .AddSingleton(Logger)
                .AddSingleton<IDialogService, DialogService>()
                .AddSingleton<IImagingService, ImagingService>()
                .AddSingleton<IThreadingService, ThreadingService>()
                .AddSingleton<ILocalizationService, LocalizationService>()
                .AddSingleton<ICloudDetector, CloudDetector>()
#if SIDELOAD
                .AddSingleton<IUpdateService, SideloadUpdateService>()
#else
                .AddSingleton<IUpdateService, UpdateService>()
#endif
                .AddSingleton<IDateTimeFormatterFactory, DateTimeFormatterFactory>()
                .AddSingleton<IDateTimeFormatter, UserDateTimeFormatter>()
                .AddSingleton<IVolumeInfoFactory, VolumeInfoFactory>()

                // TODO(i): FileSystem operations:
                // (IFilesystemHelpersService, IFilesystemOperationsService)
                // (IStorageEnumerator, IFallbackStorageEnumerator)
                .AddSingleton<ISizeProvider, UserSizeProvider>()

                ; // End of service configuration


            return services.BuildServiceProvider();
        }

        private static async Task EnsureSettingsAndConfigurationAreBootstrapped()
        {
            AppSettings ??= new SettingsViewModel();

            ExternalResourcesHelper ??= new ExternalResourcesHelper();
            //await ExternalResourcesHelper.LoadSelectedTheme(); //WINUI3
            new AppearanceViewModel().SetCompactStyles(updateTheme: false);

            JumpList ??= new JumpListManager();
            RecentItemsManager ??= new RecentItemsManager();
            MainViewModel ??= new MainViewModel();
            PaneViewModel ??= new PaneViewModel();
            PreviewPaneViewModel ??= new PreviewPaneViewModel();
            LibraryManager ??= new LibraryManager();
            DrivesManager ??= new DrivesManager();
            NetworkDrivesManager ??= new NetworkDrivesManager();
            CloudDrivesManager ??= new CloudDrivesManager();
            WSLDistroManager ??= new WSLDistroManager();
            FileTagsManager ??= new FileTagsManager();
            SidebarPinnedController ??= new SidebarPinnedController();
            TerminalController ??= new TerminalController();
        }

        private static async Task StartAppCenter()
        {
            try
            {
                if (!AppCenter.Configured)
                {
                    var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(@"ms-appx:///Resources/AppCenterKey.txt"));
                    var lines = await FileIO.ReadTextAsync(file);
                    var obj = Newtonsoft.Json.Linq.JObject.Parse(lines);
                    AppCenter.Start((string)obj.SelectToken("key"), typeof(Analytics), typeof(Crashes));
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "AppCenter could not be started.");
            }
        }

        private static async Task InitializeAppComponentsAsync()
        {
            var userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
            var appearanceSettingsService = userSettingsService.AppearanceSettingsService;

            // Start off a list of tasks we need to run before we can continue startup
            await Task.Run(async () =>
            {
                await Task.WhenAll(
                    StartAppCenter(),
                    DrivesManager.UpdateDrivesAsync(),
                    OptionalTask(CloudDrivesManager.UpdateDrivesAsync(), appearanceSettingsService.ShowCloudDrivesSection),
                    LibraryManager.UpdateLibrariesAsync(),
                    OptionalTask(NetworkDrivesManager.UpdateDrivesAsync(), appearanceSettingsService.ShowNetworkDrivesSection),
                    OptionalTask(WSLDistroManager.UpdateDrivesAsync(), appearanceSettingsService.ShowWslSection),
                    OptionalTask(FileTagsManager.UpdateFileTagsAsync(), appearanceSettingsService.ShowFileTagsSection),
                    SidebarPinnedController.InitializeAsync()
                );
                await Task.WhenAll(
                    AppSettings.DetectQuickLook(),
                    TerminalController.InitializeAsync(),
                    JumpList.InitializeAsync(),
                    //ExternalResourcesHelper.LoadOtherThemesAsync(), //WINUI3
                    ContextFlyoutItemHelper.CachedNewContextMenuEntries
                );

                userSettingsService.ReportToAppCenter();
            });

            // Check for required updates
            var updateService = Ioc.Default.GetRequiredService<IUpdateService>();
            await updateService.CheckForUpdates();
            await updateService.DownloadMandatoryUpdates();

            static async Task OptionalTask(Task task, bool condition)
            {
                if (condition)
                {
                    await task;
                }
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            var activatedEventArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();

            await logWriter.InitializeAsync("debug.log");
            Logger.Info($"App launched.");

            //start tracking app usage
            if (activatedEventArgs.Data is Windows.ApplicationModel.Activation.IActivatedEventArgs iaea)
            {
                SystemInformation.Instance.TrackAppUse(iaea);
            }

            // Initialize MainWindow here
            EnsureWindowIsInitialized();

            await EnsureSettingsAndConfigurationAreBootstrapped();
            _ = InitializeAppComponentsAsync().ContinueWith(t => Logger.Warn(t.Exception, "Error during InitializeAppComponentsAsync()"), TaskContinuationOptions.OnlyOnFaulted);

            await Window.InitializeApplication(activatedEventArgs);

            //WindowDecorationsHelper.RequestWindowDecorationsAccess();
        }

        private void EnsureWindowIsInitialized()
        {
            Window = new MainWindow();
            Window.Activated += Window_Activated;
            Window.Activate();
            var appWindow = GetAppWindow(Window);
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                appWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
            }
            else
            {
                Window.ExtendsContentIntoTitleBar = true;
            }
            WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(Window);
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.CodeActivated ||
                args.WindowActivationState == WindowActivationState.PointerActivated)
            {
                ShowErrorNotification = true;
                ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = Process.GetCurrentProcess().Id;
                if (MainViewModel != null)
                {
                    MainViewModel.Clipboard_ContentChanged(null, null);
                }
            }
        }

        public async void OnActivated(AppActivationArguments activatedEventArgs)
        {
            await Window.InitializeApplication(activatedEventArgs);
        }

        // WINUI3: OnSuspending

        public static void SaveSessionTabs() // Enumerates through all tabs and gets the Path property and saves it to AppSettings.LastSessionPages
        {
            IUserSettingsService userSettingsService = Ioc.Default.GetService<IUserSettingsService>();
            IBundlesSettingsService bundlesSettingsService = Ioc.Default.GetService<IBundlesSettingsService>();

            if (bundlesSettingsService != null)
            {
                bundlesSettingsService.FlushSettings();
            }
            if (userSettingsService?.PreferencesSettingsService != null)
            {
                userSettingsService.PreferencesSettingsService.LastSessionTabList = MainPageViewModel.AppInstances.DefaultIfEmpty().Select(tab =>
                {
                    if (tab != null && tab.TabItemArguments != null)
                    {
                        return tab.TabItemArguments.Serialize();
                    }
                    else
                    {
                        var defaultArg = new TabItemArguments() { InitialPageType = typeof(PaneHolderPage), NavigationArg = "Home".GetLocalizedResource() };
                        return defaultArg.Serialize();
                    }
                }).ToList();
            }
        }

        // Occurs when an exception is not handled on the UI thread.
        private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e) => AppUnhandledException(e.Exception);

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
            Logger.UnhandledError(ex, ex.Message);
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
                                    Text = "ExceptionNotificationHeader".GetLocalizedResource()
                                },
                                new AdaptiveText()
                                {
                                    Text = "ExceptionNotificationBody".GetLocalizedResource()
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
                            new ToastButton("ExceptionNotificationReportButton".GetLocalizedResource(), "report")
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

        public static void CloseApp()
        {
            Window.Close();
        }

        public static AppWindow GetAppWindow(Window w)
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(w);

            Microsoft.UI.WindowId windowId =
                Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

            return
                AppWindow.GetFromWindowId(windowId);
        }

        public static MainWindow Window { get; set; }

        public static IntPtr WindowHandle { get; private set; }
    }
}
