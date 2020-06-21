using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Helpers
{
    class RegistryReader
    {

        private async Task ParseRegistryAndAddToList(List<(string commandKey, string commandName, string commandIcon, string command)> shellList, RegistryKey shellKey)
        {
            if(shellKey != null)
            {
                foreach (var keyname in shellKey.GetSubKeyNames())
                {
                    try
                    {
                        var commandNameKey = shellKey.OpenSubKey(keyname);
                        var commandName = commandNameKey.GetValue(String.Empty)?.ToString() ?? "";
                        //@ is a special command under the registry. We need to search for MUIVerb:
                        if (string.IsNullOrEmpty(commandName) || commandName.StartsWith("@"))
                        {

                            var muiVerb = commandNameKey.GetValue("MUIVerb")?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(muiVerb) && App.Connection != null)
                            {
                                var muiVerbRequest = new ValueSet
                            {
                                { "Arguments", "LoadMUIVerb" },
                                { "MUIVerbLocation", muiVerb?.Split(',')[0]?.TrimStart('@') },
                                { "MUIVerbLine", Convert.ToInt32(muiVerb?.Split(',')[1]?.TrimStart('-')) }
                            };
                                var responseMUIVerb = await App.Connection.SendMessageAsync(muiVerbRequest);
                                if (responseMUIVerb.Status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success
                                    && responseMUIVerb.Message.ContainsKey("MUIVerbString"))
                                {
                                    commandName = (string)responseMUIVerb.Message["MUIVerbString"];
                                    if (string.IsNullOrEmpty(commandName))
                                    {
                                        continue;
                                    }
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                        var commandNameString = commandName.Replace("&", "");
                        var commandIconString = commandNameKey.GetValue("Icon")?.ToString();


                        var commandNameKeyNames = commandNameKey.GetSubKeyNames();
                        if (commandNameKeyNames.Contains("command") && !shellList.Any(c => c.commandKey == keyname))
                        {
                            var command = commandNameKey.OpenSubKey("command");
                            shellList.Add((commandKey: keyname, commandNameString, commandIconString, command: command.GetValue(string.Empty).ToString()));

                        }
                    }
                    catch
                    {
                        continue;
                    }
                   
                }
            }
        }

        public async Task<IEnumerable<(string commanyKey, string commandName, string commandIcon, string command)>> GetExtensionContextMenuForFiles(bool isDirectory, string fileExtension)
        {

            var shellList = new List<(string commandKey, string commandName, string commandIcon, string command)>();
            try
            {
               
                if(isDirectory)
                {
                    using RegistryKey classRootDirectoryShellKey = Registry.ClassesRoot.OpenSubKey("Directory\\shell");
                    await ParseRegistryAndAddToList(shellList, classRootDirectoryShellKey);
                }
                else
                {
                    using RegistryKey classRootShellKey = Registry.ClassesRoot.OpenSubKey("*\\shell");
                    await ParseRegistryAndAddToList(shellList, classRootShellKey);

                    using RegistryKey classRootFileExtensionShellKey = Registry.ClassesRoot.OpenSubKey($"{fileExtension}\\shell");
                    await ParseRegistryAndAddToList(shellList, classRootFileExtensionShellKey);

                    using RegistryKey currentUserShellKey = Registry.CurrentUser.OpenSubKey("Software\\Classes\\*\\shell");
                    await ParseRegistryAndAddToList(shellList, currentUserShellKey);

                    using RegistryKey currentUserFileExtensionShellKey = Registry.CurrentUser.OpenSubKey($"Software\\Classes\\{fileExtension}\\shell");
                    await ParseRegistryAndAddToList(shellList, currentUserFileExtensionShellKey);

                }

                return shellList;
            }
            catch
            {
                return shellList;
            }
        }
    }
}
