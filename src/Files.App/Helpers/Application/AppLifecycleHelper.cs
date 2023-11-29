// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Notifications;
using Files.App.Services.DateTimeFormatter;
using Files.App.Services.Settings;
using Files.App.Storage.FtpStorage;
using Files.App.Storage.NativeStorage;
using Files.App.ViewModels.Settings;
using Files.Core.Services.SizeProvider;
using Files.Core.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using Windows.Storage;
using Windows.System;
using Windows.UI.Notifications;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper to manage app lifecycle.
	/// </summary>
	public static class AppLifecycleHelper
	{
		/// <summary>
		/// Initializes the app components.
		/// </summary>
		public static async Task InitializeAppComponentsAsync()
		{
			var userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			var addItemService = Ioc.Default.GetRequiredService<IAddItemService>();
			var generalSettingsService = userSettingsService.GeneralSettingsService;

			// Start off a list of tasks we need to run before we can continue startup
			await Task.WhenAll(
				OptionalTaskAsync(CloudDrivesManager.UpdateDrivesAsync(), generalSettingsService.ShowCloudDrivesSection),
				App.LibraryManager.UpdateLibrariesAsync(),
				OptionalTaskAsync(WSLDistroManager.UpdateDrivesAsync(), generalSettingsService.ShowWslSection),
				OptionalTaskAsync(App.FileTagsManager.UpdateFileTagsAsync(), generalSettingsService.ShowFileTagsSection),
				App.QuickAccessManager.InitializeAsync()
			);

			await Task.WhenAll(
				JumpListHelper.InitializeUpdatesAsync(),
				addItemService.InitializeAsync(),
				ContextMenu.WarmUpQueryContextMenuAsync()
			);

			FileTagsHelper.UpdateTagsDb();

			await CheckAppUpdate();

			static Task OptionalTaskAsync(Task task, bool condition)
			{
				if (condition)
					return task;

				return Task.CompletedTask;
			}
		}

		/// <summary>
		/// Checks application updates and download if available.
		/// </summary>
		public static async Task CheckAppUpdate()
		{
			var updateService = Ioc.Default.GetRequiredService<IUpdateService>();

			await updateService.CheckForUpdatesAsync();
			await updateService.DownloadMandatoryUpdatesAsync();
			await updateService.CheckAndUpdateFilesLauncherAsync();
			await updateService.CheckLatestReleaseNotesAsync();
		}

		/// <summary>
		/// Configures AppCenter service, such as Analytics and Crash Report.
		/// </summary>
		public static void ConfigureAppCenter()
		{
			try
			{
				if (!Microsoft.AppCenter.AppCenter.Configured)
				{
					Microsoft.AppCenter.AppCenter.Start(
						Constants.AutomatedWorkflowInjectionKeys.AppCenterSecret,
						typeof(Microsoft.AppCenter.Analytics.Analytics),
						typeof(Microsoft.AppCenter.Crashes.Crashes));
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Failed to start AppCenter service.");
			}
		}

		/// <summary>
		/// Configures DI (dependency injection) container.
		/// </summary>
		public static IHost ConfigureHost()
		{
			return Host.CreateDefaultBuilder()
				.UseEnvironment(ApplicationService.AppEnvironment.ToString())
				.ConfigureLogging(builder => builder
					.AddProvider(new FileLoggerProvider(Path.Combine(ApplicationData.Current.LocalFolder.Path, "debug.log")))
					.SetMinimumLevel(LogLevel.Information))
				.ConfigureServices(services => services
					// Settings services
					.AddSingleton<IUserSettingsService, UserSettingsService>()
					.AddSingleton<IAppearanceSettingsService, AppearanceSettingsService>(sp => new AppearanceSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IGeneralSettingsService, GeneralSettingsService>(sp => new GeneralSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IFoldersSettingsService, FoldersSettingsService>(sp => new FoldersSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IApplicationSettingsService, ApplicationSettingsService>(sp => new ApplicationSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IInfoPaneSettingsService, InfoPaneSettingsService>(sp => new InfoPaneSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<ILayoutSettingsService, LayoutSettingsService>(sp => new LayoutSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IAppSettingsService, AppSettingsService>(sp => new AppSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IFileTagsSettingsService, FileTagsSettingsService>()
					// Contexts
					.AddSingleton<IPageContext, PageContext>()
					.AddSingleton<IContentPageContext, ContentPageContext>()
					.AddSingleton<IDisplayPageContext, DisplayPageContext>()
					.AddSingleton<IWindowContext, WindowContext>()
					.AddSingleton<IMultitaskingContext, MultitaskingContext>()
					.AddSingleton<ITagsContext, TagsContext>()
					// Services
					.AddSingleton<IDialogService, DialogService>()
					.AddSingleton<IImageService, ImagingService>()
					.AddSingleton<IThreadingService, ThreadingService>()
					.AddSingleton<ILocalizationService, LocalizationService>()
					.AddSingleton<ICloudDetector, CloudDetector>()
					.AddSingleton<IFileTagsService, FileTagsService>()
					.AddSingleton<ICommandManager, CommandManager>()
					.AddSingleton<IModifiableCommandManager, ModifiableCommandManager>()
					.AddSingleton<IApplicationService, ApplicationService>()
					.AddSingleton<IStorageService, NativeStorageService>()
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
					.AddSingleton<IStartMenuService, StartMenuService>()
					// ViewModels
					.AddSingleton<MainPageViewModel>()
					.AddSingleton<InfoPaneViewModel>()
					.AddSingleton<SidebarViewModel>()
					.AddSingleton<SettingsViewModel>()
					.AddSingleton<DrivesViewModel>()
					.AddSingleton<NetworkDrivesViewModel>()
					.AddSingleton<StatusCenterViewModel>()
					.AddSingleton<AppearanceViewModel>()
					.AddTransient<HomeViewModel>()
					// Utilities
					.AddSingleton<QuickAccessManager>()
					.AddSingleton<StorageHistoryWrapper>()
					.AddSingleton<FileTagsManager>()
					.AddSingleton<RecentItems>()
					.AddSingleton<LibraryManager>()
					.AddSingleton<AppModel>()
				).Build();
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
					var defaultArg = new CustomTabViewItemParameter()
					{
						InitialPageType = typeof(PaneHolderPage),
						NavigationParameter = "Home"
					};

					return defaultArg.Serialize();
				}
			})
			.ToList();
		}

		/// <summary>
		/// Shows exception on the Debug Output and sends Toast Notification to the Windows Notification Center.
		/// </summary>
		public static void HandleAppUnhandledException(Exception? ex, bool showToastNotification)
		{
			StringBuilder formattedException = new()
			{
				Capacity = 200
			};

			formattedException.AppendLine("--------- UNHANDLED EXCEPTION ---------");

			if (ex is not null)
			{
				formattedException.AppendLine($">>>> HRESULT: {ex.HResult}");

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

			SaveSessionTabs();
			App.Logger.LogError(ex, ex?.Message ?? "An unhandled error occurred.");

			if (!showToastNotification)
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
			var toastNotification = new ToastNotification(toastContent.GetXml());

			// And send the notification
			ToastNotificationManager.CreateToastNotifier().Show(toastNotification);

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

				// Try to re-launch and start over
				MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					await Launcher.LaunchUriAsync(new Uri("files-uwp:"));
				})
				.Wait(1000);
			}
			Process.GetCurrentProcess().Kill();
		}
	}
}
