// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.IO;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Microsoft.UI.Xaml;
using WinUIEx;
using IO = System.IO;

namespace Files.App
{
	public sealed partial class MainWindow : WindowEx
	{
		private readonly IApplicationService ApplicationService;

		private MainPageViewModel mainPageViewModel;

		private static MainWindow? _Instance;
		public static MainWindow Instance => _Instance ??= new();

		public IntPtr WindowHandle { get; }

		private MainWindow()
		{
			ApplicationService = new ApplicationService();
			WindowHandle = this.GetWindowHandle();
			InitializeComponent();
			EnsureEarlyWindow();
		}

		private void EnsureEarlyWindow()
		{
			// Set PersistenceId
			PersistenceId = "FilesMainWindow";

			// Set minimum sizes
			MinHeight = 416;
			MinWidth = 516;

			AppWindow.Title = "Files";
			AppWindow.SetIcon(Path.Combine(Package.Current.InstalledLocation.Path, ApplicationService.AppIcoPath));
			AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
			AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

			// Workaround for full screen window messing up the taskbar
			// https://github.com/microsoft/microsoft-ui-xaml/issues/8431
			// This property should only be set if the "Automatically hide the taskbar" in Windows 11,
			// or "Automatically hide the taskbar in desktop mode" in Windows 10 is enabled.
			// Setting this property when the setting is disabled will result in the taskbar overlapping the application
			if (AppLifecycleHelper.IsAutoHideTaskbarEnabled()) 
				InteropHelpers.SetPropW(WindowHandle, "NonRudeHWND", new IntPtr(1));
		}

		private ContentControl GetMainContent()
		{
			if (Content is ContentControl mainContent)
				return mainContent;

			return new ContentControl()
			{
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				VerticalContentAlignment = VerticalAlignment.Stretch
			};
		}

		public void ShowSplashScreen()
		{
			var mainContent = GetMainContent();
			mainContent.Content = new SplashScreenPage();
			Content = mainContent;
		}

		public async Task InitializeApplicationAsync(object activatedEventArgs)
		{
			var mainContent = GetMainContent();

			// Set system backdrop
			SystemBackdrop = new AppSystemBackdrop();

			switch (activatedEventArgs)
			{
				case ILaunchActivatedEventArgs launchArgs:
					if (launchArgs.Arguments is not null &&
						(CommandLineParser.SplitArguments(launchArgs.Arguments, true)[0].EndsWith($"files.exe", StringComparison.OrdinalIgnoreCase)
						|| CommandLineParser.SplitArguments(launchArgs.Arguments, true)[0].EndsWith($"files", StringComparison.OrdinalIgnoreCase)))
					{
						// WINUI3: When launching from commandline the argument is not ICommandLineActivatedEventArgs (#10370)
						var ppm = CommandLineParser.ParseUntrustedCommands(launchArgs.Arguments);
						if (ppm.IsEmpty())
							NavigateRoot(new MainPage(), null);
						else
							await InitializeFromCmdLineArgsAsync(ppm);
					}
					else if (mainContent.Content is null || mainContent.Content is SplashScreenPage || !MainPageViewModel.AppInstances.Any())
					{
						// When the navigation stack isn't restored navigate to the first page,
						// configuring the new page by passing required information as a navigation parameter
						NavigateRoot(new MainPage(), launchArgs.Arguments);
					}
					else if (!(string.IsNullOrEmpty(launchArgs.Arguments) && MainPageViewModel.AppInstances.Count > 0))
					{
						InteropHelpers.SwitchToThisWindow(WindowHandle, true);
						await NavigationHelpers.AddNewTabByPathAsync(typeof(PaneHolderPage), launchArgs.Arguments);
					}
					else
					{
						NavigateRoot(new MainPage(), null);
					}
					break;

				case IProtocolActivatedEventArgs eventArgs:
					if (eventArgs.Uri.AbsoluteUri == "files-uwp:")
					{
						NavigateRoot(new MainPage(), null);
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
								NavigateRoot(new MainPage(), new MainPageNavigationArguments() { Parameter = CustomTabViewItemParameter.Deserialize(unescapedValue), IgnoreStartupSettings = true });
								break;

							case "folder":
								NavigateRoot(new MainPage(), new MainPageNavigationArguments() { Parameter = unescapedValue, IgnoreStartupSettings = true });
								break;

							case "cmd":
								var ppm = CommandLineParser.ParseUntrustedCommands(unescapedValue);
								if (ppm.IsEmpty())
									NavigateRoot(new MainPage(), null);
								else
									await InitializeFromCmdLineArgsAsync(ppm);
								break;
							default:
								NavigateRoot(new MainPage(), null);
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
						await InitializeFromCmdLineArgsAsync(parsedCommands, activationPath);
					}
					else
					{
						NavigateRoot(new MainPage(), null);
					}
					break;

				case IFileActivatedEventArgs fileArgs:
					var index = 0;
					if (mainContent.Content is null || mainContent.Content is SplashScreenPage || !MainPageViewModel.AppInstances.Any())
					{
						// When the navigation stack isn't restored navigate to the first page,
						// configuring the new page by passing required information as a navigation parameter
						NavigateRoot(new MainPage(), fileArgs.Files.First().Path);
						index = 1;
					}
					else
						InteropHelpers.SwitchToThisWindow(WindowHandle, true);
					for (; index < fileArgs.Files.Count; index++)
					{
						await NavigationHelpers.AddNewTabByPathAsync(typeof(PaneHolderPage), fileArgs.Files[index].Path);
					}
					break;

				case IStartupTaskActivatedEventArgs startupArgs:
					// Just launch the app with no arguments
					NavigateRoot(new MainPage(), null);
					break;

				default:
					// Just launch the app with no arguments
					NavigateRoot(new MainPage(), null);
					break;
			}

			if (!AppWindow.IsVisible)
			{
				// When resuming the cached instance
				AppWindow.Show();
				Activate();
			}

			Content = mainContent;
		}

