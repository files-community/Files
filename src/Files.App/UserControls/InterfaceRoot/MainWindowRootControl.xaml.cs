using Files.Shared.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

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

		public object AppContent => Root.Content;

		private void MainWindowRootControl_Loaded(object sender, RoutedEventArgs e)
		{
			_ = InitAsync();
		}

		/// <inheritdoc/>
		public async Task InitAsync(CancellationToken cancellationToken = default)
		{
			await CreateSplashScreenAsync();
			_ = InitializeAppComponentsAsync();
			await ShowMainScreenAsync(cancellationToken);
			_ = CheckForRequiredUpdates(cancellationToken);
		}

		private async Task CreateSplashScreenAsync()
		{
			Root.Content = new SplashScreenPage();

			// Delay to allow splash-screen to load
			await Task.Delay(100);
			await Task.Delay(15);
		}

		private async Task ShowMainScreenAsync(CancellationToken cancellationToken)
		{
			var parameter = await GetActivationParameterAsync(cancellationToken);
			var mainPage = new MainPage(parameter);
			Root.Content = mainPage;
			await mainPage.InitAsync(cancellationToken);
		}

		private async Task InitializeAppComponentsAsync()
		{
			var userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			var addItemService = Ioc.Default.GetRequiredService<IAddItemService>();
			var generalSettingsService = userSettingsService.GeneralSettingsService;

			// Start off a list of tasks we need to run before we can continue startup
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

			static Task OptionalTask(Task task, bool condition)
			{
				if (condition)
					return task;

				return Task.CompletedTask;
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

		private async Task<object?> GetActivationParameterAsync(CancellationToken cancellationToken)
		{
			var mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

			switch (App.ActivationArgs)
			{
				case ILaunchActivatedEventArgs launchArgs:
					if (launchArgs.Arguments is not null &&
						(CommandLineParser.SplitArguments(launchArgs.Arguments, true)[0].EndsWith($"files.exe", StringComparison.OrdinalIgnoreCase)
						|| CommandLineParser.SplitArguments(launchArgs.Arguments, true)[0].EndsWith($"files", StringComparison.OrdinalIgnoreCase)))
					{
						// WINUI3: When launching from commandline the argument is not ICommandLineActivatedEventArgs (#10370)
						var ppm = CommandLineParser.ParseUntrustedCommands(launchArgs.Arguments);
						if (ppm.IsEmpty())
							return null;
						else
							return await InitializeFromCmdLineArgs(ppm, mainPageViewModel);
					}
					else if (!MainPageViewModel.AppInstances.Any())
					{
						// When the navigation stack isn't restored navigate to the first page,
						// configuring the new page by passing required information as a navigation parameter
						return launchArgs.Arguments;
					}
					else if (!(string.IsNullOrEmpty(launchArgs.Arguments) && MainPageViewModel.AppInstances.Count > 0))
					{
						//await mainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), launchArgs.Arguments);
					}
					else
					{
						return null;
					}
					break;

				case IProtocolActivatedEventArgs eventArgs:
					if (eventArgs.Uri.AbsoluteUri == "files-uwp:")
					{
						return null;
					}
					else
					{
						var parsedArgs = eventArgs.Uri.Query.TrimStart('?').Split('=');
						var unescapedValue = Uri.UnescapeDataString(parsedArgs[1]);
						var folder = (StorageFolder)await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(unescapedValue).AsTask());
						if (folder is not null && !string.IsNullOrEmpty(folder.Path))
						{
							// Convert short name to long name (#6190)
							unescapedValue = folder.Path;
						}
						switch (parsedArgs[0])
						{
							case "tab":
								return new MainPageNavigationArguments()
								{
									Parameter = CustomTabViewItemParameter.Deserialize(unescapedValue),
									IgnoreStartupSettings = true
								};
								break;

							case "folder":
								return
									new MainPageNavigationArguments()
										{ Parameter = unescapedValue, IgnoreStartupSettings = true };
								break;

							case "cmd":
								var ppm = CommandLineParser.ParseUntrustedCommands(unescapedValue);
								if (ppm.IsEmpty())
									return null;
								else
									return await InitializeFromCmdLineArgs(ppm, mainPageViewModel);
								break;
							default:
								return null;
								break;
						}
					}
					break;

				case ICommandLineActivatedEventArgs cmdLineArgs:
					var operation = cmdLineArgs.Operation;
					var cmdLineString = operation.Arguments;
					var activationPath = operation.CurrentDirectoryPath;

					var parsedCommands = CommandLineParser.ParseUntrustedCommands(cmdLineString);
					if (parsedCommands is not null && parsedCommands.Count > 0)
					{
						return await InitializeFromCmdLineArgs(parsedCommands, mainPageViewModel, activationPath);
					}
					else
					{
						return null;
					}
					break;

				case IFileActivatedEventArgs fileArgs:
					var index = 0;
					if (!MainPageViewModel.AppInstances.Any())
					{
						// When the navigation stack isn't restored navigate to the first page,
						// configuring the new page by passing required information as a navigation parameter
						return fileArgs.Files.First().Path;
						index = 1;
					}
					for (; index < fileArgs.Files.Count; index++)
					{
						await mainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), fileArgs.Files[index].Path);
					}
					break;
			}

			return null;
		}

		private async Task<object?> InitializeFromCmdLineArgs(ParsedCommands parsedCommands, MainPageViewModel mainPageViewModel, string activationPath = "")
		{
			async Task<object?> PerformNavigation(string payload, string selectItem = null)
			{
				if (!string.IsNullOrEmpty(payload))
				{
					payload = Constants.UserEnvironmentPaths.ShellPlaces.Get(payload.ToUpperInvariant(), payload);
					var folder = (StorageFolder)await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(payload).AsTask());
					if (folder is not null && !string.IsNullOrEmpty(folder.Path))
						payload = folder.Path; // Convert short name to long name (#6190)
				}

				var generalSettingsService = Ioc.Default.GetService<IGeneralSettingsService>();
				var paneNavigationArgs = new PaneNavigationArguments
				{
					LeftPaneNavPathParam = payload,
					LeftPaneSelectItemParam = selectItem,
					RightPaneNavPathParam = MainWindow.Instance.Bounds.Width > PaneHolderPage.DualPaneWidthThreshold && (generalSettingsService?.AlwaysOpenDualPaneInNewTab ?? false) ? "Home" : null,
				};

				if (MainPageViewModel.AppInstances.Any())
				{
					await mainPageViewModel.AddNewTabByParam(typeof(PaneHolderPage), paneNavigationArgs);
					return null;
				}
				else
					return paneNavigationArgs;
			}
			foreach (var command in parsedCommands)
			{
				switch (command.Type)
				{
					case ParsedCommandType.OpenDirectory:
					case ParsedCommandType.OpenPath:
					case ParsedCommandType.ExplorerShellCommand:
						var selectItemCommand = parsedCommands.FirstOrDefault(x => x.Type == ParsedCommandType.SelectItem);
						return await PerformNavigation(command.Payload, selectItemCommand?.Payload);
						break;

					case ParsedCommandType.SelectItem:
						if (SystemIO.Path.IsPathRooted(command.Payload))
							return await PerformNavigation(SystemIO.Path.GetDirectoryName(command.Payload), SystemIO.Path.GetFileName(command.Payload));
						break;

					case ParsedCommandType.TagFiles:
						var tagService = Ioc.Default.GetService<IFileTagsSettingsService>();
						var tag = tagService.GetTagsByName(command.Payload).FirstOrDefault();
						foreach (var file in command.Args.Skip(1))
						{
							var fileFRN = await FilesystemTasks.Wrap(() => StorageHelpers.ToStorageItem<IStorageItem>(file))
								.OnSuccess(item => FileTagsHelper.GetFileFRN(item));
							if (fileFRN is not null)
							{
								var tagUid = tag is not null ? new[] { tag.Uid } : null;
								var dbInstance = FileTagsHelper.GetDbInstance();
								dbInstance.SetTags(file, fileFRN, tagUid);
								FileTagsHelper.WriteFileTag(file, tagUid);
							}
						}
						break;

					case ParsedCommandType.Unknown:
						if (command.Payload.Equals("."))
						{
							return await PerformNavigation(activationPath);
						}
						else
						{
							if (!string.IsNullOrEmpty(command.Payload))
							{
								var target = SystemIO.Path.GetFullPath(SystemIO.Path.Combine(activationPath, command.Payload));
								return await PerformNavigation(target);
							}
							else
							{
								await PerformNavigation(null);
							}
						}
						break;

					case ParsedCommandType.OutputPath:
						App.OutputPath = command.Payload;
						break;
				}
			}

			return null;
		}
	}
}
