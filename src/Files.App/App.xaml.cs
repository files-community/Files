// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
#if STORE || STABLE || PREVIEW
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
#endif
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Files.App
{
	public partial class App : Application
	{
		private static bool ShowErrorNotification = false;
		public static string OutputPath { get; set; }
		public static CommandBarFlyout? LastOpenedFlyout { get; set; }
		public static TaskCompletionSource? SplashScreenLoadingTCS { get; private set; }

		// TODO: Remove those all static instances
		public static StorageHistoryWrapper HistoryWrapper { get; private set; }
		public static AppModel AppModel { get; private set; }
		public static RecentItems RecentItemsManager { get; private set; }
		public static QuickAccessManager QuickAccessManager { get; private set; }
		public static CloudDrivesManager CloudDrivesManager { get; private set; }
		public static WSLDistroManager WSLDistroManager { get; private set; }
		public static LibraryManager LibraryManager { get; private set; }
		public static FileTagsManager FileTagsManager { get; private set; }
		public static SecondaryTileHelper SecondaryTileHelper { get; private set; }
		public static ILogger Logger { get; private set; }

		public App()
		{
			InitializeComponent();

			InitializeApp();
		}

		private void InitializeApp()
		{
			// Configure exception handlers
			UnhandledException += App_UnhandledException;
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

#if STORE || STABLE || PREVIEW
			try
			{
				// AppCenter secret is injected in builds/azure-pipelines-release.yml
				if (!AppCenter.Configured)
					AppCenter.Start("appcenter.secret", typeof(Analytics), typeof(Crashes));
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "AppCenter could not be started.");
			}
#endif
		}

		private static async Task InitializeAppComponentsAsync()
		{
			var userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			var addItemService = Ioc.Default.GetRequiredService<IAddItemService>();
			var generalSettingsService = userSettingsService.GeneralSettingsService;

			// Start off a list of tasks we need to run before we can continue startup
			await Task.Run(async () =>
			{
				await Task.WhenAll(
					OptionalTask(CloudDrivesManager.UpdateDrivesAsync(), generalSettingsService.ShowCloudDrivesSection),
					LibraryManager.UpdateLibrariesAsync(),
					OptionalTask(WSLDistroManager.UpdateDrivesAsync(), generalSettingsService.ShowWslSection),
					OptionalTask(FileTagsManager.UpdateFileTagsAsync(), generalSettingsService.ShowFileTagsSection),
					QuickAccessManager.InitializeAsync()
				);

				await Task.WhenAll(
					JumpListHelper.InitializeUpdatesAsync(),
					addItemService.InitializeAsync(),
					ContextMenu.WarmUpQueryContextMenuAsync()
				);

				FileTagsHelper.UpdateTagsDb();
			});

			await AppLifecycleHelper.CheckForRequiredUpdates();

			static async Task OptionalTask(Task task, bool condition)
			{
				if (condition)
					await task;
			}
		}

		private void InitializeMainWindow()
		{
			// Get the MainWindow instance
			var window = MainWindow.Instance;

			// Hook events for the window
			window.Activated += Window_Activated;
			window.Closed += Window_Closed;

			// Attempt to activate it
			window.Activate();
		}

		protected override void OnLaunched(LaunchActivatedEventArgs e)
		{
			_ = ActivateAsync();

			async Task ActivateAsync()
			{
				// Initialize and activate MainWindow
				InitializeMainWindow();

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

				// TODO: Remove initialization here and move out all classes (visible below) out of App.xaml.cs
				HistoryWrapper ??= new();
				RecentItemsManager ??= new();
				AppModel ??= new();
				LibraryManager ??= new();
				CloudDrivesManager ??= new();
				WSLDistroManager ??= new();
				FileTagsManager ??= new();
				QuickAccessManager ??= new();
				SecondaryTileHelper ??= new();

				// TODO: Remove App.Logger instance and replace with DI
				Logger = Ioc.Default.GetRequiredService<ILogger<App>>();
				Logger.LogInformation($"App launched. Launch args type: {appActivationArguments.Data.GetType().Name}");

				// Wait for the UI to update
				await SplashScreenLoadingTCS!.Task.WithTimeoutAsync(TimeSpan.FromMilliseconds(500));
				SplashScreenLoadingTCS = null;

				_ = InitializeAppComponentsAsync().ContinueWith(t => Logger.LogWarning(t.Exception, "Error during InitializeAppComponentsAsync()"), TaskContinuationOptions.OnlyOnFaulted);

				_ = MainWindow.Instance.InitializeApplication(appActivationArguments.Data);
			}
		}

		public void OnActivated(AppActivationArguments activatedEventArgs)
		{
			Logger.LogInformation($"App activated. Activated args type: {activatedEventArgs.Data.GetType().Name}");
			var data = activatedEventArgs.Data;

			// InitializeApplication accesses UI, needs to be called on UI thread
			_ = MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => MainWindow.Instance.InitializeApplication(data));
		}

		private void Window_Activated(object sender, WindowActivatedEventArgs args)
		{
			// TODO(s): Is this code still needed?
			if (args.WindowActivationState is not (WindowActivationState.CodeActivated or WindowActivationState.PointerActivated))
				return;

			ShowErrorNotification = true;
			ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = -Process.GetCurrentProcess().Id;
		}

		private async void Window_Closed(object sender, WindowEventArgs args)
		{
			// Save application state and stop any background activity

			// A Workaround for the crash (#10110)
			if (LastOpenedFlyout?.IsOpen ?? false)
			{
				args.Handled = true;
				LastOpenedFlyout.Closed += (sender, e) => App.Current.Exit();
				LastOpenedFlyout.Hide();
				return;
			}

			if (Ioc.Default.GetRequiredService<IUserSettingsService>().GeneralSettingsService.LeaveAppRunning &&
				!Process.GetProcessesByName("Files").Any(x => x.Id != Process.GetCurrentProcess().Id))
			{
				// Close open content dialogs
				UIHelpers.CloseAllDialogs();

				// Cache the window instead of closing it
				MainWindow.Instance.AppWindow.Hide();
				args.Handled = true;

				// Save and close all tabs
				AppLifecycleHelper.SaveSessionTabs();
				MainPageViewModel.AppInstances.ForEach(tabItem => tabItem.Unload());
				MainPageViewModel.AppInstances.Clear();
				await Task.Delay(500);

				// Wait for all properties windows to close
				await FilePropertiesHelpers.WaitClosingAll();

				// Sleep current instance
				Program.Pool = new(0, 1, "Files-Instance");
				Thread.Yield();
				if (Program.Pool.WaitOne())
				{
					// Resume the instance
					Program.Pool.Dispose();
					MainWindow.Instance.AppWindow.Show();
					MainWindow.Instance.Activate();

					_ = AppLifecycleHelper.CheckForRequiredUpdates();

					MainWindow.Instance.EnsureWindowIsInitialized().Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
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
					var instance = MainPageViewModel.AppInstances.FirstOrDefault(x => x.Control.TabItemContent.IsCurrentInstance);
					if (instance is null)
						return;

					var items = (instance.Control.TabItemContent as PaneHolderPage)?.ActivePane?.SlimContentPage?.SelectedItems;
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

		#region Exception Handlers

		/// <summary>
		/// Occurs when an exception is not handled on the UI thread.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
		{
			AppLifecycleHelper.NotifyUnhandledException(e.Exception, true && ShowErrorNotification);
		}

		private static void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
		{
			AppLifecycleHelper.NotifyUnhandledException(e.ExceptionObject as Exception, false && ShowErrorNotification);
		}

		/// <summary>
		/// Occurs when an exception is not handled on a background thread.
		/// i.e. A task is fired and forgotten Task.Run(() => {...})
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
		{
			AppLifecycleHelper.NotifyUnhandledException(e.Exception, false && ShowErrorNotification);
		}

		#endregion
	}
}
