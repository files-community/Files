// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
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
		private static SystemTrayIcon? SystemTrayIcon { get; set; }

		public static TaskCompletionSource? SplashScreenLoadingTCS { get; private set; }
		public static string? OutputPath { get; set; }

		private static CommandBarFlyout? _LastOpenedFlyout;
		public static CommandBarFlyout? LastOpenedFlyout
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
		public static RecentItems RecentItemsManager { get; private set; } = null!;
		public static LibraryManager LibraryManager { get; private set; } = null!;
		public static AppModel AppModel { get; private set; } = null!;
		public static ILogger Logger { get; private set; } = null!;

		/// <summary>
		/// Initializes an instance of <see cref="App"/>.
		/// </summary>
		public App()
		{
			InitializeComponent();

			// Configure exception handlers
			UnhandledException += (sender, e) => AppLifecycleHelper.HandleAppUnhandledException(e.Exception, true);
			AppDomain.CurrentDomain.UnhandledException += (sender, e) => AppLifecycleHelper.HandleAppUnhandledException(e.ExceptionObject as Exception, false);
			TaskScheduler.UnobservedTaskException += (sender, e) => AppLifecycleHelper.HandleAppUnhandledException(e.Exception, false);
		}

		/// <summary>
		/// Gets invoked when the application is launched normally by the end user.
		/// </summary>
		protected override void OnLaunched(LaunchActivatedEventArgs e)
		{
			_ = ActivateAsync();

			async Task ActivateAsync()
			{
				// Get AppActivationArguments
				var appActivationArguments = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
				var isStartupTask = appActivationArguments.Data is Windows.ApplicationModel.Activation.IStartupTaskActivatedEventArgs;

				if (!isStartupTask)
				{
					// Initialize and activate MainWindow
					MainWindow.Instance.Activate();

					// Wait for the Window to initialize
					await Task.Delay(10);

					SplashScreenLoadingTCS = new TaskCompletionSource();
					MainWindow.Instance.ShowSplashScreen();
				}

				// Start tracking app usage
				if (appActivationArguments.Data is Windows.ApplicationModel.Activation.IActivatedEventArgs activationEventArgs)
					SystemInformation.Instance.TrackAppUse(activationEventArgs);

				// Configure the DI (dependency injection) container
				var host = AppLifecycleHelper.ConfigureHost();
				Ioc.Default.ConfigureServices(host.Services);

#if STORE || STABLE || PREVIEW
				// Configure AppCenter
				AppLifecycleHelper.ConfigureAppCenter();
#endif

				var userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
				var isLeaveAppRunning = userSettingsService.GeneralSettingsService.LeaveAppRunning;

				if (isStartupTask && !isLeaveAppRunning)
				{
					// Initialize and activate MainWindow
					MainWindow.Instance.Activate();

					// Wait for the Window to initialize
					await Task.Delay(10);

					SplashScreenLoadingTCS = new TaskCompletionSource();
					MainWindow.Instance.ShowSplashScreen();
				}

				// TODO: Replace with DI
				QuickAccessManager = Ioc.Default.GetRequiredService<QuickAccessManager>();
				HistoryWrapper = Ioc.Default.GetRequiredService<StorageHistoryWrapper>();
				FileTagsManager = Ioc.Default.GetRequiredService<FileTagsManager>();
				RecentItemsManager = Ioc.Default.GetRequiredService<RecentItems>();
				LibraryManager = Ioc.Default.GetRequiredService<LibraryManager>();
				Logger = Ioc.Default.GetRequiredService<ILogger<App>>();
				AppModel = Ioc.Default.GetRequiredService<AppModel>();

				// Hook events for the window
				MainWindow.Instance.Closed += Window_Closed;
				MainWindow.Instance.Activated += Window_Activated;

				Logger.LogInformation($"App launched. Launch args type: {appActivationArguments.Data.GetType().Name}");

				if (!(isStartupTask && isLeaveAppRunning))
				{
					// Wait for the UI to update
					await SplashScreenLoadingTCS!.Task.WithTimeoutAsync(TimeSpan.FromMilliseconds(500));
					SplashScreenLoadingTCS = null;

					// Create a system tray icon
					SystemTrayIcon = new SystemTrayIcon().Show();

					_ = MainWindow.Instance.InitializeApplicationAsync(appActivationArguments.Data);
				}

				await AppLifecycleHelper.InitializeAppComponentsAsync();

				if (isStartupTask && isLeaveAppRunning)
				{
					// Create a system tray icon when initialization is done
					SystemTrayIcon = new SystemTrayIcon().Show();
					App.Current.Exit();
				}
				else
					await AppLifecycleHelper.CheckAppUpdate();
			}
		}

		/// <summary>
		/// Gets invoked when the application is activated.
		/// </summary>
		public async Task OnActivatedAsync(AppActivationArguments activatedEventArgs)
		{
			var activatedEventArgsData = activatedEventArgs.Data;
			Logger.LogInformation($"The app is being activated. Activation type: {activatedEventArgsData.GetType().Name}");

			// InitializeApplication accesses UI, needs to be called on UI thread
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(()
				=> MainWindow.Instance.InitializeApplicationAsync(activatedEventArgsData));
		}

		/// <summary>
		/// Gets invoked when the main window is activated.
		/// </summary>
		private void Window_Activated(object sender, WindowActivatedEventArgs args)
		{
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

			// A Workaround for the crash (#10110)
			if (_LastOpenedFlyout?.IsOpen ?? false)
			{
				args.Handled = true;
				_LastOpenedFlyout.Closed += (sender, e) => App.Current.Exit();
				_LastOpenedFlyout.Hide();
				return;
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

				// Save and close all tabs
				AppLifecycleHelper.SaveSessionTabs();
				MainPageViewModel.AppInstances.ForEach(tabItem => tabItem.Unload());
				MainPageViewModel.AppInstances.Clear();

				// Wait for all properties windows to close
				await FilePropertiesHelpers.WaitClosingAll();

				// Sleep current instance
				Program.Pool = new(0, 1, $"Files-{ApplicationService.AppEnvironment}-Instance");

				Thread.Yield();

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

			// Method can take a long time, make sure the window is hidden
			await Task.Yield();

			AppLifecycleHelper.SaveSessionTabs();

			if (OutputPath is not null)
			{
				await SafetyExtensions.IgnoreExceptions(async () =>
				{
					var instance = MainPageViewModel.AppInstances.FirstOrDefault(x => x.TabItemContent.IsCurrentInstance);
					if (instance is null)
						return;

					var items = (instance.TabItemContent as PaneHolderPage)?.ActivePane?.SlimContentPage?.SelectedItems;
					if (items is null)
						return;

					await FileIO.WriteLinesAsync(await StorageFile.GetFileFromPathAsync(OutputPath), items.Select(x => x.ItemPath));
				},
				Logger);
			}

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

			// Dispose git operations' thread
			GitHelpers.TryDispose();

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
			if (sender is not CommandBarFlyout commandBarFlyout)
				return;

			commandBarFlyout.Closed -= LastOpenedFlyout_Closed;
			if (_LastOpenedFlyout == commandBarFlyout)
				_LastOpenedFlyout = null;
		}
	}
}
