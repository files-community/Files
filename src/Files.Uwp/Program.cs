using Files.Uwp.CommandLine;
using Files.Uwp.Helpers;
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

namespace Files.Uwp
{
    internal class Program
    {
        // Note: We can't declare Main to be async because in a WinUI app
        // this prevents Narrator from reading XAML elements.
        //WINUI3: verify if still true
        // https://github.com/microsoft/WindowsAppSDK-Samples/blob/main/Samples/AppLifecycle/Instancing/cs-winui-packaged/CsWinUiDesktopInstancing/CsWinUiDesktopInstancing/Program.cs
        [STAThread]
        private static async Task Main()
        {
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
                                await instance.RedirectActivationToAsync(activatedArgs);
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
                            await instance.RedirectActivationToAsync(activatedArgs);
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
                        await instance.RedirectActivationToAsync(activatedArgs);
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
                                    await OpenShellCommandInExplorerAsync(command.Payload, proc.Id);
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
                        await instance.RedirectActivationToAsync(activatedArgs);
                        // Terminate "zombie" Files process which remains in suspended state
                        // after redirection when launched by command line
                        await TerminateUwpAppInstance(proc.Id);
                        return;
                    }
                }
            }

            var currentInstance = AppInstance.FindOrRegisterForKey(proc.Id.ToString());
            if (currentInstance.IsCurrent)
            {
                currentInstance.Activated += OnActivated;
            }
            ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = proc.Id;
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

        public static async Task OpenShellCommandInExplorerAsync(string shellCommand, int pid)
        {
            ApplicationData.Current.LocalSettings.Values["ShellCommand"] = shellCommand;
            ApplicationData.Current.LocalSettings.Values["Arguments"] = "ShellCommand";
            ApplicationData.Current.LocalSettings.Values["pid"] = pid;
            await Windows.ApplicationModel.FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        public static async Task TerminateUwpAppInstance(int pid)
        {
            ApplicationData.Current.LocalSettings.Values["Arguments"] = "TerminateUwp";
            ApplicationData.Current.LocalSettings.Values["pid"] = pid;
            await Windows.ApplicationModel.FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }
    }
}