using Files.CommandLine;
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
        private static void Main()
        {
            var args = Environment.GetCommandLineArgs();

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
                                var proc = System.Diagnostics.Process.GetCurrentProcess();
                                OpenShellCommandInExplorer(command.Payload, proc.Id).GetAwaiter().GetResult();
                                //Exit..

                                return;

                            default:
                                break;
                        }
                    }
                }
            }

            IActivatedEventArgs activatedArgs = AppInstance.GetActivatedEventArgs();

            if (AppInstance.RecommendedInstance != null)
            {
                AppInstance.RecommendedInstance.RedirectActivationTo();
            }
            else if (activatedArgs is LaunchActivatedEventArgs)
            {
                var launchArgs = activatedArgs as LaunchActivatedEventArgs;

                // Constant key, only one instance activated
                var instance = AppInstance.FindOrRegisterInstanceForKey("FILESUWP");
                if (instance.IsCurrentInstance || string.IsNullOrEmpty(launchArgs.Arguments))
                {
                    // If we successfully registered this instance, we can now just
                    // go ahead and do normal XAML initialization.
                    Application.Start(_ => new App());
                }
                else
                {
                    // Some other instance has registered for this key, so we'll 
                    // redirect this activation to that instance instead.
                    instance.RedirectActivationTo();
                }
            }
            else
            {
                Application.Start(_ => new App());
            }
        }

        public static async Task OpenShellCommandInExplorer(string shellCommand, int pid)
        {
            System.Diagnostics.Debug.WriteLine("Launching shell command in FullTrustProcess");
            ApplicationData.Current.LocalSettings.Values["ShellCommand"] = shellCommand;
            ApplicationData.Current.LocalSettings.Values["Arguments"] = "ShellCommand";
            ApplicationData.Current.LocalSettings.Values["pid"] = pid;
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }
    }
}