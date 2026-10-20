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
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Win32;
using WinRT;

namespace Files.App
{
	/// <summary>
	/// Represents the entry point of UI for Files app.
	/// </summary>
	public partial class App : Application
	{
		public static TaskCompletionSource? SplashScreenLoadingTCS { get; private set; }
		public static string? OutputPath { get; set; }

		private static FlyoutBase? _LastOpenedFlyout;
		private static bool _isWindowTeardownCompleted;
		private static bool _isWindowClosing;
		private static readonly Task<Server.AppService> serverSession = Task.Run(() => new Server.AppService(Environment.ProcessId));
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
			AppDomain.CurrentDomain.ProcessExit += static (_, _) =>
				SafetyExtensions.IgnoreExceptions(() => Ioc.Default.GetService<FileLoggerProvider>()?.TryCompleteAndFlush(TimeSpan.FromSeconds(2)));
		}

		/// <summary>
		/// Gets invoked when the application is launched normally by the end user.
		/// </summary>
		protected override void OnLaunched(LaunchActivatedEventArgs e)
		{
			UiDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

			// Constructed on the UI thread: the ctor subscribes the UI-thread-only Clipboard.ContentChanged
			AppModel = new AppModel();

			_ = ActivateAsync();

			async Task ActivateAsync()
			{
				var appActivationArguments = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
				var isStartupTask = appActivationArguments.Data is Windows.ApplicationModel.Activation.IStartupTaskActivatedEventArgs;

				// Build the DI container off-thread while the window initializes
				var appModel = AppModel;
				Task commandsWarmupTask = Task.CompletedTask;
				var servicesTask = Task.Run(() =>
				{
					try
					{
						var provider = AppLifecycleHelper.ConfigureHost(appModel);

						// Configure Ioc here so Ioc.Default-dependent constructions warm off-thread too
						Ioc.Default.ConfigureServices(provider);

						// Load the appearance settings needed for the first frame off the UI thread.
						if (isStartupTask && provider.GetRequiredService<IGeneralSettingsService>().CloseToBackground)
							return provider;
						var appearanceSettings = provider.GetRequiredService<IAppearanceSettingsService>();
						_ = appearanceSettings.AppThemeMode;
						_ = appearanceSettings.AppThemeBackdropMaterial;

						// Commands are needed for the main page, not for the initial window and backdrop.
						commandsWarmupTask = Task.Run(() =>
						{
							var previousPriority = Thread.CurrentThread.Priority;
							Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
							try
							{
								// Action and context constructors access these statics.
								QuickAccessManager = provider.GetRequiredService<QuickAccessManager>();
								HistoryWrapper = provider.GetRequiredService<StorageHistoryWrapper>();
								FileTagsManager = provider.GetRequiredService<FileTagsManager>();
								LibraryManager = provider.GetRequiredService<LibraryManager>();

								_ = provider.GetRequiredService<ICommandManager>();
							}
							catch (Exception)
							{
								// UI-thread-only constructors run on first use instead.
							}
							finally
							{
								Thread.CurrentThread.Priority = previousPriority;
							}
						});

						return provider;
					}
					catch (Exception)
					{
						// A UI-thread-only service ctor failed off-thread; rebuilt on the UI thread below
						return null;
					}
				});

				// IsDynamicCodeSupported is false on Native AOT, where startup is fast enough to skip the splash screen
				var showSplashScreen = System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported;

				if (!isStartupTask)
				{
					// Construct the window while appearance settings are loaded.
					_ = MainWindow.Instance;
				}

				// Configure the DI (dependency injection) container
				var serviceProvider = await servicesTask;
				if (serviceProvider is null)
				{
					serviceProvider = AppLifecycleHelper.ConfigureHost(appModel);
					Ioc.Default.ConfigureServices(serviceProvider);
				}

				var userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
				var serverInitialization = SafetyExtensions.IgnoreExceptions(async () =>
				{
					await serverSession;
					await Server.AppService.InitializeAsync();
				}, Logger);
				if (isStartupTask && userSettingsService.GeneralSettingsService.CloseToBackground)
				{
					await serverInitialization;
					await SafetyExtensions.IgnoreExceptions(async () => (await serverSession).Dispose(), Logger);
					Exit();
					return;
				}

				if (isStartupTask)
					Program.RegisterCurrentInstance();

				if (showSplashScreen)
				{
					SplashScreenLoadingTCS = new TaskCompletionSource();
					MainWindow.Instance.ShowSplashScreen();
				}

				if (!await MainWindow.Instance.ActivateWithBackdropAsync())
					return;

				await commandsWarmupTask;

				// Configure Sentry
				if (AppLifecycleHelper.AppEnvironment is not AppEnvironment.Dev)
					AppLifecycleHelper.ConfigureSentry();

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

				if (SplashScreenLoadingTCS is not null)
				{
					await SplashScreenLoadingTCS.Task.WithTimeoutAsync(TimeSpan.FromMilliseconds(500));
					SplashScreenLoadingTCS = null;
				}

				_ = SafetyExtensions.IgnoreExceptions(async () => (await serverSession).SetWindowHandle(MainWindow.Instance.WindowHandle), Logger);
				_ = MainWindow.Instance.InitializeApplicationAsync(appActivationArguments.Data);

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

			if (!_isWindowClosing && args.WindowActivationState != WindowActivationState.Deactivated)
				AppModel.IsMainWindowClosed = false;
		}

		/// <summary>
		/// Gets invoked when the application execution is closed.
		/// </summary>
		/// <remarks>
		/// Saves the current state of the app such as opened tabs, and disposes all cached resources.
		/// </remarks>
		private async void Window_Closed(object sender, WindowEventArgs args)
		{
			if (_isWindowTeardownCompleted)
				return;

			args.Handled = true;
			if (_isWindowClosing)
				return;

			if (_LastOpenedFlyout?.IsOpen ?? false)
			{
				_LastOpenedFlyout.Closed += (_, _) => MainWindow.Instance.Close();
				_LastOpenedFlyout.Hide();
				return;
			}

			_isWindowClosing = true;
			AppModel.IsMainWindowClosed = true;
			if (serverSession.IsCompletedSuccessfully)
				SafetyExtensions.IgnoreExceptions(() => serverSession.Result.SetWindowHandle(0), Logger);
			Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().UnregisterKey();
			ActiveSessionTracker.OnActivationChanged(false);

			try
			{
				FilePropertiesHelpers.DestroyCachedWindows();
				MainWindow.Instance.CloseOtherWindows();

				var settings = Ioc.Default.GetRequiredService<IUserSettingsService>();
				var commands = Ioc.Default.GetRequiredService<ICommandManager>();
				if (settings.GeneralSettingsService.ContinueLastSessionOnStartUp || settings.AppSettingsService.RestoreTabsOnStartup)
					AppLifecycleHelper.SaveSessionTabs();
				else
					await commands.CloseAllTabs.ExecuteAsync();

				if (OutputPath is not null)
				{
					var instance = MainPageViewModel.AppInstances.FirstOrDefault(tab => tab.TabItemContent?.IsCurrentInstance == true);
					var items = (instance?.TabItemContent as ShellPanesPage)?.ActivePane?.SlimContentPage?.SelectedItems;
					if (items is not null)
					{
						System.IO.File.WriteAllLines(OutputPath, items.Select(item => item.ItemPath!));
						using var eventHandle = PInvoke.CreateEvent(null, false, false, "FILEDIALOG");
						PInvoke.SetEvent(eventHandle);
					}
				}

				MainWindow.Instance.AppWindow.Hide();
				SafetyExtensions.IgnoreExceptions(() => Ioc.Default.GetRequiredService<InfoPaneViewModel>().UnloadPreview());

				if (settings.GeneralSettingsService.CloseToBackground && settings.AppSettingsService.ShowBackgroundRunningNotification)
				{
					SafetyExtensions.IgnoreExceptions(() =>
					{
						AppToastNotificationHelper.ShowBackgroundRunningToast();
						settings.AppSettingsService.ShowBackgroundRunningNotification = false;
					});
				}

				SafetyExtensions.IgnoreExceptions(() =>
				{
					var dataPackage = Clipboard.GetContent();
					if (dataPackage.Properties.PackageFamilyName == Package.Current.Id.FamilyName && dataPackage.Contains(StandardDataFormats.StorageItems))
						Clipboard.Flush();
				}, Logger);

				await Task.Run(FileOperationsHelpers.WaitForCompletion);
			}
			catch (Exception ex)
			{
				Logger.LogWarning(ex, "Failed to finish window teardown.");
			}
			finally
			{
				await SafetyExtensions.IgnoreExceptions(async () => (await serverSession).Dispose(), Logger);
				_isWindowTeardownCompleted = true;
				MainWindow.Instance.Close();
			}
		}

		/// <summary>
		/// Gets invoked when the last opened flyout is closed.
		/// </summary>
		[DynamicWindowsRuntimeCast(typeof(FlyoutBase))]
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
