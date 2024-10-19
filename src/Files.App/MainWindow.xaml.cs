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
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace Files.App
{
    public sealed partial class MainWindow : WinUIEx.WindowEx
    {
        private static readonly Lazy<MainWindow> _Instance = new(() => new MainWindow());
        public static MainWindow Instance => _Instance.Value;

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

        private void SetAppCulture()
        {
            var culture = CultureInfo.CurrentUICulture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            ApplicationLanguages.PrimaryLanguageOverride = culture.Name;
        }

        public void ShowSplashScreen()
        {
            var rootFrame = EnsureWindowIsInitialized();

            if (rootFrame != null)
            {
                rootFrame.Navigate(typeof(SplashScreenPage));
            }
        }

        public async Task InitializeApplicationAsync(object activatedEventArgs)
        {
            var rootFrame = EnsureWindowIsInitialized();

            if (rootFrame == null)
                return;

            // Set system backdrop
            SystemBackdrop = new AppSystemBackdrop();

            try
            {
                switch (activatedEventArgs)
                {
                    case ILaunchActivatedEventArgs launchArgs:
                        await HandleLaunchArgsAsync(rootFrame, launchArgs);
                        break;

                    case IProtocolActivatedEventArgs protocolArgs:
                        await HandleProtocolArgsAsync(rootFrame, protocolArgs);
                        break;

                    case IFileActivatedEventArgs fileArgs:
                        HandleFileArgs(rootFrame, fileArgs);
                        break;

                    default:
                        rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
                        break;
                }
            }
            catch (Exception ex)
            {
                // Log or handle exceptions accordingly
                Console.WriteLine($"Error during initialization: {ex.Message}");
            }
        }

        private async Task HandleLaunchArgsAsync(Frame rootFrame, ILaunchActivatedEventArgs launchArgs)
        {
            var args = CommandLineParser.SplitArguments(launchArgs.Arguments, true);
            if (args.Length > 0 &&
                (args[0].EndsWith("files.exe", StringComparison.OrdinalIgnoreCase) ||
                 args[0].EndsWith("files", StringComparison.OrdinalIgnoreCase)))
            {
                var ppm = CommandLineParser.ParseUntrustedCommands(launchArgs.Arguments);
                if (ppm.IsEmpty())
                {
                    rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
                }
                else
                {
                    await InitializeFromCmdLineArgsAsync(rootFrame, ppm);
                }
            }
            else if (rootFrame.Content is null || rootFrame.Content is SplashScreenPage || !MainPageViewModel.AppInstances.Any())
            {
                rootFrame.Navigate(typeof(MainPage), launchArgs.Arguments, new SuppressNavigationTransitionInfo());
            }
            else
            {
                Win32Helper.BringToForegroundEx(new(WindowHandle));
                await NavigationHelpers.AddNewTabByPathAsync(typeof(ShellPanesPage), launchArgs.Arguments, true);
            }
        }

        private async Task HandleProtocolArgsAsync(Frame rootFrame, IProtocolActivatedEventArgs eventArgs)
        {
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
                if (parsedArgs.Length == 2)
                {
                    var unescapedValue = Uri.UnescapeDataString(parsedArgs[1]);
                    var folder = (StorageFolder)await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(unescapedValue).AsTask());
                    if (folder != null && !string.IsNullOrEmpty(folder.Path))
                    {
                        unescapedValue = folder.Path;
                    }

                    switch (parsedArgs[0])
                    {
                        case "tab":
                            rootFrame.Navigate(typeof(MainPage), new MainPageNavigationArguments() { Parameter = TabBarItemParameter.Deserialize(unescapedValue), IgnoreStartupSettings = true }, new SuppressNavigationTransitionInfo());
                            break;

                        case "folder":
                            rootFrame.Navigate(typeof(MainPage), new MainPageNavigationArguments() { Parameter = unescapedValue, IgnoreStartupSettings = true }, new SuppressNavigationTransitionInfo());
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
                else
                {
                    rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
                }
            }
        }

        private void HandleFileArgs(Frame rootFrame, IFileActivatedEventArgs fileArgs)
        {
            if (fileArgs.Files.Count > 0)
            {
                rootFrame.Navigate(typeof(MainPage), fileArgs.Files, new SuppressNavigationTransitionInfo());
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
