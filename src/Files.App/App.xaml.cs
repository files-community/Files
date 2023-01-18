using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Helpers;
using CommunityToolkit.WinUI.Notifications;
using Files.App.Controllers;
using Files.App.DataModels;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.Cloud;
using Files.App.Filesystem.FilesystemHistory;
using Files.App.Helpers;
using Files.App.ServicesImplementation;
using Files.App.ServicesImplementation.DateTimeFormatter;
using Files.App.ServicesImplementation.Settings;
using Files.App.UserControls.MultitaskingControl;
using Files.App.ViewModels;
using Files.App.Views;
using Files.Backend.Services;
using Files.Backend.Services.Settings;
using Files.Backend.Services.SizeProvider;
using Files.Shared;
using Files.Shared.Cloud;
using Files.Shared.Extensions;
using Files.Shared.Services.DateTimeFormatter;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Notifications;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Files.App
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	public partial class App : Application
	{
		private static bool ShowErrorNotification = false;

		public static string OutputPath { get; set; }
		public static StorageHistoryWrapper HistoryWrapper = new StorageHistoryWrapper();
		public static SettingsViewModel AppSettings { get; private set; }
		public static AppModel AppModel { get; private set; }
		public static PreviewPaneViewModel PreviewPaneViewModel { get; private set; }
		public static JumpListManager JumpList { get; private set; }
		public static RecentItems RecentItemsManager { get; private set; }
		public static SidebarPinnedController SidebarPinnedController { get; private set; }
		public static CloudDrivesManager CloudDrivesManager { get; private set; }
		public static NetworkDrivesManager NetworkDrivesManager { get; private set; }
		public static DrivesManager DrivesManager { get; private set; }
		public static WSLDistroManager WSLDistroManager { get; private set; }
		public static LibraryManager LibraryManager { get; private set; }
		public static FileTagsManager FileTagsManager { get; private set; }
		public static AppThemeResourcesHelper AppThemeResourcesHelper { get; private set; }

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
			Services = ConfigureServices();
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
				.AddSingleton<IAppearanceSettingsService, AppearanceSettingsService>((sp) => new AppearanceSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
				.AddSingleton<IPreferencesSettingsService, PreferencesSettingsService>((sp) => new PreferencesSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
				.AddSingleton<IFoldersSettingsService, FoldersSettingsService>((sp) => new FoldersSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
				.AddSingleton<IApplicationSettingsService, ApplicationSettingsService>((sp) => new ApplicationSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
				.AddSingleton<IPreviewPaneSettingsService, PreviewPaneSettingsService>((sp) => new PreviewPaneSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
				.AddSingleton<ILayoutSettingsService, LayoutSettingsService>((sp) => new LayoutSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
				.AddSingleton<IAppSettingsService, AppSettingsService>((sp) => new AppSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
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

		private static void EnsureSettingsAndConfigurationAreBootstrapped()
		{
			AppSettings ??= new SettingsViewModel();
			AppThemeResourcesHelper ??= new AppThemeResourcesHelper();
			JumpList ??= new JumpListManager();
			RecentItemsManager ??= new RecentItems();
			AppModel ??= new AppModel();
			PreviewPaneViewModel ??= new PreviewPaneViewModel();
			LibraryManager ??= new LibraryManager();
			DrivesManager ??= new DrivesManager();
			NetworkDrivesManager ??= new NetworkDrivesManager();
			CloudDrivesManager ??= new CloudDrivesManager();
			WSLDistroManager ??= new WSLDistroManager();
			FileTagsManager ??= new FileTagsManager();
			SidebarPinnedController ??= new SidebarPinnedController();
		}

		private static async Task StartAppCenter()
		{
			try
			{
				if (!AppCenter.Configured)
				{
					var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(@"ms-appx:///Resources/AppCenterKey.txt"));
					var lines = await FileIO.ReadTextAsync(file);
					using var document = System.Text.Json.JsonDocument.Parse(lines);
					var obj = document.RootElement;
					AppCenter.Start(obj.GetProperty("key").GetString(), typeof(Analytics), typeof(Crashes));
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
			var preferencesSettingsService = userSettingsService.PreferencesSettingsService;

			// Start off a list of tasks we need to run before we can continue startup
			await Task.Run(async () =>
			{
				await Task.WhenAll(
					StartAppCenter(),
					DrivesManager.UpdateDrivesAsync(),
					OptionalTask(CloudDrivesManager.UpdateDrivesAsync(), preferencesSettingsService.ShowCloudDrivesSection),
					LibraryManager.UpdateLibrariesAsync(),
					OptionalTask(NetworkDrivesManager.UpdateDrivesAsync(), preferencesSettingsService.ShowNetworkDrivesSection),
					OptionalTask(WSLDistroManager.UpdateDrivesAsync(), preferencesSettingsService.ShowWslSection),
					OptionalTask(FileTagsManager.UpdateFileTagsAsync(), preferencesSettingsService.ShowFileTagsSection),
					SidebarPinnedController.InitializeAsync()
				);
				await Task.WhenAll(
					JumpList.InitializeAsync(),
					ContextFlyoutItemHelper.CachedNewContextMenuEntries
				);
				FileTagsHelper.UpdateTagsDb();
			});

			// Check for required updates
			var updateService = Ioc.Default.GetRequiredService<IUpdateService>();
			await updateService.CheckForUpdates();
			await updateService.DownloadMandatoryUpdates();
			await updateService.CheckLatestReleaseNotesAsync();

			static async Task OptionalTask(Task task, bool condition)
			{
				if (condition)
					await task;
			}
		}

		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used such as when the application is launched to open a specific file.
		/// </summary>
		/// <param name="args">Details about the launch request and process.</param>
		protected override void OnLaunched(LaunchActivatedEventArgs e)
		{
			var activatedEventArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();

			Task.Run(async () => await logWriter.InitializeAsync("debug.log"));
			Logger.Info($"App launched. Launch args type: {activatedEventArgs.Data.GetType().Name}");

			//start tracking app usage
			if (activatedEventArgs.Data is Windows.ApplicationModel.Activation.IActivatedEventArgs iaea)
				SystemInformation.Instance.TrackAppUse(iaea);

			// Initialize MainWindow here
			EnsureWindowIsInitialized();

			EnsureSettingsAndConfigurationAreBootstrapped();

			_ = InitializeAppComponentsAsync().ContinueWith(t => Logger.Warn(t.Exception, "Error during InitializeAppComponentsAsync()"), TaskContinuationOptions.OnlyOnFaulted);

			_ = Window.InitializeApplication(activatedEventArgs.Data);
		}

		private void EnsureWindowIsInitialized()
		{
			Window = new MainWindow();
			Window.Activated += Window_Activated;
			Window.Closed += Window_Closed;
			WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(Window);
		}

		private void Window_Activated(object sender, WindowActivatedEventArgs args)
		{
			if (args.WindowActivationState == WindowActivationState.CodeActivated ||
				args.WindowActivationState == WindowActivationState.PointerActivated)
			{
				ShowErrorNotification = true;
				ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = -Process.GetCurrentProcess().Id;
			}
		}

		public void OnActivated(AppActivationArguments activatedEventArgs)
		{
			Logger.Info($"App activated. Activated args type: {activatedEventArgs.Data.GetType().Name}");
			var data = activatedEventArgs.Data;
			// InitializeApplication accesses UI, needs to be called on UI thread
			_ = Window.DispatcherQueue.EnqueueAsync(() => Window.InitializeApplication(data));
		}

		/// <summary>
		/// Invoked when application execution is being closed. Save application state.
		/// </summary>
		/// <param name="sender">The source of the suspend request.</param>
		/// <param name="args">Details about the suspend request.</param>
		private async void Window_Closed(object sender, WindowEventArgs args)
		{
			// Save application state and stop any background activity

			await Task.Yield(); // Method can take a long time, make sure the window is hidden

			SaveSessionTabs();

			if (OutputPath is not null)
			{
				await SafetyExtensions.IgnoreExceptions(async () =>
				{
					var instance = MainPageViewModel.AppInstances.FirstOrDefault(x => x.Control.TabItemContent.IsCurrentInstance);
					if (instance is null)
						return;
					var items = (instance.Control.TabItemContent as PaneHolderPage)?.ActivePane?.SlimContentPage?.SelectedItems;
					if (items is null)
						return;
					await FileIO.WriteLinesAsync(await StorageFile.GetFileFromPathAsync(OutputPath), items.Select(x => x.ItemPath));
				}, Logger);
			}

			DrivesManager?.Dispose();
			PreviewPaneViewModel?.Dispose();

			// Try to maintain clipboard data after app close
			SafetyExtensions.IgnoreExceptions(() =>
			{
				var dataPackage = Clipboard.GetContent();
				if (dataPackage.Properties.PackageFamilyName == Package.Current.Id.FamilyName)
				{
					if (dataPackage.Contains(StandardDataFormats.StorageItems))
						Clipboard.Flush();
				}
			}, Logger);

			// Wait for ongoing file operations
			FileOperationsHelpers.WaitForCompletion();
		}

		public static void SaveSessionTabs() // Enumerates through all tabs and gets the Path property and saves it to AppSettings.LastSessionPages
		{
			IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			IBundlesSettingsService bundlesSettingsService = Ioc.Default.GetRequiredService<IBundlesSettingsService>();

			bundlesSettingsService.FlushSettings();

			userSettingsService.PreferencesSettingsService.LastSessionTabList = MainPageViewModel.AppInstances.DefaultIfEmpty().Select(tab =>
			{
				if (tab is not null && tab.TabItemArguments is not null)
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

		// Occurs when an exception is not handled on the UI thread.
		private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e) => AppUnhandledException(e.Exception, true);

		// Occurs when an exception is not handled on a background thread.
		// ie. A task is fired and forgotten Task.Run(() => {...})
		private static void OnUnobservedException(object sender, UnobservedTaskExceptionEventArgs e) => AppUnhandledException(e.Exception, false);

		private static void AppUnhandledException(Exception ex, bool shouldShowNotification)
		{
			StringBuilder formattedException = new StringBuilder() { Capacity = 200 };

			formattedException.Append("--------- UNHANDLED EXCEPTION ---------");
			if (ex is not null)
			{
				formattedException.Append($"\n>>>> HRESULT: {ex.HResult}\n");
				if (ex.Message is not null)
				{
					formattedException.Append("\n--- MESSAGE ---");
					formattedException.Append(ex.Message);
				}
				if (ex.StackTrace is not null)
				{
					formattedException.Append("\n--- STACKTRACE ---");
					formattedException.Append(ex.StackTrace);
				}
				if (ex.Source is not null)
				{
					formattedException.Append("\n--- SOURCE ---");
					formattedException.Append(ex.Source);
				}
				if (ex.InnerException is not null)
				{
					formattedException.Append("\n--- INNER ---");
					formattedException.Append(ex.InnerException);
				}
			}
			else
			{
				formattedException.Append("\nException is null!\n");
			}

			formattedException.Append("---------------------------------------");

			Debug.WriteLine(formattedException.ToString());

			Debugger.Break(); // Please check "Output Window" for exception details (View -> Output Window) (CTRL + ALT + O)

			SaveSessionTabs();
			Logger.UnhandledError(ex, ex.Message);

			if (!ShowErrorNotification || !shouldShowNotification)
				return;

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

		public static MainWindow Window { get; set; } = null!;

		public static IntPtr WindowHandle { get; private set; }
	}
}
