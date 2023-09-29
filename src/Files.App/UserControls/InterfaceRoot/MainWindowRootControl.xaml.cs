using Files.Shared.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Files.App.UserControls.InterfaceRoot
{
	public sealed partial class MainWindowRootControl : UserControl, IAsyncInitialize
	{
		public MainWindowRootControl()
		{
			InitializeComponent();
		}

		private void MainWindowRootControl_Loaded(object sender, RoutedEventArgs e)
		{
			_ = InitAsync();
		}

		/// <inheritdoc/>
		public async Task InitAsync(CancellationToken cancellationToken = default)
		{
			await CreateSplashScreenAsync();
			await InitializeAppComponentsAsync(cancellationToken);
			await CheckForRequiredUpdates(cancellationToken);
		}

		private async Task CreateSplashScreenAsync()
		{
			Root.Content = new SplashScreenPage();
			await Task.Delay(20);
		}

		private async Task InitializeAppComponentsAsync(CancellationToken cancellationToken)
		{
			var userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			var addItemService = Ioc.Default.GetRequiredService<IAddItemService>();
			var generalSettingsService = userSettingsService.GeneralSettingsService;

			// Start off a list of tasks we need to run before we can continue startup
			await Task.Run(async () =>
			{
				await Task.WhenAll(
					OptionalTask(CloudDrivesManager.UpdateDrivesAsync(), generalSettingsService.ShowCloudDrivesSection),
					App.LibraryManager.UpdateLibrariesAsync(),
					OptionalTask(WSLDistroManager.UpdateDrivesAsync(), generalSettingsService.ShowWslSection),
					OptionalTask(App.FileTagsManager.UpdateFileTagsAsync(), generalSettingsService.ShowFileTagsSection),
					App.QuickAccessManager.InitializeAsync()
				);

				await Task.WhenAll(
					JumpListHelper.InitializeUpdatesAsync(),
					addItemService.InitializeAsync(),
					ContextMenu.WarmUpQueryContextMenuAsync()
				);

				FileTagsHelper.UpdateTagsDb();
			}, cancellationToken);


			static async Task OptionalTask(Task task, bool condition)
			{
				if (condition)
					await task;
			}
		}

		private async Task CheckForRequiredUpdates(CancellationToken cancellationToken)
		{
			var updateService = Ioc.Default.GetRequiredService<IUpdateService>();

			await updateService.CheckForUpdates();
			await updateService.DownloadMandatoryUpdates();
			await updateService.CheckAndUpdateFilesLauncherAsync();
			await updateService.CheckLatestReleaseNotesAsync(cancellationToken);
		}
	}
}
