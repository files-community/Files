using Files.CommandLine;
using System;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;

namespace Files
{
    class Program
    {
        static void Main()
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

            Application.Start(_ => new App());
        }

        public static async Task OpenShellCommandInExplorer(string shellCommand, int pid)
        {
            System.Diagnostics.Debug.WriteLine("Launching shell command in FullTrustProcess");
            if (App.Connection != null)
            {
                var value = new ValueSet();
                value.Add("ShellCommand", shellCommand);
                value.Add("Arguments", "ShellCommand");
                value.Add("pid", pid);
                await App.Connection.SendMessageAsync(value);
            }
        }
    }
}
