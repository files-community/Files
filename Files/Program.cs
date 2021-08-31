using Files.CommandLine;
using Files.Common;
using System;
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
            var args = Environment.GetCommandLineArgs();
            var proc = System.Diagnostics.Process.GetCurrentProcess();

            if (args.Length == 2)
            {
                var parsedCommands = CommandLineParser.ParseUntrustedCommands(args);

                if (parsedCommands != null && parsedCommands.Count > 0)
                {
                    foreach (var command in parsedCommands)
                    {
                        switch (command.Type)
                        {
                            case ParsedCommandType.ExplorerShellCommand:
                                await OpenShellCommandInExplorerAsync(command.Payload, proc.Id);
                                //Exit..

                                return;

                            default:
                                break;
                        }
                    }
                }
            }

            if (!ApplicationData.Current.LocalSettings.Values.Get("AlwaysOpenANewInstance", false))
            {
                IActivatedEventArgs activatedArgs = AppInstance.GetActivatedEventArgs();

                if (AppInstance.RecommendedInstance != null)
                {
                    AppInstance.RecommendedInstance.RedirectActivationTo();
                    return;
                }
                else if (activatedArgs is LaunchActivatedEventArgs)
                {
                    var launchArgs = activatedArgs as LaunchActivatedEventArgs;

                    var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
                    var instance = AppInstance.FindOrRegisterInstanceForKey(activePid.ToString());
                    if (!instance.IsCurrentInstance && !string.IsNullOrEmpty(launchArgs.Arguments))
                    {
                        instance.RedirectActivationTo();
                        return;
                    }
                }
                else if (activatedArgs is FileActivatedEventArgs)
                {
                    var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
                    var instance = AppInstance.FindOrRegisterInstanceForKey(activePid.ToString());
                    if (!instance.IsCurrentInstance)
                    {
                        instance.RedirectActivationTo();
                        return;
                    }
                }
                else if (activatedArgs is CommandLineActivatedEventArgs)
                {
                    var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
                    var instance = AppInstance.FindOrRegisterInstanceForKey(activePid.ToString());
                    if (!instance.IsCurrentInstance)
                    {
                        instance.RedirectActivationTo();
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
            System.Diagnostics.Debug.WriteLine("Launching shell command in FullTrustProcess");
            ApplicationData.Current.LocalSettings.Values["ShellCommand"] = shellCommand;
            ApplicationData.Current.LocalSettings.Values["Arguments"] = "ShellCommand";
            ApplicationData.Current.LocalSettings.Values["pid"] = pid;
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }
    }
}