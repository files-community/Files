// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System.Globalization;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.Activation;
using Windows.Globalization;
using Windows.Storage;
using IO = System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace Files.App
{
    public sealed partial class MainWindow : WinUIEx.WindowEx
    {
        private static MainWindow? _Instance;
        public static MainWindow Instance => _Instance ??= new();

        public nint WindowHandle { get; }

        public MainWindow()
        {
            InitializeComponent();

            // Force the app to use the correct culture for UI elements
            SetAppCulture();

            WindowHandle = WinUIEx.WindowExtensions.GetWindowHandle(this);
            MinHeight = 316;
            MinWidth = 416;
            ExtendsContentIntoTitleBar = true;
            Title = "Files";
            PersistenceId = "FilesMainWindow";
            AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonPressedBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonHoverBackgroundColor = Colors.Transparent;
            AppWindow.SetIcon(AppLifecycleHelper.AppIconPath);
        }

        /// <summary>
        /// Set the application's culture to match the system's current UI culture.
        /// This ensures that the correct language is applied to all UI elements.
        /// </summary>
        private void SetAppCulture()
        {
            var culture = CultureInfo.CurrentUICulture;

            // Apply the system's UI culture across the application
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            ApplicationLanguages.PrimaryLanguageOverride = culture.Name;
        }

        public void ShowSplashScreen()
        {
            var rootFrame = EnsureWindowIsInitialized();

            rootFrame?.Navigate(typeof(SplashScreenPage));
        }

        public async Task InitializeApplicationAsync(object activatedEventArgs)
        {
            var rootFrame = EnsureWindowIsInitialized();

            if (rootFrame is null)
                return;

            // Set system backdrop
            SystemBackdrop = new AppSystemBackdrop();

            switch (activatedEventArgs)
            {
                case ILaunchActivatedEventArgs launchArgs:
                    if (launchArgs.Arguments is not null &&
                        (CommandLineParser.SplitArguments(launchArgs.Arguments, true)[0].EndsWith($"files.exe", StringComparison.OrdinalIgnoreCase)
                        || CommandLineParser.SplitArguments(launchArgs.Arguments, true)[0].EndsWith($"files", StringComparison.OrdinalIgnoreCase)))
                    {
                        var ppm = CommandLineParser.ParseUntrustedCommands(launchArgs.Arguments);
                        if (ppm.IsEmpty())
                            rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
                        else
                            await InitializeFromCmdLineArgsAsync(rootFrame, ppm);
                    }
                    else if (rootFrame.Content is null || rootFrame.Content is SplashScreenPage || !MainPageViewModel.AppInstances.Any())
                    {
                        rootFrame.Navigate(typeof(MainPage), launchArgs.Arguments, new SuppressNavigationTransitionInfo());
                    }
                    else if (!(string.IsNullOrEmpty(launchArgs.Arguments) && MainPageViewModel.AppInstances.Count > 0))
                    {
                        Win32Helper.BringToForegroundEx(new(WindowHandle));
                        await NavigationHelpers.AddNewTabByPathAsync(typeof(ShellPanesPage), launchArgs.Arguments, true);
                    }
                    else
                    {
                        rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
                    }
                    break;

                case IProtocolActivatedEventArgs eventArgs:
                    if (eventArgs.Uri.AbsoluteUri == "files-uwp:")
                    {
                        rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
                        if (MainPageViewModel.AppInstances.Count > 0)
                        {
                            Win32Helper.BringToForegroundEx(new(WindowHandle));
                        }
                    }
                    else
                    {
                        var parsedArgs = eventArgs.Uri.Query.TrimStart('?').Split('=');
                        var unescapedValue = Uri.UnescapeDataString(parsedArgs[1]);
                        var folder = (StorageFolder)await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(unescapedValue).AsTask());
                        if (folder is not null && !string.IsNullOrEmpty(folder.Path))
                        {
                            unescapedValue = folder.Path;
                        }
                        switch (parsedArgs[0])
                        {
                            case "tab":
                                rootFrame.Navigate(typeof(MainPage),
                                    new MainPageNavigationArguments() { Parameter = TabBarItemParameter.Deserialize(unescapedValue), IgnoreStartupSettings = true },
                                    new SuppressNavigationTransitionInfo());
                                break;

                            case "folder":
                                rootFrame.Navigate(typeof(MainPage),
                                    new MainPageNavigationArguments() { Parameter = unescapedValue, IgnoreStartupSettings = true },
                                    new SuppressNavigationTransitionInfo());
                                break;

                            case "cmd":
                                var ppm = CommandLineParser.ParseUntrustedCommands(unescapedValue);
                                if (ppm.IsEmpty())
                                    rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
                                else
                                    await InitializeFromCmdLineArgsAsync(rootFrame, ppm);
                                break;
                            default:
                                rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
                                break;
                        }
                    }
                    break;

                case IFileActivatedEventArgs fileArgs:
                    if (fileArgs.Files.Count > 0)
                    {
                        rootFrame.Navigate(typeof(MainPage), fileArgs.Files, new SuppressNavigationTransitionInfo());
                    }
                    break;

                default:
                    rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
                    break;
            }
        }

        private Frame? EnsureWindowIsInitialized()
        {
            try
            {
                if (Instance.Content is not Frame rootFrame)
                {
                    rootFrame = new() { CacheSize = 1 };
                    rootFrame.NavigationFailed += (s, e) =>
                    {
                        throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
                    };

                    Instance.Content = rootFrame;
                }

                return rootFrame;
            }
            catch (COMException)
            {
                return null;
            }
        }

        private async Task InitializeFromCmdLineArgsAsync(Frame rootFrame, CommandLineParameterModel ppm)
        {
            if (ppm.Commands?.FirstOrDefault() is string cmd)
            {
                if (cmd.Equals("open", StringComparison.OrdinalIgnoreCase) && ppm.Parameters?.FirstOrDefault() is string path)
                {
                    await NavigationHelpers.AddNewTabByPathAsync(typeof(ShellPanesPage), path, true);
                }
                else if (cmd.Equals("new", StringComparison.OrdinalIgnoreCase))
                {
                    rootFrame.Navigate(typeof(MainPage), new MainPageNavigationArguments() { Parameter = null }, new SuppressNavigationTransitionInfo());
                }
            }
        }
    }
}
