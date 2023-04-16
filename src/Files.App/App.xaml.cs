using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Helpers;
using CommunityToolkit.WinUI.Notifications;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.DataModels;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.Cloud;
using Files.App.Filesystem.FilesystemHistory;
using Files.App.Helpers;
using Files.App.ServicesImplementation;
using Files.App.ServicesImplementation.DateTimeFormatter;
using Files.App.ServicesImplementation.Settings;
using Files.App.Shell;
using Files.App.Storage.NativeStorage;
using Files.App.UserControls.MultitaskingControl;
using Files.App.ViewModels;
using Files.App.ViewModels.Settings;
using Files.App.Views;
using Files.Backend.Services;
using Files.Backend.Services.Settings;
using Files.Backend.Services.SizeProvider;
using Files.Sdk.Storage;
using Files.Shared;
using Files.Shared.Cloud;
using Files.Shared.Extensions;
using Files.Shared.Services;
using Files.Shared.Services.DateTimeFormatter;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Notifications;

namespace Files.App
{
	public partial class App : Application
	{
		private static bool ShowErrorNotification = false;
		private IHost host { get; set; }
		public static string OutputPath { get; set; }
		public static CommandBarFlyout? LastOpenedFlyout { get; set; }
		public static StorageHistoryWrapper HistoryWrapper = new StorageHistoryWrapper();
		public static AppModel AppModel { get; private set; }
		public static RecentItems RecentItemsManager { get; private set; }
		public static QuickAccessManager QuickAccessManager { get; private set; }
		public static CloudDrivesManager CloudDrivesManager { get; private set; }
		public static NetworkDrivesManager NetworkDrivesManager { get; private set; }
		public static DrivesManager DrivesManager { get; private set; }
		public static WSLDistroManager WSLDistroManager { get; private set; }
		public static LibraryManager LibraryManager { get; private set; }
		public static FileTagsManager FileTagsManager { get; private set; }

		public static ILogger Logger { get; private set; }
		public static SecondaryTileHelper SecondaryTileHelper { get; private set; } = new SecondaryTileHelper();

		public static string AppVersion = $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}";
		public static string LogoPath;

		public IServiceProvider Services { get; private set; }

