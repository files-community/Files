// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Helpers.Application;
using Files.App.Services.Git;
using Files.App.Services.SizeProvider;
using Files.App.Utils.Logger;
using Files.App.ViewModels.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Sentry;
using Sentry.Protocol;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.System;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper to manage app lifecycle.
	/// </summary>
	public static class AppLifecycleHelper
	{
		private readonly static string AppInformationKey = @$"Software\Files Community\{Package.Current.Id.Name}\v1\AppInformation";

		/// <summary>
		/// Gets the value that indicates whether the app is updated.
		/// </summary>
		public static bool IsAppUpdated { get; }

		/// <summary>
		/// Gets the value that indicates whether the app is running for the first time.
		/// </summary>
		public static bool IsFirstRun { get; }

		/// <summary>
		/// Gets the value that indicates the total launch count of the app.
		/// </summary>
		public static long TotalLaunchCount { get; }

		/// <summary>
		/// Gets the value that indicates if the release notes tab was automatically opened.
		/// </summary>
		private static bool ViewedReleaseNotes { get; set; } = false;

		static AppLifecycleHelper()
		{
			using var infoKey = Registry.CurrentUser.CreateSubKey(AppInformationKey);
			var version = infoKey.GetValue("LastLaunchVersion");
			var launchCount = infoKey.GetValue("TotalLaunchCount");
			if (version is null)
			{
				IsAppUpdated = true;
				IsFirstRun = true;
			}
			else
			{
				IsAppUpdated = version.ToString() != AppVersion.ToString();
			}

			TotalLaunchCount = long.TryParse(launchCount?.ToString(), out var v) ? v + 1 : 1;
			infoKey.SetValue("LastLaunchVersion", AppVersion.ToString());
			infoKey.SetValue("TotalLaunchCount", TotalLaunchCount);
		}

		/// <summary>
		/// Gets the value that provides application environment or branch name.
		/// </summary>
		public static AppEnvironment AppEnvironment =>
			Enum.TryParse("cd_app_env_placeholder", true, out AppEnvironment appEnvironment)
				? appEnvironment
				: AppEnvironment.Dev;


		/// <summary>
		/// Gets application package version.
		/// </summary>
		public static Version AppVersion { get; } =
			new(Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);

		/// <summary>
		/// Gets application icon path.
		/// </summary>
		public static string AppIconPath { get; } =
			SystemIO.Path.Combine(Package.Current.InstalledLocation.Path, AppEnvironment switch
			{
				AppEnvironment.Dev => Constants.AssetPaths.DevLogo,
				AppEnvironment.SideloadPreview or AppEnvironment.StorePreview => Constants.AssetPaths.PreviewLogo,
				_ => Constants.AssetPaths.StableLogo
			});

		/// <summary>
		/// Initializes the app components.
		/// </summary>
		public static async Task InitializeAppComponentsAsync()
		{
			var userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			var addItemService = Ioc.Default.GetRequiredService<IAddItemService>();
			var generalSettingsService = userSettingsService.GeneralSettingsService;
			var jumpListService = Ioc.Default.GetRequiredService<IWindowsJumpListService>();

			ActiveSessionTracker.ReportPersistedTime();

			// Start non-critical tasks without waiting; pinned loads alongside the others so its shell enumeration doesn't block them.
			_ = Task.Run(async () =>
			{
				await Task.WhenAll(
					App.QuickAccessManager.InitializeAsync(),
					OptionalTaskAsync(CloudDrivesManager.UpdateDrivesAsync(), generalSettingsService.ShowCloudDrivesSection),
					App.LibraryManager.UpdateLibrariesAsync(),
					OptionalTaskAsync(WSLDistroManager.UpdateDrivesAsync(), generalSettingsService.ShowWslSection),
					OptionalTaskAsync(App.FileTagsManager.UpdateFileTagsAsync(), generalSettingsService.ShowFileTagsSection),
					jumpListService.InitializeAsync()
				);

				//Start the tasks separately to reduce resource contention
				await Task.WhenAll(
					addItemService.InitializeAsync(),
					ContextMenu.WarmUpQueryContextMenuAsync()
				);
			});

			_ = Task.Run(FileTagsHelper.UpdateTagsDb);

			_ = Task.Run(async () =>
			{
				// The follwing method invokes UI thread, so we run it in a separate task
				await CheckAppUpdate();

				await PeriodicallyCheckForUpdatesAsync();
			});

			static Task OptionalTaskAsync(Task task, bool condition)
			{
				if (condition)
					return task;

				return Task.CompletedTask;
			}

			generalSettingsService.PropertyChanged += GeneralSettingsService_PropertyChanged;
		}

		/// <summary>
		/// Checks application updates and download if available.
		/// </summary>
		public static async Task CheckAppUpdate()
		{
			var updateService = Ioc.Default.GetRequiredService<IUpdateService>();

			await updateService.CheckForReleaseNotesAsync();

			// Check for release notes before checking for new updates
			if (AppEnvironment != AppEnvironment.Dev &&
				IsAppUpdated &&
				updateService.AreReleaseNotesAvailable &&
				!ViewedReleaseNotes)
			{
				await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					await Ioc.Default.GetRequiredService<ICommandManager>().OpenReleaseNotes.ExecuteAsync();
					ViewedReleaseNotes = true;
				});
			}

			await updateService.CheckForUpdatesAsync();
			await updateService.DownloadMandatoryUpdatesAsync();

			if (IsAppUpdated)
				await updateService.CheckAndUpdateFilesLauncherAsync();
		}

		/// <summary>
		/// Periodically re-checks for updates while the app keeps running.
		/// </summary>
		public static async Task PeriodicallyCheckForUpdatesAsync()
		{
			var updateService = Ioc.Default.GetRequiredService<IUpdateService>();

			var interval = AppEnvironment is AppEnvironment.SideloadPreview or AppEnvironment.StorePreview
				? TimeSpan.FromHours(2)
				: TimeSpan.FromHours(5);

			using var timer = new PeriodicTimer(interval);
			while (await timer.WaitForNextTickAsync())
			{
				if (updateService.IsUpdateAvailable)
					break;

				// CheckForUpdatesAsync resets IsUpdateAvailable, so skip while a download is in progress
				if (updateService.IsUpdating)
					continue;

				await updateService.CheckForUpdatesAsync();
			}
		}

		/// <summary>
		/// Configures Sentry service, such as Analytics and Crash Report.
		/// </summary>
		public static void ConfigureSentry()
		{
			SentrySdk.Init(options =>
			{
				options.Dsn = Constants.AutomatedWorkflowInjectionKeys.SentrySecret;
				options.AutoSessionTracking = true;
				var packageVersion = Package.Current.Id.Version;
				options.Release = $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}";
				options.TracesSampleRate = 0.10;
				// Active-session reports must not be sampled away or their sums undercount;
				// returning null falls back to TracesSampleRate for everything else
				options.TracesSampler = context =>
					context.TransactionContext.Operation == ActiveSessionTracker.TransactionOperation ? 1.0 : null;
				options.ProfilesSampleRate = 0.05;
				options.Environment = AppEnvironment == AppEnvironment.StorePreview || AppEnvironment == AppEnvironment.SideloadPreview ? "preview" : "production";
				options.CacheDirectoryPath = ApplicationData.Current.LocalFolder.Path;

				options.DisableWinUiUnhandledExceptionIntegration();

				options.SetBeforeSend(sentryEvent =>
				{
					if (sentryEvent.Message is { } message)
					{
						message.Message = SanitizeSentryText(message.Message);
						message.Formatted = SanitizeSentryText(message.Formatted);
					}

					if (sentryEvent.SentryExceptions is { } sentryExceptions)
					{
						foreach (var sentryException in sentryExceptions)
						{
							sentryException.Value = SanitizeSentryText(sentryException.Value);

							if (sentryException.Stacktrace?.Frames is { } frames)
							{
								foreach (var frame in frames)
								{
									frame.FileName = LogPathHelper.RedactUserName(frame.FileName);
									frame.AbsolutePath = LogPathHelper.RedactUserName(frame.AbsolutePath);
								}
							}
						}
					}

					foreach (var key in sentryEvent.Extra.Keys.ToList())
					{
						if (sentryEvent.Extra[key] is string text)
							sentryEvent.SetExtra(key, SanitizeSentryText(text) ?? string.Empty);
					}

					return sentryEvent;
				});

				options.SetBeforeBreadcrumb(breadcrumb =>
				{
					var message = SanitizeSentryText(breadcrumb.Message);

					Dictionary<string, string>? sanitizedData = null;
					if (breadcrumb.Data is { } data)
					{
						foreach (var (key, value) in data)
						{
							var sanitizedValue = SanitizeSentryText(value);
							if (sanitizedValue != value)
							{
								sanitizedData ??= new(data);
								sanitizedData[key] = sanitizedValue ?? string.Empty;
							}
						}
					}

					if (message == breadcrumb.Message && sanitizedData is null)
						return breadcrumb;

					return new Breadcrumb(message!, breadcrumb.Type!, sanitizedData ?? breadcrumb.Data, breadcrumb.Category, breadcrumb.Level);
				});
			});
		}

		/// <summary>
		/// Scrubs user names and file system paths from text before it is attached to a Sentry event.
		/// </summary>
		private static string? SanitizeSentryText(string? text)
		{
			return text is null ? null : LogPathHelper.SanitizeMessage(text);
		}

		/// <summary>
		/// Configures DI (dependency injection) container.
		/// </summary>
		/// <param name="appModel">Constructed on the UI thread by the caller (its ctor is UI-thread-only).</param>
		public static IServiceProvider ConfigureHost(AppModel appModel)
		{
			var services = new ServiceCollection();
			var fileLoggerProvider = new FileLoggerProvider(Path.Combine(ApplicationData.Current.LocalFolder.Path, "debug.log"));

			services.AddSingleton(fileLoggerProvider);

			services.AddLogging(builder => builder
					.AddDebug()
					.AddProvider(fileLoggerProvider)
					.AddProvider(new SentryLoggerProvider())
					.SetMinimumLevel(LogLevel.Information));

			services
					// Settings services
					.AddSingleton<IUserSettingsService, UserSettingsService>()
					.AddSingleton<IAppearanceSettingsService, AppearanceSettingsService>(sp => new AppearanceSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IGeneralSettingsService, GeneralSettingsService>(sp => new GeneralSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IFoldersSettingsService, FoldersSettingsService>(sp => new FoldersSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IDevToolsSettingsService, DevToolsSettingsService>(sp => new DevToolsSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IApplicationSettingsService, ApplicationSettingsService>(sp => new ApplicationSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IInfoPaneSettingsService, InfoPaneSettingsService>(sp => new InfoPaneSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<ILayoutSettingsService, LayoutSettingsService>(sp => new LayoutSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IAppSettingsService, AppSettingsService>(sp => new AppSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IActionsSettingsService, ActionsSettingsService>(sp => new ActionsSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IFileTagsSettingsService, FileTagsSettingsService>()
					// Contexts
					.AddSingleton<IMultiPanesContext, MultiPanesContext>()
					.AddSingleton<IContentPageContext, ContentPageContext>()
					.AddSingleton<IDisplayPageContext, DisplayPageContext>()
					.AddSingleton<IHomePageContext, HomePageContext>()
					.AddSingleton<IWindowContext, WindowContext>()
					.AddSingleton<IMultitaskingContext, MultitaskingContext>()
					.AddSingleton<ITagsContext, TagsContext>()
					.AddSingleton<ISidebarContext, SidebarContext>()
					.AddSingleton<IShelfContext, ShelfContext>()
					// Services
					.AddSingleton<IWindowsRecentItemsService, WindowsRecentItemsService>()
					.AddSingleton<IWindowsIniService, WindowsIniService>()
					.AddSingleton<IWindowsWallpaperService, WindowsWallpaperService>()
					.AddSingleton<IWindowsSecurityService, WindowsSecurityService>()
					.AddSingleton<IAppThemeModeService, AppThemeModeService>()
					.AddSingleton<IDialogService, DialogService>()
					.AddSingleton<ICommonDialogService, CommonDialogService>()
					.AddSingleton<IImageService, ImagingService>()
					.AddSingleton<IThreadingService, ThreadingService>()
					.AddSingleton<ILocalizationService, LocalizationService>()
					.AddSingleton<ICloudDetector, CloudDetector>()
					.AddSingleton<IFileTagsService, FileTagsService>()
					.AddSingleton<ICommandManager, CommandManager>()
					.AddSingleton<IModifiableCommandManager, ModifiableCommandManager>()
					.AddSingleton<IStorageService, NativeStorageLegacyService>()
					.AddSingleton<IFtpStorageService, FtpStorageService>()
					.AddSingleton<IAddItemService, AddItemService>()
					.AddSingleton<IPreviewPopupService, PreviewPopupService>()
					.AddSingleton<IDateTimeFormatterFactory, DateTimeFormatterFactory>()
					.AddSingleton<IDateTimeFormatter, UserDateTimeFormatter>()
					.AddSingleton<ISizeProvider, UserSizeProvider>()
					.AddSingleton<IQuickAccessService, QuickAccessService>()
					.AddSingleton<IResourcesService, ResourcesService>()
					.AddSingleton<IWindowsJumpListService, WindowsJumpListService>()
					.AddSingleton<IStorageTrashBinService, StorageTrashBinService>()
					.AddSingleton<IRemovableDrivesService, RemovableDrivesService>()
					.AddSingleton<INetworkService, NetworkService>()
					.AddSingleton<IStartMenuService, StartMenuService>()
					.AddSingleton<IStorageCacheService, StorageCacheService>()
					.AddSingleton<IIconCacheService, IconCacheService>()
					.AddSingleton<IStorageArchiveService, StorageArchiveService>()
					.AddSingleton<IStorageSecurityService, StorageSecurityService>()
					.AddSingleton<IWindowsCompatibilityService, WindowsCompatibilityService>()
					.AddSingleton</*IVersionControlService,*/ LibGit2Service>()
					// ViewModels
					.AddSingleton<MainPageViewModel>()
					.AddSingleton<InfoPaneViewModel>()
					.AddSingleton<SidebarViewModel>()
					.AddSingleton<DrivesViewModel>()
					.AddSingleton<ShelfViewModel>()
					.AddSingleton<StatusCenterViewModel>()
					.AddSingleton<AppearanceViewModel>()
					.AddSingleton<ToolbarCustomizationViewModel>()
					.AddTransient<HomeViewModel>()
					.AddSingleton<QuickAccessWidgetViewModel>()
					.AddSingleton<DrivesWidgetViewModel>()
					.AddSingleton<NetworkLocationsWidgetViewModel>()
					.AddSingleton<FileTagsWidgetViewModel>()
					.AddSingleton<RecentFilesWidgetViewModel>()
					.AddSingleton<ReleaseNotesViewModel>()
					// Utilities
					.AddSingleton<QuickAccessManager>()
					.AddSingleton<StorageHistoryWrapper>()
					.AddSingleton<FileTagsManager>()
					.AddSingleton<LibraryManager>()
					.AddSingleton(appModel);

			// Conditional DI
			if (AppEnvironment is AppEnvironment.SideloadPreview or AppEnvironment.SideloadStable)
				services.AddSingleton<IUpdateService, SideloadUpdateService>();
			else if (AppEnvironment is AppEnvironment.StorePreview or AppEnvironment.StoreStable)
				services.AddSingleton<IUpdateService, StoreUpdateService>();
			else
				services.AddSingleton<IUpdateService, DummyUpdateService>();

			return services.BuildServiceProvider();
		}

		/// <summary>
		/// Saves saves all opened tabs to the app cache.
		/// </summary>
		public static void SaveSessionTabs()
		{
			var userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			userSettingsService.GeneralSettingsService.LastSessionTabList = MainPageViewModel.AppInstances.DefaultIfEmpty().Select(tab =>
			{
				if (tab is not null && tab.NavigationParameter is not null)
				{
					return tab.NavigationParameter.Serialize();
				}
				else
				{
					return "";
				}
			})
			.ToList();

			userSettingsService.GeneralSettingsService.LastSessionSelectedTabIndex = App.AppModel.TabStripSelectedIndex;
		}

		// XAML delivers Application.UnhandledException with the managed stack already stripped,
		// so recently thrown exceptions are buffered here to recover their stacks at crash time.
		private const int RecentExceptionsCapacity = 16;
		private static readonly Exception?[] _recentExceptions = new Exception?[RecentExceptionsCapacity];
		private static readonly string?[] _recentExceptionStacks = new string?[RecentExceptionsCapacity];
		private static int _recentExceptionsNext = -1;

		[ThreadStatic]
		private static bool _isRecordingException;

		/// <summary>
		/// Starts recording thrown exceptions into a fixed-size buffer included in crash reports.
		/// </summary>
		public static void RecordFirstChanceExceptions()
		{
			AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
			{
				// A throw inside this handler would raise FirstChanceException again on the same thread
				if (_isRecordingException)
					return;

				_isRecordingException = true;
				try
				{
					// Cancellations are routine app-wide and would evict the faults worth keeping
					if (e.Exception is OperationCanceledException)
						return;

					var slot = (uint)Interlocked.Increment(ref _recentExceptionsNext) % RecentExceptionsCapacity;
					_recentExceptions[slot] = e.Exception;

					// COM/SEH exceptions arrive at the crash handler with an empty StackTrace, so snapshot the live stack now.
					_recentExceptionStacks[slot] = e.Exception is COMException or SEHException ? Environment.StackTrace : null;
				}
				finally
				{
					_isRecordingException = false;
				}
			};
		}

		private static string FormatRecentExceptions()
		{
			StringBuilder builder = new();
			var next = Volatile.Read(ref _recentExceptionsNext);

			for (var i = Math.Max(0, next - RecentExceptionsCapacity + 1); i <= next; i++)
			{
				var slot = (uint)i % RecentExceptionsCapacity;
				if (_recentExceptions[slot] is not Exception recent)
					continue;

				var text = recent.ToString();
				builder.AppendLine(text[..Math.Min(text.Length, 1024)]);

				if (string.IsNullOrEmpty(recent.StackTrace) && _recentExceptionStacks[slot] is string capturedStack)
				{
					builder.AppendLine("-- captured at throw --");
					builder.AppendLine(capturedStack[..Math.Min(capturedStack.Length, 2048)]);
				}

				builder.AppendLine("----");
			}

			return builder.ToString();
		}

		/// <summary>
		/// Shows exception on the Debug Output and sends Toast Notification to the Windows Notification Center.
		/// </summary>
		public static void HandleAppUnhandledException(Exception? ex, bool showToastNotification, string mechanism = "Application.UnhandledException", string? unhandledMessage = null)
		{
			try
			{
				// IoC may not be configured yet if the exception happened during early startup
				var generalSettingsService = SafetyExtensions.IgnoreExceptions(Ioc.Default.GetService<IGeneralSettingsService>);

				StringBuilder formattedException = new()
				{
					Capacity = 200
				};

				formattedException.AppendLine("--------- UNHANDLED EXCEPTION ---------");

				if (ex is not null)
				{
					ex.Data[Mechanism.HandledKey] = false;
					ex.Data[Mechanism.MechanismKey] = mechanism;

					SafetyExtensions.IgnoreExceptions(() =>
					{
						SentrySdk.CaptureException(ex, scope =>
						{
							scope.User.Id = generalSettingsService?.UserId;
							scope.Level = SentryLevel.Fatal;
							scope.SetTag("hresult", $"0x{ex.HResult:X8}");

							if (!string.IsNullOrEmpty(unhandledMessage))
								scope.SetExtra("unhandled_message", unhandledMessage);

							// Exception.ToString of a buffered exception may run a throwing override
							if (string.IsNullOrEmpty(ex.StackTrace))
								scope.SetExtra("recent_exceptions", SafetyExtensions.IgnoreExceptions(FormatRecentExceptions));
						});
					});

					formattedException.AppendLine($">>>> HRESULT: {ex.HResult}");

					if (unhandledMessage is not null)
					{
						formattedException.AppendLine("--- UNHANDLED MESSAGE ---");
						formattedException.AppendLine(unhandledMessage);
					}

					if (ex.Message is not null)
					{
						formattedException.AppendLine("--- MESSAGE ---");
						formattedException.AppendLine(ex.Message);
					}
					if (ex.StackTrace is not null)
					{
						formattedException.AppendLine("--- STACKTRACE ---");
						formattedException.AppendLine(ex.StackTrace);
					}
					if (ex.Source is not null)
					{
						formattedException.AppendLine("--- SOURCE ---");
						formattedException.AppendLine(ex.Source);
					}
					if (ex.InnerException is not null)
					{
						formattedException.AppendLine("--- INNER ---");
						formattedException.AppendLine(ex.InnerException.ToString());
					}
				}
				else
				{
					formattedException.AppendLine("Exception data is not available.");
				}

				formattedException.AppendLine("---------------------------------------");

				Debug.WriteLine(formattedException.ToString());

				// Please check "Output Window" for exception details (View -> Output Window) (CTRL + ALT + O)
				Debugger.Break();

				// Save the current tab list in case it was overwriten by another instance
				SafetyExtensions.IgnoreExceptions(SaveSessionTabs);
				SafetyExtensions.IgnoreExceptions(() => App.Logger?.LogError(ex, ex?.Message ?? "An unhandled error occurred."));

				if (!showToastNotification)
					return;

				SafetyExtensions.IgnoreExceptions(AppToastNotificationHelper.ShowUnhandledExceptionToast);

				SafetyExtensions.IgnoreExceptions(() =>
				{
					// Restart the app
					var userSettingsService = Ioc.Default.GetService<IUserSettingsService>();
					if (userSettingsService is null)
						return;

					var lastSessionTabList = userSettingsService.GeneralSettingsService.LastSessionTabList;

					if (lastSessionTabList is null ||
						userSettingsService.GeneralSettingsService.LastCrashedTabList?.SequenceEqual(lastSessionTabList) is true)
					{
						// Avoid infinite restart loop
						userSettingsService.GeneralSettingsService.LastSessionTabList = null;
					}
					else
					{
						userSettingsService.AppSettingsService.RestoreTabsOnStartup = true;
						userSettingsService.GeneralSettingsService.LastCrashedTabList = lastSessionTabList;

						// Try to re-launch and start over
						MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
						{
							await Launcher.LaunchUriAsync(new Uri("files-dev:"));
						})
						.Wait(100);
					}
				});
			}
			catch
			{
				// Swallow any exception escaping the handler so it can't re-enter
				// Application.UnhandledException before the process terminates.
			}
			finally
			{
				Environment.Exit(ex?.HResult ?? 1);
			}
		}

		/// <summary>
		/// Updates the visibility of the system tray icon
		/// </summary>
		private static void GeneralSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is not IGeneralSettingsService generalSettingsService)
				return;

			if (e.PropertyName == nameof(IGeneralSettingsService.ShowSystemTrayIcon))
			{
				if (generalSettingsService.ShowSystemTrayIcon)
					App.SystemTrayIcon?.Show();
				else
					App.SystemTrayIcon?.Hide();
			}
		}
	}
}
