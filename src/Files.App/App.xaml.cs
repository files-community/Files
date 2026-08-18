// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Helpers.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.Windows.AppLifecycle;
using Windows.Win32;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Files.App
{
	/// <summary>
	/// Represents the entry point of UI for Files app.
	/// </summary>
	public partial class App : Application
	{
		public static readonly System.Diagnostics.Stopwatch StartupStopwatch = System.Diagnostics.Stopwatch.StartNew();

		public static SystemTrayIcon? SystemTrayIcon { get; private set; }

		public static TaskCompletionSource? SplashScreenLoadingTCS { get; private set; }
		public static string? OutputPath { get; set; }

		private static FlyoutBase? _LastOpenedFlyout;
		public static FlyoutBase? LastOpenedFlyout
		{
			set
			{
				_LastOpenedFlyout = value;

				if (_LastOpenedFlyout is not null)
					_LastOpenedFlyout.Closed += LastOpenedFlyout_Closed;
			}
		}

		// TODO: Replace with DI
		public static QuickAccessManager QuickAccessManager { get; private set; } = null!;
		public static StorageHistoryWrapper HistoryWrapper { get; private set; } = null!;
		public static FileTagsManager FileTagsManager { get; private set; } = null!;
		public static LibraryManager LibraryManager { get; private set; } = null!;
		public static AppModel AppModel { get; private set; } = null!;
		public static ILogger Logger { get; private set; } = NullLogger.Instance;

		public static Microsoft.UI.Dispatching.DispatcherQueue? UiDispatcher { get; private set; }

		/// <summary>
		/// Initializes an instance of <see cref="App"/>.
		/// </summary>
		public App()
		{
			InitializeComponent();

			// Configure exception handlers
			AppLifecycleHelper.RecordFirstChanceExceptions();
			UnhandledException += (sender, e) => AppLifecycleHelper.HandleAppUnhandledException(e.Exception, true, "Application.UnhandledException", e.Message);
			AppDomain.CurrentDomain.UnhandledException += (sender, e) => AppLifecycleHelper.HandleAppUnhandledException(e.ExceptionObject as Exception, false, "AppDomain.UnhandledException");
			TaskScheduler.UnobservedTaskException += (sender, e) => AppLifecycleHelper.HandleAppUnhandledException(e.Exception, false, "TaskScheduler.UnobservedTaskException");
		}

		/// <summary>
		/// Merges the app-wide style dictionaries. Called at the start of <see cref="OnLaunched"/>:
		/// the Application.Resources ABI throws E_UNEXPECTED until construction completes, and
		/// MainWindow's XAML hasn't parsed yet, so StaticResource lookups still resolve.
		/// The MenuFlyoutSubItem dictionaries are menu-only and load after the first frame instead.
		/// </summary>
		private void MergeStyleDictionaries()
		{
			foreach (var source in (ReadOnlySpan<string>)
			[
				"ms-appx:///Files.App.Controls/Themes/Generic.xaml",
				"ms-appx:///Styles/PathIcons.xaml",
				"ms-appx:///Styles/TextBlockStyles.xaml",
				"ms-appx:///Styles/ShimmerStyles.xaml",
			])
			{
				Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(source) });
			}
		}

		private static bool _deferredDictionariesLoaded;

		/// <summary>
		/// Loads resource dictionaries that only flyout menus need; called after the first frame.
		/// </summary>
		public static void LoadDeferredResourceDictionaries()
		{
			if (_deferredDictionariesLoaded)
				return;

			_deferredDictionariesLoaded = true;

			Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///Styles/MenuFlyoutSubItemWithImageStyle.xaml") });
			Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///Styles/MenuFlyoutSubItemWithThemedIconStyle.xaml") });
		}

		/// <summary>
		/// Gets invoked when the application is launched normally by the end user.
		/// </summary>
		protected override void OnLaunched(LaunchActivatedEventArgs e)
		{
			UiDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

			// Constructed here (UI thread) rather than by the container: the ctor subscribes
			// Clipboard.ContentChanged, which is UI-thread-only, and contexts resolved during the
			// off-thread command prewarm read App.AppModel
			AppModel = new AppModel();

			MergeStyleDictionaries();
			_ = ActivateAsync();

			async Task ActivateAsync()
			{
				// Build the DI container off-thread while the window initializes
				var appModel = AppModel;
				var servicesTask = Task.Run(() =>
				{
					try
					{
						var provider = AppLifecycleHelper.ConfigureHost(appModel);

						// Configured here (not after the await) so constructions that resolve
						// through Ioc.Default can also be warmed off the UI thread
						Ioc.Default.ConfigureServices(provider);

						// Warm the settings file read off the UI thread
						_ = provider.GetRequiredService<IGeneralSettingsService>().LeaveAppRunning;
						_ = provider.GetRequiredService<IAppearanceSettingsService>().AppThemeBackdropMaterial;

						// Thread-safe to construct (file watchers, event subscriptions), and several
						// action/context constructors resolved by the command prewarm below read them
						// through these statics
						QuickAccessManager = provider.GetRequiredService<QuickAccessManager>();
						HistoryWrapper = provider.GetRequiredService<StorageHistoryWrapper>();
						FileTagsManager = provider.GetRequiredService<FileTagsManager>();
						LibraryManager = provider.GetRequiredService<LibraryManager>();

						// Warm the command manager (every command and hotkey) off the UI thread,
						// below normal priority so the window building concurrently on the UI
						// thread wins every core
						var previousPriority = Thread.CurrentThread.Priority;
						Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
						try
						{
							_ = provider.GetRequiredService<ICommandManager>();
						}
						catch (Exception)
						{
							// A command constructor that requires the UI thread (COMException from a
							// XAML static, or a not-yet-assigned App static) aborts the warm-up;
							// commands then construct on first use on the UI thread instead
						}
						finally
						{
							Thread.CurrentThread.Priority = previousPriority;
						}

						return provider;
					}
					catch (Exception)
					{
						// A service constructor that requires the UI thread failed the off-thread
						// build; the container is rebuilt on the UI thread below, where the same
						// failure would surface through the normal unhandled-exception handlers
						return null;
					}
				});

				// Get AppActivationArguments
				var appActivationArguments = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
				var isStartupTask = appActivationArguments.Data is Windows.ApplicationModel.Activation.IStartupTaskActivatedEventArgs;

				// IsDynamicCodeSupported is false on Native AOT, where startup is fast enough to skip the splash screen
				var showSplashScreen = System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported;

				if (!isStartupTask)
				{
					// Initialize and activate MainWindow
					MainWindow.Instance.Activate();

					if (showSplashScreen)
					{
						// Wait for the Window to initialize
						await Task.Delay(10);

						SplashScreenLoadingTCS = new TaskCompletionSource();
						MainWindow.Instance.ShowSplashScreen();
					}
				}

				// Configure the DI (dependency injection) container
				var serviceProvider = await servicesTask;
				if (serviceProvider is null)
				{
					serviceProvider = AppLifecycleHelper.ConfigureHost(appModel);
					Ioc.Default.ConfigureServices(serviceProvider);
				}
				Logger = Ioc.Default.GetRequiredService<ILogger<App>>();
				Logger.LogInformation($"Startup: DI ready at {StartupStopwatch.ElapsedMilliseconds}ms");

				// Configure Sentry
				if (AppLifecycleHelper.AppEnvironment is not AppEnvironment.Dev)
					AppLifecycleHelper.ConfigureSentry();

				var userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
				var isLeaveAppRunning = userSettingsService.GeneralSettingsService.LeaveAppRunning;

				if (isStartupTask && !isLeaveAppRunning)
				{
					// Initialize and activate MainWindow
					MainWindow.Instance.Activate();

					if (showSplashScreen)
					{
						// Wait for the Window to initialize
						await Task.Delay(10);

						SplashScreenLoadingTCS = new TaskCompletionSource();
						MainWindow.Instance.ShowSplashScreen();
					}
				}

				// TODO: Replace with DI
				QuickAccessManager = Ioc.Default.GetRequiredService<QuickAccessManager>();
				HistoryWrapper = Ioc.Default.GetRequiredService<StorageHistoryWrapper>();
				FileTagsManager = Ioc.Default.GetRequiredService<FileTagsManager>();
				LibraryManager = Ioc.Default.GetRequiredService<LibraryManager>();
				Logger = Ioc.Default.GetRequiredService<ILogger<App>>();
				AppModel = Ioc.Default.GetRequiredService<AppModel>();

				// Hook events for the window
				MainWindow.Instance.Closed += Window_Closed;
				MainWindow.Instance.Activated += Window_Activated;

				Logger.LogInformation($"App launched. Launch args type: {appActivationArguments.Data.GetType().Name}");

				if (!(isStartupTask && isLeaveAppRunning))
				{
					if (SplashScreenLoadingTCS is not null)
					{
						// Wait for the UI to update
						await SplashScreenLoadingTCS.Task.WithTimeoutAsync(TimeSpan.FromMilliseconds(500));
						SplashScreenLoadingTCS = null;
					}

					// Create the system tray icon after the first frame has had a chance to render
					MainWindow.Instance.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
					{
						SystemTrayIcon = new SystemTrayIcon();
						if (userSettingsService.GeneralSettingsService.ShowSystemTrayIcon)
							SystemTrayIcon.Show();
					});

					_ = MainWindow.Instance.InitializeApplicationAsync(appActivationArguments.Data);
				}
				else
				{
					// Create a system tray icon
					SystemTrayIcon = new SystemTrayIcon();
					if (userSettingsService.GeneralSettingsService.ShowSystemTrayIcon)
						SystemTrayIcon.Show();

					// Sleep current instance
					Program.Pool = new(0, 1, $"Files-{AppLifecycleHelper.AppEnvironment}-Instance");

					Thread.Yield();

					if (Program.Pool.WaitOne())
					{
						// Resume the instance
						Program.Pool.Dispose();
						Program.Pool = null;
					}
				}

				await AppLifecycleHelper.InitializeAppComponentsAsync();
			}
		}

		/// <summary>
		/// Gets invoked when the application is activated.
		/// </summary>
		public async Task OnActivatedAsync(AppActivationArguments activatedEventArgs)
		{
			var activatedEventArgsData = activatedEventArgs.Data;

			Logger.LogInformation($"The app is being activated. Activation type: {activatedEventArgsData?.GetType().Name ?? "Unknown"}");

			// InitializeApplication accesses UI, needs to be called on UI thread
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(()
				=> MainWindow.Instance.InitializeApplicationAsync(activatedEventArgsData));
		}

		/// <summary>
		/// Gets invoked when the main window is activated.
		/// </summary>
		private void Window_Activated(object sender, WindowActivatedEventArgs args)
		{
			Logger.LogInformation($"Window_Activated: State={args.WindowActivationState}");

			ActiveSessionTracker.OnActivationChanged(args.WindowActivationState != WindowActivationState.Deactivated);

			if (args.WindowActivationState != WindowActivationState.Deactivated)
				AppModel.IsMainWindowClosed = false;

			// TODO(s): Is this code still needed?
			if (args.WindowActivationState != WindowActivationState.CodeActivated ||
				args.WindowActivationState != WindowActivationState.PointerActivated)
				return;

			ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = -Environment.ProcessId;
		}

		/// <summary>
		/// Gets invoked when the application execution is closed.
		/// </summary>
		/// <remarks>
		/// Saves the current state of the app such as opened tabs, and disposes all cached resources.
		/// </remarks>
		private async void Window_Closed(object sender, WindowEventArgs args)
		{
			// Save application state and stop any background activity
			IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			StatusCenterViewModel statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();
			ICommandManager commandManager = Ioc.Default.GetRequiredService<ICommandManager>();

			// A Workaround for the crash (#10110)
			if (_LastOpenedFlyout?.IsOpen ?? false)
			{
				args.Handled = true;
				_LastOpenedFlyout.Closed += (sender, e) => App.Current.Exit();
				_LastOpenedFlyout.Hide();
				return;
			}

			// Persist the final active stretch; it is reported on the next launch
			ActiveSessionTracker.OnActivationChanged(false);

			// Save the current tab list in case it was overwriten by another instance
			if (userSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp || userSettingsService.AppSettingsService.RestoreTabsOnStartup)
				AppLifecycleHelper.SaveSessionTabs();
			else
				await commandManager.CloseAllTabs.ExecuteAsync();

			if (OutputPath is not null)
			{
				var instance = MainPageViewModel.AppInstances.FirstOrDefault(x =>
					(x.TabItemContent ?? throw new InvalidOperationException("A tab does not have content.")).IsCurrentInstance);
				if (instance is null)
					return;

				var items = (instance.TabItemContent as ShellPanesPage)?.ActivePane?.SlimContentPage?.SelectedItems;
				if (items is null)
					return;

				var results = items.Select(x => x.ItemPath!).ToList();
				System.IO.File.WriteAllLines(OutputPath, results);

				using var eventHandle = PInvoke.CreateEvent(null, false, false, "FILEDIALOG");
				PInvoke.SetEvent(eventHandle);
			}

			// Continue running the app on the background
			if (userSettingsService.GeneralSettingsService.LeaveAppRunning &&
				!AppModel.ForceProcessTermination &&
				!Process.GetProcessesByName("Files").Any(x => x.Id != Environment.ProcessId))
			{
				// Close open content dialogs
				UIHelpers.CloseAllDialogs();

				// Close all notification banners except in progress
				statusCenterViewModel.RemoveAllCompletedItems();

				// Cache the window instead of closing it
				MainWindow.Instance.AppWindow.Hide();
				AppModel.IsMainWindowClosed = true;

				// Close all tabs
				MainPageViewModel.AppInstances.ForEach(tabItem => tabItem.Unload());
				MainPageViewModel.AppInstances.Clear();

				// Wait for all properties windows to close
				await FilePropertiesHelpers.WaitClosingAll();

				// Sleep current instance
				Program.Pool = new(0, 1, $"Files-{AppLifecycleHelper.AppEnvironment}-Instance");

				Thread.Yield();

				// Displays a notification the first time the app goes to the background
				if (userSettingsService.AppSettingsService.ShowBackgroundRunningNotification)
				{
					SafetyExtensions.IgnoreExceptions(() =>
					{
						AppToastNotificationHelper.ShowBackgroundRunningToast();

						userSettingsService.AppSettingsService.ShowBackgroundRunningNotification = false;
					});
				}

				if (Program.Pool.WaitOne())
				{
					// Resume the instance
					Program.Pool.Dispose();
					Program.Pool = null;

					if (!AppModel.ForceProcessTermination)
					{
						args.Handled = true;
						_ = AppLifecycleHelper.CheckAppUpdate();
						return;
					}
				}
			}

			// Stop the tray icon's hidden window before continuing teardown so a late "Quit"
			// click can't dispatch into OnQuitClicked once Application.Current is null.
			SystemTrayIcon?.Dispose();
			SystemTrayIcon = null;

			// Method can take a long time, make sure the window is hidden
			await Task.Yield();

			// Try to maintain clipboard data after app close
			SafetyExtensions.IgnoreExceptions(() =>
			{
				var dataPackage = Clipboard.GetContent();
				if (dataPackage.Properties.PackageFamilyName == Package.Current.Id.FamilyName)
				{
					if (dataPackage.Contains(StandardDataFormats.StorageItems))
						Clipboard.Flush();
				}
			},
			Logger);

			// Destroy cached properties windows
			FilePropertiesHelpers.DestroyCachedWindows();
			AppModel.IsMainWindowClosed = true;

			// Wait for ongoing file operations
			FileOperationsHelpers.WaitForCompletion();
		}

		/// <summary>
		/// Gets invoked when the last opened flyout is closed.
		/// </summary>
		private static void LastOpenedFlyout_Closed(object? sender, object e)
		{
			if (sender is not FlyoutBase flyoutBase)
				return;

			flyoutBase.Closed -= LastOpenedFlyout_Closed;
			if (_LastOpenedFlyout == flyoutBase)
				_LastOpenedFlyout = null;
		}
	}
}
