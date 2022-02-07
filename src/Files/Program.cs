using Files.CommandLine;
using Files.Common;
using Files.Extensions;
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

                    if (launchArgs.PrelaunchActivated)
                    {
                        ApplicationData.Current.LocalSettings.Values["PRELAUNCH_INSTANCE"] = proc.Id;
                    }
                    else
                    {
                        var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
                        var instance = FindAppInstanceForKey(activePid.ToString());
                        if (instance != null && !instance.IsCurrentInstance && !string.IsNullOrWhiteSpace(launchArgs.Arguments))
                        {
                            instance.RedirectActivationTo();
                            await TerminateUwpAppInstance(proc.Id);
                            return;
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
                        var instance = FindAppInstanceForKey(activePid.ToString());
                        if (instance != null && !instance.IsCurrentInstance)
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
                    var instance = FindAppInstanceForKey(activePid.ToString());
                    if (instance != null && !instance.IsCurrentInstance)
                    {
                        instance.RedirectActivationTo();
                        await TerminateUwpAppInstance(proc.Id);
                        return;
                    }
                }
            }

            var prelaunchPid = ApplicationData.Current.LocalSettings.Values.Get("PRELAUNCH_INSTANCE", -1);
            var prelaunchInstance = FindAppInstanceForKey(prelaunchPid.ToString());
            if (prelaunchInstance != null && !prelaunchInstance.IsCurrentInstance)
            {
                prelaunchInstance.RedirectActivationTo();
                await TerminateUwpAppInstance(proc.Id);
                return;
            }

            App.ShouldPrepareForPrelaunch = prelaunchInstance == null && prelaunchPid != proc.Id;

            AppInstance.FindOrRegisterInstanceForKey(proc.Id.ToString());
            ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = proc.Id;
            Application.Start(_ => new App());
        }

        public static AppInstance FindAppInstanceForKey(string key)
        {
            var instances = AppInstance.GetInstances();
            return instances.FirstOrDefault(x => x.Key.Equals(key));
        }

        public static async Task OpenShellCommandInExplorerAsync(string shellCommand, int pid)
        {
            ApplicationData.Current.LocalSettings.Values["ShellCommand"] = shellCommand;
            ApplicationData.Current.LocalSettings.Values["Arguments"] = "ShellCommand";
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