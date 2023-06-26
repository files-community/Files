// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.MultitaskingControl;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using WinUIEx;

namespace Files.App
{
	public sealed partial class MainWindow : WindowEx
	{
		private readonly MainPageViewModel _mainPageViewModel;

		public MainWindow()
		{
			InitializeComponent();

			_mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

			EnsureEarlyWindow();
		}

		private void EnsureEarlyWindow()
		{
			PersistenceId = "FilesMainWindow";
			MinHeight = 328;
			MinWidth = 516;

			// Initialize AppWindow
			AppWindow.Title = "Files";
			AppWindow.SetIcon(SystemIO.Path.Combine(Package.Current.InstalledLocation.Path, App.LogoPath));
			AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
			AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
		}

		public async Task InitializeApplication(object activatedEventArgs)
		{
			// Get an initialized frame
			var rootFrame = EnsureWindowIsInitialized();

			Activate();

			switch (activatedEventArgs)
			{
				case ILaunchActivatedEventArgs launchArgs:
					if (launchArgs.Arguments is not null && launchArgs.Arguments.Contains($"files.exe", StringComparison.OrdinalIgnoreCase))
					{
						// WINUI3: When launching from command line the argument is not ICommandLineActivatedEventArgs (#10370)
						var ppm = CommandLineParser.ParseUntrustedCommands(launchArgs.Arguments);
						if (ppm.IsEmpty())
							rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
						else
							await InitializeFromCmdLineArgs(rootFrame, ppm);
					}
					else if (rootFrame.Content is null)
					{
						// When the navigation stack isn't restored navigate to the first page,
						// configuring the new page by passing required information as a navigation parameter
						rootFrame.Navigate(typeof(MainPage), launchArgs.Arguments, new SuppressNavigationTransitionInfo());
					}
					else
					{
						if (!(string.IsNullOrEmpty(launchArgs.Arguments) && MainPageViewModel.AppInstances.Count > 0))
						{
							await _mainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), launchArgs.Arguments);
						}
					}
					break;

				case IProtocolActivatedEventArgs eventArgs:
					if (eventArgs.Uri.AbsoluteUri == "files-uwp:")
					{
						rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
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
								rootFrame.Navigate(typeof(MainPage), TabItemArguments.Deserialize(unescapedValue), new SuppressNavigationTransitionInfo());
								break;

							case "folder":
								rootFrame.Navigate(typeof(MainPage), unescapedValue, new SuppressNavigationTransitionInfo());
								break;

							case "cmd":
								var ppm = CommandLineParser.ParseUntrustedCommands(unescapedValue);
								if (ppm.IsEmpty())
									rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
								else
									await InitializeFromCmdLineArgs(rootFrame, ppm);
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
						await InitializeFromCmdLineArgs(rootFrame, parsedCommands, activationPath);
					}
					break;

				case IFileActivatedEventArgs fileArgs:
					var index = 0;
					if (rootFrame.Content is null)
					{
						// When the navigation stack isn't restored navigate to the first page,
						// configuring the new page by passing required information as a navigation parameter
						rootFrame.Navigate(typeof(MainPage), fileArgs.Files.First().Path, new SuppressNavigationTransitionInfo());
						index = 1;
					}
					for (; index < fileArgs.Files.Count; index++)
					{
						await _mainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), fileArgs.Files[index].Path);
					}
					break;
			}

			if (rootFrame.Content is null)
				rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
		}

		private Frame EnsureWindowIsInitialized()
		{
			// NOTE:
			//  Do not repeat app initialization when the Window already has content,
			//  just ensure that the window is active
			if (App.Window.Content is not Frame rootFrame)
			{
				// Set system backdrop
				SystemBackdrop = new AppSystemBackdrop();

				// Create a Frame to act as the navigation context and navigate to the first page
				rootFrame = new() { CacheSize = 1 };
				rootFrame.NavigationFailed += OnNavigationFailed;

				// Place the frame in the current Window
				App.Window.Content = rootFrame;
			}

			return rootFrame;
		}

		private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
		{
			throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
		}

		private async Task InitializeFromCmdLineArgs(Frame rootFrame, ParsedCommands parsedCommands, string activationPath = "")
		{
			async Task PerformNavigation(string payload, string selectItem = null)
			{
				if (!string.IsNullOrEmpty(payload))
				{
					payload = Constants.UserEnvironmentPaths.ShellPlaces.Get(payload.ToUpperInvariant(), payload);
					var folder = (StorageFolder)await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(payload).AsTask());
					if (folder is not null && !string.IsNullOrEmpty(folder.Path))
						payload = folder.Path; // Convert short name to long name (#6190)
				}

				var paneNavigationArgs = new PaneNavigationArguments
				{
					LeftPaneNavPathParam = payload,
					LeftPaneSelectItemParam = selectItem,
				};

				if (rootFrame.Content is not null)
					await _mainPageViewModel.AddNewTabByParam(typeof(PaneHolderPage), paneNavigationArgs);
				else
					rootFrame.Navigate(typeof(MainPage), paneNavigationArgs, new SuppressNavigationTransitionInfo());
			}

			foreach (var command in parsedCommands)
			{
				switch (command.Type)
				{
					case ParsedCommandType.OpenDirectory:
					case ParsedCommandType.OpenPath:
					case ParsedCommandType.ExplorerShellCommand:
						var selectItemCommand = parsedCommands.FirstOrDefault(x => x.Type == ParsedCommandType.SelectItem);
						await PerformNavigation(command.Payload, selectItemCommand?.Payload);
						break;

					case ParsedCommandType.SelectItem:
						if (SystemIO.Path.IsPathRooted(command.Payload))
							await PerformNavigation(SystemIO.Path.GetDirectoryName(command.Payload), SystemIO.Path.GetFileName(command.Payload));
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
							await PerformNavigation(activationPath);
						}
						else
						{
							if (!string.IsNullOrEmpty(command.Payload))
							{
								var target = SystemIO.Path.GetFullPath(SystemIO.Path.Combine(activationPath, command.Payload));
								await PerformNavigation(target);
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
		}
	}
}
