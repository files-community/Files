using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Windows.Storage;

namespace ProcessLauncher
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var arguments = (string)localSettings.Values["Arguments"];
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                if (arguments.Equals("StartupTasks"))
                {
                    // Check QuickLook Availability
                    QuickLook.CheckQuickLookAvailability(localSettings);
                }
                else if (arguments.Equals("ToggleQuickLook"))
                {
                    var path = (string)localSettings.Values["path"];
                    QuickLook.ToggleQuickLook(path);
                }
                else if (arguments.Equals("ShellCommand"))
                {
                    //Kill the process. This is a BRUTAL WAY to kill a process.
                    var pid = (int)ApplicationData.Current.LocalSettings.Values["pid"];
                    Process.GetProcessById(pid).Kill();

                    Process process = new Process();
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.FileName = "explorer.exe";
                    process.StartInfo.CreateNoWindow = false;
                    process.StartInfo.Arguments = (string)ApplicationData.Current.LocalSettings.Values["ShellCommand"];
                    process.Start();
                }
                else
                {
                    var executable = (string)ApplicationData.Current.LocalSettings.Values["Application"];
                    Process process = new Process();
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = executable;
                    process.StartInfo.CreateNoWindow = false;
                    process.StartInfo.Arguments = arguments;
                    process.Start();
                }
            }
            else
            {
                try
                {
                    var executable = (string)ApplicationData.Current.LocalSettings.Values["Application"];
                    Process process = new Process();
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = executable;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                }
                catch (Win32Exception)
                {
                    var executable = (string)ApplicationData.Current.LocalSettings.Values["Application"];
                    Process process = new Process();
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.Verb = "runas";
                    process.StartInfo.FileName = executable;
                    process.StartInfo.CreateNoWindow = true;
                    try
                    {
                        process.Start();
                    }
                    catch (Win32Exception)
                    {
                        Process.Start(executable);
                    }
                }
            }
        }
    }
}