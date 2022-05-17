﻿using Files.Shared.Extensions;
using Files.FullTrust.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.Foundation.Collections;

namespace Files.FullTrust.MessageHandlers
{
    [SupportedOSPlatform("Windows10.0.10240")]
    public class ApplicationLaunchHandler : Disposable, IMessageHandler
    {
        public void Initialize(PipeStream connection)
        {
        }

        public async Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "LaunchSettings":
                    {
                        var page = message.Get("page", (string)null);
                        var appActiveManager = new Shell32.IApplicationActivationManager();
                        appActiveManager.ActivateApplication("windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel",
                            page, Shell32.ACTIVATEOPTIONS.AO_NONE, out _);
                        break;
                    }

                case "LaunchApp":
                    if (message.ContainsKey("Application"))
                    {
                        var application = (string)message["Application"];
                        var success = await HandleApplicationLaunch(application, message);
                        await Win32API.SendMessageAsync(connection, new ValueSet()
                        {
                            { "Success", success }
                        }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "RunCompatibilityTroubleshooter":
                    {
                        var filePath = (string)message["filepath"];
                        var afPath = Path.Combine(Path.GetTempPath(), "CompatibilityTroubleshooterAnswerFile.xml");
                        File.WriteAllText(afPath, string.Format("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Answers Version=\"1.0\"><Interaction ID=\"IT_LaunchMethod\"><Value>CompatTab</Value></Interaction><Interaction ID=\"IT_BrowseForFile\"><Value>{0}</Value></Interaction></Answers>", filePath));
                        message["Parameters"] = $"/id PCWDiagnostic /af \"{afPath}\"";
                        await HandleApplicationLaunch("msdt.exe", message);
                    }
                    break;
            }
        }

        private async Task<bool> HandleApplicationLaunch(string application, Dictionary<string, object> message)
        {
            var arguments = message.Get("Parameters", "");
            var workingDirectory = message.Get("WorkingDirectory", "");
            var currentWindows = Win32API.GetDesktopWindows();

            if (new[] { ".vhd", ".vhdx" }.Contains(Path.GetExtension(application).ToLowerInvariant()))
            {
                // Use powershell to mount vhds as this requires admin rights
                return Win32API.MountVhdDisk(application);
            }

            try
            {
                using Process process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = application;
                // Show window if workingDirectory (opening terminal)
                process.StartInfo.CreateNoWindow = string.IsNullOrEmpty(workingDirectory);
                if (arguments == "runas")
                {
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.Verb = "runas";
                    if (string.Equals(Path.GetExtension(application), ".msi", StringComparison.OrdinalIgnoreCase))
                    {
                        process.StartInfo.FileName = "msiexec.exe";
                        process.StartInfo.Arguments = $"/a \"{application}\"";
                    }
                }
                else if (arguments == "runasuser")
                {
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.Verb = "runasuser";
                    if (string.Equals(Path.GetExtension(application), ".msi", StringComparison.OrdinalIgnoreCase))
                    {
                        process.StartInfo.FileName = "msiexec.exe";
                        process.StartInfo.Arguments = $"/i \"{application}\"";
                    }
                }
                else
                {
                    process.StartInfo.Arguments = arguments;
                    // Refresh env variables for the child process
                    foreach (DictionaryEntry ent in Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine))
                        process.StartInfo.EnvironmentVariables[(string)ent.Key] = (string)ent.Value;
                    foreach (DictionaryEntry ent in Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User))
                        process.StartInfo.EnvironmentVariables[(string)ent.Key] = (string)ent.Value;
                    process.StartInfo.EnvironmentVariables["PATH"] = string.Join(";",
                        Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine),
                        Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User));
                }
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.Start();
                Win32API.BringToForeground(currentWindows);
                return true;
            }
            catch (Win32Exception)
            {
                using Process process = new Process();
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.FileName = application;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WorkingDirectory = workingDirectory;
                try
                {
                    process.Start();
                    Win32API.BringToForeground(currentWindows);
                    return true;
                }
                catch (Win32Exception)
                {
                    try
                    {
                        var opened = await Win32API.StartSTATask(() =>
                        {
                            var split = application.Split('|').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => GetMtpPath(x));
                            if (split.Count() == 1)
                            {
                                Process.Start(split.First());
                                Win32API.BringToForeground(currentWindows);
                            }
                            else
                            {
                                var groups = split.GroupBy(x => new
                                {
                                    Dir = Path.GetDirectoryName(x),
                                    Prog = Win32API.GetFileAssociationAsync(x).Result ?? Path.GetExtension(x)
                                });
                                foreach (var group in groups)
                                {
                                    if (!group.Any())
                                    {
                                        continue;
                                    }
                                    using var cMenu = ContextMenu.GetContextMenuForFiles(group.ToArray(), Shell32.CMF.CMF_DEFAULTONLY);
                                    cMenu?.InvokeVerb(Shell32.CMDSTR_OPEN);
                                }
                            }
                            return true;
                        });
                        if (!opened)
                        {
                            if (application.StartsWith(@"\\SHELL\", StringComparison.Ordinal))
                            {
                                opened = await Win32API.StartSTATask(() =>
                                {
                                    using var si = ShellFolderExtensions.GetShellItemFromPathOrPidl(application);
                                    using var cMenu = ContextMenu.GetContextMenuForFiles(new[] { si }, Shell32.CMF.CMF_DEFAULTONLY);
                                    cMenu?.InvokeItem(cMenu?.Items.FirstOrDefault().ID ?? -1);
                                    return true;
                                });
                            }
                        }
                        return opened;
                    }
                    catch (Win32Exception)
                    {
                        // Cannot open file (e.g DLL)
                        return false;
                    }
                    catch (ArgumentException)
                    {
                        // Cannot open file (e.g DLL)
                        return false;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Invalid file path
                return false;
            }
        }

        private string GetMtpPath(string executable)
        {
            if (executable.StartsWith("\\\\?\\", StringComparison.Ordinal))
            {
                using var computer = new ShellFolder(Shell32.KNOWNFOLDERID.FOLDERID_ComputerFolder);
                using var device = computer.FirstOrDefault(i => executable.Replace("\\\\?\\", "", StringComparison.Ordinal).StartsWith(i.Name, StringComparison.Ordinal));
                var deviceId = device?.ParsingName;
                var itemPath = Regex.Replace(executable, @"^\\\\\?\\[^\\]*\\?", "");
                return deviceId != null ? Path.Combine(deviceId, itemPath) : executable;
            }
            return executable;
        }
    }
}