		/// <summary>
		/// Initializes the singleton application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			UnhandledException += OnUnhandledException;
			TaskScheduler.UnobservedTaskException += OnUnobservedException;
			InitializeComponent();
			LogoPath = Package.Current.DisplayName == "Files - Dev" ? Constants.AssetPaths.DevLogo
					: (Package.Current.DisplayName == "Files (Preview)" ? Constants.AssetPaths.PreviewLogo : Constants.AssetPaths.StableLogo);
		}

		private static void EnsureSettingsAndConfigurationAreBootstrapped()
		{
			RecentItemsManager ??= new RecentItems();
			AppModel ??= new AppModel();
			LibraryManager ??= new LibraryManager();
			DrivesManager ??= new DrivesManager();
			NetworkDrivesManager ??= new NetworkDrivesManager();
			CloudDrivesManager ??= new CloudDrivesManager();
			WSLDistroManager ??= new WSLDistroManager();
			FileTagsManager ??= new FileTagsManager();
			QuickAccessManager ??= new QuickAccessManager();
		}

		private static Task StartAppCenter()
		{
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

			return Task.CompletedTask;
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
					StartAppCenter(),
					DrivesManager.UpdateDrivesAsync(),
					OptionalTask(CloudDrivesManager.UpdateDrivesAsync(), generalSettingsService.ShowCloudDrivesSection),
					LibraryManager.UpdateLibrariesAsync(),
					OptionalTask(NetworkDrivesManager.UpdateDrivesAsync(), generalSettingsService.ShowNetworkDrivesSection),
					OptionalTask(WSLDistroManager.UpdateDrivesAsync(), generalSettingsService.ShowWslSection),
					OptionalTask(FileTagsManager.UpdateFileTagsAsync(), generalSettingsService.ShowFileTagsSection),
					QuickAccessManager.InitializeAsync()
				);

				await Task.WhenAll(
					JumpListHelper.InitializeUpdatesAsync(),
					addItemService.GetNewEntriesAsync(),
					ContextMenu.WarmUpQueryContextMenuAsync()
				);

				FileTagsHelper.UpdateTagsDb();
			});

			// Check for required updates
			var updateService = Ioc.Default.GetRequiredService<IUpdateService>();
			await updateService.CheckForUpdates();
			await updateService.DownloadMandatoryUpdates();
			await updateService.CheckAndUpdateFilesLauncherAsync();
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

			var logPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "debug.log");
			//start tracking app usage
			if (activatedEventArgs.Data is Windows.ApplicationModel.Activation.IActivatedEventArgs iaea)
				SystemInformation.Instance.TrackAppUse(iaea);

			// Initialize MainWindow here
			EnsureWindowIsInitialized();
			host = Host.CreateDefaultBuilder()
				.ConfigureLogging(builder => 
					builder
					.AddProvider(new FileLoggerProvider(logPath))
					.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information)
				)
				.ConfigureServices(services => 
					services
						.AddSingleton<IUserSettingsService, UserSettingsService>()
						.AddSingleton<IAppearanceSettingsService, AppearanceSettingsService>((sp) => new AppearanceSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
						.AddSingleton<IGeneralSettingsService, GeneralSettingsService>((sp) => new GeneralSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
						.AddSingleton<IFoldersSettingsService, FoldersSettingsService>((sp) => new FoldersSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
						.AddSingleton<IApplicationSettingsService, ApplicationSettingsService>((sp) => new ApplicationSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
						.AddSingleton<IPreviewPaneSettingsService, PreviewPaneSettingsService>((sp) => new PreviewPaneSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
						.AddSingleton<ILayoutSettingsService, LayoutSettingsService>((sp) => new LayoutSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
						.AddSingleton<IAppSettingsService, AppSettingsService>((sp) => new AppSettingsService((sp.GetService<IUserSettingsService>() as UserSettingsService).GetSharingContext()))
						.AddSingleton<IFileTagsSettingsService, FileTagsSettingsService>()
						.AddSingleton<IBundlesSettingsService, BundlesSettingsService>()
						.AddSingleton<IPageContext, PageContext>()
						.AddSingleton<IContentPageContext, ContentPageContext>()
						.AddSingleton<IDisplayPageContext, DisplayPageContext>()
						.AddSingleton<IWindowContext, WindowContext>()
						.AddSingleton<IMultitaskingContext, MultitaskingContext>()
						.AddSingleton<IDialogService, DialogService>()
						.AddSingleton<IImageService, ImagingService>()
						.AddSingleton<IThreadingService, ThreadingService>()
						.AddSingleton<ILocalizationService, LocalizationService>()
						.AddSingleton<ICloudDetector, CloudDetector>()
						.AddSingleton<IFileTagsService, FileTagsService>()
						.AddSingleton<ICommandManager, CommandManager>()
#if UWP
						.AddSingleton<IStorageService, WindowsStorageService>()
#else
						.AddSingleton<IStorageService, NativeStorageService>()
#endif
						.AddSingleton<IAddItemService, AddItemService>()
#if SIDELOAD
						.AddSingleton<IUpdateService, SideloadUpdateService>()
#else
						.AddSingleton<IUpdateService, UpdateService>()
#endif
						.AddSingleton<IDateTimeFormatterFactory, DateTimeFormatterFactory>()
						.AddSingleton<IDateTimeFormatter, UserDateTimeFormatter>()
						.AddSingleton<IVolumeInfoFactory, VolumeInfoFactory>()
						.AddSingleton<ISizeProvider, UserSizeProvider>()
						.AddSingleton<IQuickAccessService, QuickAccessService>()
						.AddSingleton<IResourcesService, ResourcesService>()
						.AddSingleton<IJumpListService, JumpListService>()
						.AddSingleton<MainPageViewModel>()
						.AddSingleton<PreviewPaneViewModel>()
						.AddSingleton<SidebarViewModel>()
						.AddSingleton<SettingsViewModel>()
						.AddSingleton<OngoingTasksViewModel>()
						.AddSingleton<AppearanceViewModel>()
				)
				.Build();

			Logger = host.Services.GetRequiredService<ILogger<App>>();
			App.Logger.LogInformation($"App launched. Launch args type: {activatedEventArgs.Data.GetType().Name}");

			Ioc.Default.ConfigureServices(host.Services);

			EnsureSettingsAndConfigurationAreBootstrapped();

			_ = InitializeAppComponentsAsync().ContinueWith(t => Logger.LogWarning(t.Exception, "Error during InitializeAppComponentsAsync()"), TaskContinuationOptions.OnlyOnFaulted);

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
			App.Logger.LogInformation($"App activated. Activated args type: {activatedEventArgs.Data.GetType().Name}");
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

			// A Workaround for the crash (#10110)
			if (LastOpenedFlyout?.IsOpen ?? false)
			{
				args.Handled = true;
				LastOpenedFlyout.Closed += (sender, e) => App.Current.Exit();
				LastOpenedFlyout.Hide();
				return;
			}

			// Method can take a long time, make sure the window is hidden
			await Task.Yield();

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
				},
				Logger);
			}

			DrivesManager?.Dispose();

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

			// Wait for ongoing file operations
			FileOperationsHelpers.WaitForCompletion();
		}

		/// <summary>
		/// Enumerates through all tabs and gets the Path property and saves it to AppSettings.LastSessionPages.
		/// </summary>
		public static void SaveSessionTabs() 
		{
			IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			IBundlesSettingsService bundlesSettingsService = Ioc.Default.GetRequiredService<IBundlesSettingsService>();

			bundlesSettingsService.FlushSettings();

			userSettingsService.GeneralSettingsService.LastSessionTabList = MainPageViewModel.AppInstances.DefaultIfEmpty().Select(tab =>
			{
				if (tab is not null && tab.TabItemArguments is not null)
				{
					return tab.TabItemArguments.Serialize();
				}
				else
				{
					var defaultArg = new TabItemArguments() { InitialPageType = typeof(PaneHolderPage), NavigationArg = "Home" };
					return defaultArg.Serialize();
				}
			})
			.ToList();
		}

		/// <summary>
		/// Occurs when an exception is not handled on the UI thread.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
			=> AppUnhandledException(e.Exception, true);

		/// <summary>
		/// Occurs when an exception is not handled on a background thread.
		/// i.e. A task is fired and forgotten Task.Run(() => {...})
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void OnUnobservedException(object sender, UnobservedTaskExceptionEventArgs e)
			=> AppUnhandledException(e.Exception, false);

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
			=> Window.Close();

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
