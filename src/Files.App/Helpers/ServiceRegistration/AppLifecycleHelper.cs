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
using Microsoft.UI.Xaml;
using System.IO;
using Windows.Storage;

namespace Files.App.Helpers.ServiceRegistration
{
	public class AppLifecycleHelper
	{
		public IHost ConfigureHost()
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
				)
				.Build();
		}
	}
}






