		private void NavigateRoot(Page page, object parameter)
		{
			if (Content is not ContentControl contentControl)
				return;

			contentControl.Content = page;
			if (page is MainPage mainPage)
				mainPage.NotifyNavigatedTo(parameter);
		}

		/// <summary>
		/// Invoked when Navigation to a certain page fails
		/// </summary>
		/// <param name="sender">The Frame which failed navigation</param>
		/// <param name="e">Details about the navigation failure</param>
		private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
			=> throw new Exception("Failed to load Page " + e.SourcePageType.FullName);

		private async Task InitializeFromCmdLineArgsAsync(ParsedCommands parsedCommands, string activationPath = "")
		{
			async Task PerformNavigationAsync(string payload, string selectItem = null)
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
					RightPaneNavPathParam = Bounds.Width > PaneHolderPage.DualPaneWidthThreshold && (generalSettingsService?.AlwaysOpenDualPaneInNewTab ?? false) ? "Home" : null,
				};

				if (Content is ContentControl contentControl && contentControl.Content is MainPage && MainPageViewModel.AppInstances.Any())
				{
					InteropHelpers.SwitchToThisWindow(WindowHandle, true);
					await NavigationHelpers.AddNewTabByParamAsync(typeof(PaneHolderPage), paneNavigationArgs);
				}
				else
					NavigateRoot(new MainPage(), paneNavigationArgs);
			}
			foreach (var command in parsedCommands)
			{
				switch (command.Type)
				{
					case ParsedCommandType.OpenDirectory:
					case ParsedCommandType.OpenPath:
					case ParsedCommandType.ExplorerShellCommand:
						var selectItemCommand = parsedCommands.FirstOrDefault(x => x.Type == ParsedCommandType.SelectItem);
						await PerformNavigationAsync(command.Payload, selectItemCommand?.Payload);
						break;

					case ParsedCommandType.SelectItem:
						if (IO.Path.IsPathRooted(command.Payload))
							await PerformNavigationAsync(IO.Path.GetDirectoryName(command.Payload), IO.Path.GetFileName(command.Payload));
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
							await PerformNavigationAsync(activationPath);
						}
						else
						{
							if (!string.IsNullOrEmpty(command.Payload))
							{
								var target = IO.Path.GetFullPath(IO.Path.Combine(activationPath, command.Payload));
								await PerformNavigationAsync(target);
							}
							else
							{
								await PerformNavigationAsync(null);
							}
						}
						break;

					case ParsedCommandType.OutputPath:
						App.OutputPath = command.Payload;
						break;
				}
			}
		}
	}
}
