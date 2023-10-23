// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
using CommunityToolkit.WinUI.Notifications;
using Files.App.Helpers;
using Files.App.Services.DateTimeFormatter;
using Files.App.Services.Settings;
using Files.App.Storage.FtpStorage;
using Files.App.Storage.NativeStorage;
using Files.App.UserControls.TabBar;
using Files.App.ViewModels.Settings;
using Files.Core.Services.SizeProvider;
using Files.Core.Storage;
#if STORE || STABLE || PREVIEW
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Windows.AppLifecycle;
using System.IO;
using System.Text;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Notifications;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Files.App
{
	public partial class App : Application
	{
		private IHost? _host;

		private static bool ShowErrorNotification = false;
		public static string OutputPath { get; set; }
		public static CommandBarFlyout? LastOpenedFlyout { get; set; }
		public static TaskCompletionSource? SplashScreenLoadingTCS { get; private set; }

		public static StorageHistoryWrapper HistoryWrapper { get; } = new();
		public static AppModel AppModel { get; private set; }
		public static RecentItems RecentItemsManager { get; private set; }
		public static QuickAccessManager QuickAccessManager { get; private set; }
		public static CloudDrivesManager CloudDrivesManager { get; private set; }
		public static WSLDistroManager WSLDistroManager { get; private set; }
		public static LibraryManager LibraryManager { get; private set; }
		public static FileTagsManager FileTagsManager { get; private set; }

		public static ILogger Logger { get; private set; }
		public static SecondaryTileHelper SecondaryTileHelper { get; private set; } = new();

		/// <summary>
		/// Initializes the singleton application object. This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			InitializeComponent();
			EnsureEarlyApp();
		}

		private void EnsureEarlyApp()
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

		private IHost ConfigureHost()
		{
			return Host.CreateDefaultBuilder()
				.UseEnvironment(ApplicationService.AppEnvironment.ToString())
				.ConfigureLogging(builder => builder
					.AddProvider(new FileLoggerProvider(Path.Combine(ApplicationData.Current.LocalFolder.Path, "debug.log")))
					.SetMinimumLevel(LogLevel.Information))
				.ConfigureServices(services => services
					.AddSingleton<IUserSettingsService, UserSettingsService>()
					.AddSingleton<IAppearanceSettingsService, AppearanceSettingsService>(sp => new AppearanceSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
					.AddSingleton<IGeneralSettingsService, GeneralSettingsService>(sp => new GeneralSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
					.AddSingleton<IFoldersSettingsService, FoldersSettingsService>(sp => new FoldersSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
					.AddSingleton<IApplicationSettingsService, ApplicationSettingsService>(sp => new ApplicationSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
					.AddSingleton<IPreviewPaneSettingsService, PreviewPaneSettingsService>(sp => new PreviewPaneSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
					.AddSingleton<ILayoutSettingsService, LayoutSettingsService>(sp => new LayoutSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
					.AddSingleton<IAppSettingsService, AppSettingsService>(sp => new AppSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
					.AddSingleton<IFileTagsSettingsService, FileTagsSettingsService>()
					.AddSingleton<IPageContext, PageContext>()
					.AddSingleton<IContentPageContext, ContentPageContext>()
					.AddSingleton<IDisplayPageContext, DisplayPageContext>()
					.AddSingleton<IWindowContext, WindowContext>()
					.AddSingleton<IMultitaskingContext, MultitaskingContext>()
					.AddSingleton<ITagsContext, TagsContext>()
					.AddSingleton<IDialogService, DialogService>()
					.AddSingleton<IImageService, ImagingService>()
					.AddSingleton<IThreadingService, ThreadingService>()
					.AddSingleton<ILocalizationService, LocalizationService>()
					.AddSingleton<ICloudDetector, CloudDetector>()
					.AddSingleton<IFileTagsService, FileTagsService>()
					.AddSingleton<ICommandManager, CommandManager>()
					.AddSingleton<IModifiableCommandManager, ModifiableCommandManager>()
					.AddSingleton<IApplicationService, ApplicationService>()
#if UWP
					.AddSingleton<IStorageService, WindowsStorageService>()
#else
					.AddSingleton<IStorageService, NativeStorageService>()
#endif
					.AddSingleton<IFtpStorageService, FtpStorageService>()
					.AddSingleton<IAddItemService, AddItemService>()
#if STABLE || PREVIEW
					.AddSingleton<IUpdateService, SideloadUpdateService>()
#else
					.AddSingleton<IUpdateService, UpdateService>()
#endif
					.AddSingleton<IPreviewPopupService, PreviewPopupService>()
					.AddSingleton<IDateTimeFormatterFactory, DateTimeFormatterFactory>()
					.AddSingleton<IDateTimeFormatter, UserDateTimeFormatter>()
					.AddSingleton<IVolumeInfoFactory, VolumeInfoFactory>()
					.AddSingleton<ISizeProvider, UserSizeProvider>()
					.AddSingleton<IQuickAccessService, QuickAccessService>()
					.AddSingleton<IResourcesService, ResourcesService>()
					.AddSingleton<IJumpListService, JumpListService>()
					.AddSingleton<IRemovableDrivesService, RemovableDrivesService>()
					.AddSingleton<INetworkDrivesService, NetworkDrivesService>()
					.AddSingleton<MainPageViewModel>()
					.AddSingleton<PreviewPaneViewModel>()
					.AddSingleton<SidebarViewModel>()
					.AddSingleton<SettingsViewModel>()
					.AddSingleton<DrivesViewModel>()
					.AddSingleton<NetworkDrivesViewModel>()
					.AddSingleton<StatusCenterViewModel>()
					.AddSingleton<AppearanceViewModel>()
				).Build();
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
					OptionalTaskAsync(CloudDrivesManager.UpdateDrivesAsync(), generalSettingsService.ShowCloudDrivesSection),
					LibraryManager.UpdateLibrariesAsync(),
					OptionalTaskAsync(WSLDistroManager.UpdateDrivesAsync(), generalSettingsService.ShowWslSection),
					OptionalTaskAsync(FileTagsManager.UpdateFileTagsAsync(), generalSettingsService.ShowFileTagsSection),
					QuickAccessManager.InitializeAsync()
				);

				await Task.WhenAll(
					JumpListHelper.InitializeUpdatesAsync(),
					addItemService.InitializeAsync(),
					ContextMenu.WarmUpQueryContextMenuAsync()
				);

				FileTagsHelper.UpdateTagsDb();
			});

			await CheckForRequiredUpdatesAsync();

			static async Task OptionalTaskAsync(Task task, bool condition)
			{
				if (condition)
					await task;
			}
		}

		private static async Task CheckForRequiredUpdatesAsync()
		{
			var updateService = Ioc.Default.GetRequiredService<IUpdateService>();
			await updateService.CheckForUpdatesAsync();
			await updateService.DownloadMandatoryUpdatesAsync();
			await updateService.CheckAndUpdateFilesLauncherAsync();
			await updateService.CheckLatestReleaseNotesAsync();
		}

		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used such as when the application is launched to open a specific file.
		/// </summary>
		/// <param name="e">Details about the launch request and process.</param>
		protected override void OnLaunched(LaunchActivatedEventArgs e)
		{
			_ = ActivateAsync();

			async Task ActivateAsync()
			{
				// Initialize and activate MainWindow
				EnsureSuperEarlyWindow();

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
				_host = ConfigureHost();
				Ioc.Default.ConfigureServices(_host.Services);

				EnsureSettingsAndConfigurationAreBootstrapped();

				// TODO: Remove App.Logger instance and replace with DI
				Logger = Ioc.Default.GetRequiredService<ILogger<App>>();
				Logger.LogInformation($"App launched. Launch args type: {appActivationArguments.Data.GetType().Name}");

				// Wait for the UI to update
				await SplashScreenLoadingTCS!.Task.WithTimeoutAsync(TimeSpan.FromMilliseconds(500));
				SplashScreenLoadingTCS = null;

				_ = InitializeAppComponentsAsync().ContinueWith(t => Logger.LogWarning(t.Exception, "Error during InitializeAppComponentsAsync()"), TaskContinuationOptions.OnlyOnFaulted);

				_ = MainWindow.Instance.InitializeApplicationAsync(appActivationArguments.Data);
			}
		}

		private static void EnsureSettingsAndConfigurationAreBootstrapped()
		{
			// TODO(s): Remove initialization here and move out all classes (visible below) out of App.xaml.cs

			RecentItemsManager ??= new RecentItems();
			AppModel ??= new AppModel();
			LibraryManager ??= new LibraryManager();
			CloudDrivesManager ??= new CloudDrivesManager();
			WSLDistroManager ??= new WSLDistroManager();
			FileTagsManager ??= new FileTagsManager();
			QuickAccessManager ??= new QuickAccessManager();
		}

		private void EnsureSuperEarlyWindow()
		{
			// Get the MainWindow instance
			var window = MainWindow.Instance;

			// Hook events for the window
			window.Activated += Window_Activated;
			window.Closed += Window_ClosedAsync;

			// Attempt to activate it
			window.Activate();
		}

		private void Window_Activated(object sender, WindowActivatedEventArgs args)
		{
			// TODO(s): Is this code still needed?
			if (args.WindowActivationState is not (WindowActivationState.CodeActivated or WindowActivationState.PointerActivated))
				return;

			ShowErrorNotification = true;
			ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = -Process.GetCurrentProcess().Id;
		}

		public void OnActivated(AppActivationArguments activatedEventArgs)
		{
			Logger.LogInformation($"App activated. Activated args type: {activatedEventArgs.Data.GetType().Name}");
			var data = activatedEventArgs.Data;

			// InitializeApplication accesses UI, needs to be called on UI thread
			_ = MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => MainWindow.Instance.InitializeApplicationAsync(data));
		}

		/// <summary>
		/// Invoked when application execution is being closed. Save application state.
		/// </summary>
		/// <param name="sender">The source of the suspend request.</param>
		/// <param name="args">Details about the suspend request.</param>
		private async void Window_ClosedAsync(object sender, WindowEventArgs args)
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
				!AppModel.ForceProcessTermination &&
				!Process.GetProcessesByName("Files").Any(x => x.Id != Process.GetCurrentProcess().Id))
			{
				// Close open content dialogs
				UIHelpers.CloseAllDialogs();
				
				// Close all notification banners except in progress
				Ioc.Default.GetRequiredService<StatusCenterViewModel>().RemoveAllCompletedItems();

				// Cache the window instead of closing it
				MainWindow.Instance.AppWindow.Hide();
				args.Handled = true;

				// Save and close all tabs
				SaveSessionTabs();
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

					_ = CheckForRequiredUpdatesAsync();
				}

				return;
			}

			// Method can take a long time, make sure the window is hidden
			await Task.Yield();

			SaveSessionTabs();

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

		/// <summary>
		/// Enumerates through all tabs and gets the Path property and saves it to AppSettings.LastSessionPages.
		/// </summary>
		public static void SaveSessionTabs()
		{
			IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			userSettingsService.GeneralSettingsService.LastSessionTabList = MainPageViewModel.AppInstances.DefaultIfEmpty().Select(tab =>
			{
				if (tab is not null && tab.NavigationParameter is not null)
				{
					return tab.NavigationParameter.Serialize();
				}
				else
				{
					var defaultArg = new CustomTabViewItemParameter() { InitialPageType = typeof(PaneHolderPage), NavigationParameter = "Home" };
					return defaultArg.Serialize();
				}
			})
			.ToList();
		}

		#region Exception Handlers

		/// <summary>
		/// Occurs when an exception is not handled on the UI thread.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
			=> AppUnhandledException(e.Exception, true);

		private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
			=> AppUnhandledException(e.ExceptionObject as Exception, false);

		/// <summary>
		/// Occurs when an exception is not handled on a background thread.
		/// i.e. A task is fired and forgotten Task.Run(() => {...})
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
			=> AppUnhandledException(e.Exception, false);

		private static void AppUnhandledException(Exception? ex, bool shouldShowNotification)
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

			// Please check "Output Window" for exception details (View -> Output Window) (CTRL + ALT + O)
			Debugger.Break();

			SaveSessionTabs();
			App.Logger.LogError(ex, ex.Message);

			if (!ShowErrorNotification || !shouldShowNotification)
				return;

			var toastContent = new ToastContent()
			{
				Visual = new()
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
						AppLogoOverride = new()
						{
							Source = "ms-appx:///Assets/error.png"
						}
					}
				},
				Actions = new ToastActionsCustom()
				{
					Buttons =
					{
						new ToastButton("ExceptionNotificationReportButton".GetLocalizedResource(), Constants.GitHub.BugReportUrl)
						{
							ActivationType = ToastActivationType.Protocol
						}
					}
				},
				ActivationType = ToastActivationType.Protocol
			};

			// Create the toast notification
			var toastNotif = new ToastNotification(toastContent.GetXml());

			// And send the notification
			ToastNotificationManager.CreateToastNotifier().Show(toastNotif);

			// Restart the app
			var userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			var lastSessionTabList = userSettingsService.GeneralSettingsService.LastSessionTabList;

			if (userSettingsService.GeneralSettingsService.LastCrashedTabList?.SequenceEqual(lastSessionTabList) ?? false)
			{
				// Avoid infinite restart loop
				userSettingsService.GeneralSettingsService.LastSessionTabList = null;
			}
			else
			{
				userSettingsService.AppSettingsService.RestoreTabsOnStartup = true;
				userSettingsService.GeneralSettingsService.LastCrashedTabList = lastSessionTabList;

				MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					await Launcher.LaunchUriAsync(new Uri("files-uwp:"));
				}).Wait(1000);
			}
			Process.GetCurrentProcess().Kill();
		}

		#endregion
	}
}
