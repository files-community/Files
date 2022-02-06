using Files.CommandLine;
using Files.Common;
using Files.Helpers;
using System;
using System.Collections.Generic;
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
        public static readonly string PrelaunchInstanceKey = "PrelaunchInstance";

        private static async Task Main()
        {
            var proc = System.Diagnostics.Process.GetCurrentProcess();

            if (!ApplicationData.Current.LocalSettings.Values.Get("AlwaysOpenANewInstance", false))
            {
                IActivatedEventArgs activatedArgs = AppInstance.GetActivatedEventArgs();

                if (AppInstance.RecommendedInstance != null)
                {
                    AppInstance.RecommendedInstance.RedirectActivationTo();
                }
                else
                {
                    if (activatedArgs is LaunchActivatedEventArgs launchArgs)
                    {
                        if (launchArgs.PrelaunchActivated)
                        {
                            var instance = AppInstance.FindOrRegisterInstanceForKey(PrelaunchInstanceKey);
                            RedirectOrStartActivation(instance, true);
                        }
                        else
                        {
                            var preLaunchInstance = FindAppInstanceForKey(PrelaunchInstanceKey);
                            if (preLaunchInstance != null && ApplicationData.Current.LocalSettings.Values.Get("PENDING_LAUNCH_FROM_PRELAUNCH", false))
                            {
                                ApplicationData.Current.LocalSettings.Values["PENDING_LAUNCH_FROM_PRELAUNCH"] = false;
                                preLaunchInstance.RedirectActivationTo();
                            }
                            else
                            {
                                var instance = AppInstance.FindOrRegisterInstanceForKey(proc.Id.ToString());
                                RedirectOrStartActivation(instance);
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
                            var instance = AppInstance.FindOrRegisterInstanceForKey(proc.Id.ToString());
                            RedirectOrStartActivation(instance);
                        }
                    }
                    else
                    {
                        var instance = AppInstance.FindOrRegisterInstanceForKey(proc.Id.ToString());
                        RedirectOrStartActivation(instance);
                    }
                }
            }
            else
            {
                AppInstance.FindOrRegisterInstanceForKey(proc.Id.ToString());
                Application.Start(_ => new App());
            }
        }

        public static void RedirectOrStartActivation(AppInstance instance, bool forPrelaunch = false)
        {
            if (instance.IsCurrentInstance)
            {
                if (forPrelaunch)
                {
                    ApplicationData.Current.LocalSettings.Values["PENDING_LAUNCH_FROM_PRELAUNCH"] = true;
                }
                Application.Start(_ => new App());
            }
            else
            {
                if (forPrelaunch)
                {
                    ApplicationData.Current.LocalSettings.Values["PENDING_LAUNCH_FROM_PRELAUNCH"] = false;
                }
                instance.RedirectActivationTo();
            }
        }

        public static AppInstance FindAppInstanceForKey(string key, IList<AppInstance> instances = null)
        {
            instances ??= AppInstance.GetInstances();
            return instances.FirstOrDefault(x => x.Key.Equals(key));
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

        [Obsolete("This method is no longer needed for multi-instancing", false)]
        public static async Task TerminateUwpAppInstance(int pid)
        {
            ApplicationData.Current.LocalSettings.Values["Arguments"] = "TerminateUwp";
            ApplicationData.Current.LocalSettings.Values["pid"] = pid;
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }
    }
}