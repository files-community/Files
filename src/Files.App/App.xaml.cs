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
		public static string? OutputPath { get; set; }
		public static CommandBarFlyout? LastOpenedFlyout { get; set; }
		public static TaskCompletionSource? SplashScreenLoadingTCS { get; private set; }

		// TODO: Replace with DI
		public static StorageHistoryWrapper HistoryWrapper { get; } = new();
		public static AppModel AppModel { get; private set; } = new();
		public static RecentItems RecentItemsManager { get; private set; } = new();
		public static QuickAccessManager QuickAccessManager { get; private set; } = new();
		public static LibraryManager LibraryManager { get; private set; } = new();
		public static FileTagsManager FileTagsManager { get; private set; } = new();
		public static ILogger Logger { get; private set; } = null!;

		/// <summary>
		/// Initializes an instance of <see cref="App"/>.
		/// </summary>
		public App()
		{
			InitializeComponent();

			// Configure exception handlers
			UnhandledException += AppLifecycleHelper.App_UnhandledException;
			AppDomain.CurrentDomain.UnhandledException += AppLifecycleHelper.CurrentDomain_UnhandledException;
			TaskScheduler.UnobservedTaskException += AppLifecycleHelper.TaskScheduler_UnobservedTaskException;

#if STORE || STABLE || PREVIEW
			AppLifecycleHelper.ConfigureAppCenter();
#endif
		}

		/// <summary>
		/// Invoked when the application is launched normally by the end user.
		/// Other entry points will be used such as when the application is launched to open a specific file.
		/// </summary>
		protected override void OnLaunched(LaunchActivatedEventArgs e)
		{
			_ = ActivateAsync();

			async Task ActivateAsync()
			{
				// Initialize and activate MainWindow
				MainWindow.Instance.Activate();

				// Wait for the Window to initialize
				await Task.Delay(10);

				SplashScreenLoadingTCS = new TaskCompletionSource();
				MainWindow.Instance.ShowSplashScreen();

				// Get AppActivationArguments
				var appActivationArguments = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();

				// Start tracking app usage
				if (appActivationArguments.Data is Windows.ApplicationModel.Activation.IActivatedEventArgs activationEventArgs)
					SystemInformation.Instance.TrackAppUse(activationEventArgs);

				// Configure Host and IoC
				var host = AppLifecycleHelper.ConfigureHost();
				Ioc.Default.ConfigureServices(host.Services);

				// Hook events for the window
				MainWindow.Instance.Closed += Window_Closed;
				MainWindow.Instance.Activated += Window_Activated;

				// TODO: Remove App.Logger instance and replace with DI
				Logger = Ioc.Default.GetRequiredService<ILogger<App>>();
				Logger.LogInformation($"App launched. Launch args type: {appActivationArguments.Data.GetType().Name}");

				// Wait for the UI to update
				await SplashScreenLoadingTCS!.Task.WithTimeoutAsync(TimeSpan.FromMilliseconds(500));
				SplashScreenLoadingTCS = null;

				_ = AppLifecycleHelper.InitializeAppComponentsAsync();
				_ = MainWindow.Instance.InitializeApplicationAsync(appActivationArguments.Data);
			}
		}

		/// <summary>
		/// Invoked when the application is activated.
		/// </summary>
		public void OnActivated(AppActivationArguments activatedEventArgs)
		{
			Logger.LogInformation($"The app is being activated. Activation type: {activatedEventArgs.Data.GetType().Name}");

			// InitializeApplication accesses UI, needs to be called on UI thread
			_ = MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(()
				=> MainWindow.Instance.InitializeApplicationAsync(activatedEventArgs.Data));
		}

		/// <summary>
		/// Invoked when the main window is activated.
		/// </summary>
		private void Window_Activated(object sender, WindowActivatedEventArgs args)
		{
			// TODO(s): Is this code still needed?
			if (args.WindowActivationState != WindowActivationState.CodeActivated ||
				args.WindowActivationState != WindowActivationState.PointerActivated)
				return;

			ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = -Process.GetCurrentProcess().Id;
		}

		/// <summary>
		/// Invoked when application execution is being closed. Save application state.
		/// </summary>
		private async void Window_Closed(object sender, WindowEventArgs args)
		{
			// Save application state and stop any background activity
			IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			StatusCenterViewModel statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

			// A Workaround for the crash (#10110)
			if (LastOpenedFlyout?.IsOpen ?? false)
			{
				args.Handled = true;
				LastOpenedFlyout.Closed += (sender, e) => App.Current.Exit();
				LastOpenedFlyout.Hide();
				return;
			}

			if (userSettingsService.GeneralSettingsService.LeaveAppRunning &&
				!AppModel.ForceProcessTermination &&
				!Process.GetProcessesByName("Files").Any(x => x.Id != Process.GetCurrentProcess().Id))
			{
				// Close open content dialogs
				UIHelpers.CloseAllDialogs();

				// Close all notification banners except in progress
				statusCenterViewModel.RemoveAllCompletedItems();

				// Cache the window instead of closing it
				MainWindow.Instance.AppWindow.Hide();
				args.Handled = true;

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

					_ = AppLifecycleHelper.CheckAppUpdate();
				}

				return;
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

			// Destroy cached properties windows
			FilePropertiesHelpers.DestroyCachedWindows();
			AppModel.IsMainWindowClosed = true;

			// Wait for ongoing file operations
			FileOperationsHelpers.WaitForCompletion();
		}
	}
}
