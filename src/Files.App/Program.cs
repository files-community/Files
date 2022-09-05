using Files.App.CommandLine;
using Files.App.Helpers;
using Files.Shared.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System.Threading;
using Microsoft.UI.Dispatching;
using System.Runtime.InteropServices;
using static UWPToWinAppSDKUpgradeHelpers.InteropHelpers;

namespace Files.App
{
    internal class Program
    {
        [DllImport("Microsoft.ui.xaml.dll")]
        private static extern void XamlCheckProcessRequirements();

        // Note: We can't declare Main to be async because in a WinUI app
        // this prevents Narrator from reading XAML elements.
        // https://github.com/microsoft/WindowsAppSDK-Samples/blob/main/Samples/AppLifecycle/Instancing/cs-winui-packaged/CsWinUiDesktopInstancing/CsWinUiDesktopInstancing/Program.cs
        [STAThread] // STAThread has no effect if main is async, needed for Clipboard
        private static void Main()
        {
            XamlCheckProcessRequirements();
            WinRT.ComWrappersSupport.InitializeComWrappers();

            var proc = System.Diagnostics.Process.GetCurrentProcess();
            var alwaysOpenNewInstance = ApplicationData.Current.LocalSettings.Values.Get("AlwaysOpenANewInstance", false);
            var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

            if (!alwaysOpenNewInstance)
            {
                if (activatedArgs.Kind is ExtendedActivationKind.Launch)
                {
                    var launchArgs = activatedArgs.Data as ILaunchActivatedEventArgs;

                    if (false)
                    {
                        // WINUI3: remove
                    }
                    else
                    {
                        if (false)
                        {
                            // WINUI3: remove
                        }
                        else
                        {
                            var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
                            var instance = AppInstance.FindOrRegisterForKey(activePid.ToString());
                            if (!instance.IsCurrent && !string.IsNullOrWhiteSpace(launchArgs.Arguments))
                            {
                                RedirectActivationTo(instance, activatedArgs);
                                return;
                            }
                        }
                    }
                }
                else if (activatedArgs.Data is IProtocolActivatedEventArgs protocolArgs)
                {
                    var parsedArgs = protocolArgs.Uri.Query.TrimStart('?').Split('=');
                    if (parsedArgs.Length == 2 && parsedArgs[0] == "cmd") // Treat as command line launch
                    {
                        var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
                        var instance = AppInstance.FindOrRegisterForKey(activePid.ToString());
                        if (!instance.IsCurrent)
                        {
                            RedirectActivationTo(instance, activatedArgs);
                            return;
                        }
                    }
                }
                else if (activatedArgs.Data is IFileActivatedEventArgs)
                {
                    var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
                    var instance = AppInstance.FindOrRegisterForKey(activePid.ToString());
                    if (!instance.IsCurrent)
                    {
                        RedirectActivationTo(instance, activatedArgs);
                        return;
                    }
                }
            }

            if (activatedArgs.Data is ICommandLineActivatedEventArgs cmdLineArgs)
            {
                var operation = cmdLineArgs.Operation;
                var cmdLineString = operation.Arguments;
                var parsedCommands = CommandLineParser.ParseUntrustedCommands(cmdLineString);

                if (parsedCommands != null)
                {
                    foreach (var command in parsedCommands)
                    {
                        switch (command.Type)
                        {
                            case ParsedCommandType.ExplorerShellCommand:
                                if (!CommonPaths.ShellPlaces.ContainsKey(command.Payload.ToUpperInvariant()))
                                {
                                    OpenShellCommandInExplorer(command.Payload, proc.Id);
                                    return; // Exit
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }

                // Always open a new instance for OpenDialog, never open new instance for "-Tag" command
                if (parsedCommands == null || !parsedCommands.Any(x => x.Type == ParsedCommandType.OutputPath)
                    && (!alwaysOpenNewInstance || parsedCommands.Any(x => x.Type == ParsedCommandType.TagFiles)))
                {
                    var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
                    var instance = AppInstance.FindOrRegisterForKey(activePid.ToString());
                    if (!instance.IsCurrent)
                    {
                        RedirectActivationTo(instance, activatedArgs);
                        // Terminate "zombie" Files process which remains in suspended state
                        // after redirection when launched by command line
                        //TerminateUwpAppInstance(proc.Id); // WINUI3: check if needed
                        return;
                    }
                }
            }

            var currentInstance = AppInstance.FindOrRegisterForKey((-proc.Id).ToString());
            if (currentInstance.IsCurrent)
            {
                currentInstance.Activated += OnActivated;
            }
            ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = -proc.Id;
            Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }

        private static void OnActivated(object? sender, AppActivationArguments args)
        {
            if (App.Current is App thisApp)
            {
                // WINUI3: verify if needed or OnLaunched is called
                thisApp.OnActivated(args);
            }
        }

        public static void OpenShellCommandInExplorer(string shellCommand, int pid)
        {
            ApplicationData.Current.LocalSettings.Values["ShellCommand"] = shellCommand;
            ApplicationData.Current.LocalSettings.Values["Arguments"] = "ShellCommand";
            ApplicationData.Current.LocalSettings.Values["pid"] = pid;
            var ftpPath = System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Files.FullTrust", "FilesFullTrust.exe");
            System.Diagnostics.Process.Start(ftpPath);
        }

        public static void TerminateUwpAppInstance(int pid)
        {
            ApplicationData.Current.LocalSettings.Values["Arguments"] = "TerminateUwp";
            ApplicationData.Current.LocalSettings.Values["pid"] = pid;
            var ftpPath = System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Files.FullTrust", "FilesFullTrust.exe");
            System.Diagnostics.Process.Start(ftpPath);
        }

        private static IntPtr redirectEventHandle = IntPtr.Zero;

        // Do the redirection on another thread, and use a non-blocking
        // wait method to wait for the redirection to complete.
        public static void RedirectActivationTo(
            AppInstance keyInstance, AppActivationArguments args)
        {
            redirectEventHandle = CreateEvent(IntPtr.Zero, true, false, null);
            Task.Run(() =>
            {
                keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
                SetEvent(redirectEventHandle);
            });
            uint CWMO_DEFAULT = 0;
            uint INFINITE = 0xFFFFFFFF;
            _ = CoWaitForMultipleObjects(
               CWMO_DEFAULT, INFINITE, 1,
               new IntPtr[] { redirectEventHandle }, out uint handleIndex);
        }
    }
}