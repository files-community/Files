using Files.CommandLine;
using Files.Common;
using Files.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Files
{
    internal class Program
    {
        const string PrelaunchInstanceKey = "PrelaunchInstance";

        private static async Task Main()
        {
            var proc = System.Diagnostics.Process.GetCurrentProcess();

            if (!ApplicationData.Current.LocalSettings.Values.Get("AlwaysOpenANewInstance", false))
            {
                IActivatedEventArgs activatedArgs = AppInstance.GetActivatedEventArgs();

                if (AppInstance.RecommendedInstance != null)
                {
                    AppInstance.RecommendedInstance.RedirectActivationTo();
                    await TerminateUwpAppInstance(proc.Id);
                    return;
                }
                else if (activatedArgs is LaunchActivatedEventArgs)
                {
                    var launchArgs = activatedArgs as LaunchActivatedEventArgs;

                    if (launchArgs.PrelaunchActivated && AppInstance.GetInstances().Count == 0)
                    {
                        AppInstance.FindOrRegisterInstanceForKey(PrelaunchInstanceKey);
                        ApplicationData.Current.LocalSettings.Values["WAS_PRELAUNCH_INSTANCE_ACTIVATED"] = false;
                        Application.Start(_ => new App());
                        return;
                    }
                    else
                    {
                        bool wasPrelaunchInstanceActivated = ApplicationData.Current.LocalSettings.Values.Get("WAS_PRELAUNCH_INSTANCE_ACTIVATED", true);
                        if (AppInstance.GetInstances().Any(x => x.Key.Equals(PrelaunchInstanceKey)) && !wasPrelaunchInstanceActivated)
                        {
                            var plInstance = AppInstance.GetInstances().First(x => x.Key.Equals(PrelaunchInstanceKey));
                            ApplicationData.Current.LocalSettings.Values["WAS_PRELAUNCH_INSTANCE_ACTIVATED"] = true;
                            plInstance.RedirectActivationTo();
                            await TerminateUwpAppInstance(proc.Id);
                            return;
                        }
                        else
                        {
                            var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
                            var instance = AppInstance.FindOrRegisterInstanceForKey(activePid.ToString());
                            if (!instance.IsCurrentInstance && !string.IsNullOrWhiteSpace(launchArgs.Arguments))
                            {
                                instance.RedirectActivationTo();
                                await TerminateUwpAppInstance(proc.Id);
                                return;
                            }
                        }
                    }
                }
                else if (activatedArgs is CommandLineActivatedEventArgs cmdLineArgs)
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

                    // Always open a new instance for OpenDialog
                    if (parsedCommands == null || !parsedCommands.Any(x => x.Type == ParsedCommandType.OutputPath))
                    {
                        var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
                        var instance = AppInstance.FindOrRegisterInstanceForKey(activePid.ToString());
                        if (!instance.IsCurrentInstance)
                        {
                            instance.RedirectActivationTo();
                            await TerminateUwpAppInstance(proc.Id);
                            return;
                        }
                    }
                }
                else if (activatedArgs is FileActivatedEventArgs)
                {
                    var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
                    var instance = AppInstance.FindOrRegisterInstanceForKey(activePid.ToString());
                    if (!instance.IsCurrentInstance)
                    {
                        instance.RedirectActivationTo();
                        await TerminateUwpAppInstance(proc.Id);
                        return;
                    }
                }
            }

            AppInstance.FindOrRegisterInstanceForKey(proc.Id.ToString());
            ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = proc.Id;
            Application.Start(_ => new App());
        }

        public static async Task OpenShellCommandInExplorerAsync(string shellCommand, int pid)
        {
            ApplicationData.Current.LocalSettings.Values["ShellCommand"] = shellCommand;
            ApplicationData.Current.LocalSettings.Values["Arguments"] = "ShellCommand";
            ApplicationData.Current.LocalSettings.Values["pid"] = pid;
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        public static async Task SpawnUnelevatedUwpAppInstance(int pid)
        {
            IActivatedEventArgs activatedArgs = AppInstance.GetActivatedEventArgs();
            if (activatedArgs is CommandLineActivatedEventArgs cmdLineArgs)
            {
                var parsedCommands = CommandLineParser.ParseUntrustedCommands(cmdLineArgs.Operation.Arguments);
                switch (parsedCommands.FirstOrDefault()?.Type)
                {
                    case ParsedCommandType.ExplorerShellCommand:
                    case ParsedCommandType.OpenDirectory:
                    case ParsedCommandType.OpenPath:
                        ApplicationData.Current.LocalSettings.Values["Folder"] = parsedCommands[0].Payload;
                        break;
                }
            }
            ApplicationData.Current.LocalSettings.Values["Arguments"] = "StartUwp";
            ApplicationData.Current.LocalSettings.Values["pid"] = pid;
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        public static async Task TerminateUwpAppInstance(int pid)
        {
            ApplicationData.Current.LocalSettings.Values["Arguments"] = "TerminateUwp";
            ApplicationData.Current.LocalSettings.Values["pid"] = pid;
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }
    }
}