using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Files.Helpers
{
    class RegistryReader
    {
        private bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                      .IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void ParseRegistryAndAddToList(List<(string commandKey, string commandName, string commandIcon, string command)> shellList, RegistryKey shellKey)
        {
            if(shellKey != null)
            {
                foreach (var keyname in shellKey.GetSubKeyNames())
                {
                    var commandNameKey = shellKey.OpenSubKey(keyname);
                    var commandName = commandNameKey.GetValue(String.Empty).ToString()?.Replace("&", "");
                    var commandIcon = commandNameKey.GetValue("Icon").ToString();

                    var commandNameKeyNames = commandNameKey.GetSubKeyNames();
                    if (commandNameKeyNames.Contains("command") && !shellList.Any(c => c.commandKey == keyname))
                    {
                        var command = commandNameKey.OpenSubKey("command");
                        shellList.Add((commandKey: keyname, commandName, commandIcon, command: command.GetValue(string.Empty).ToString()));

                    }
                }
            }
        }

        public IEnumerable<(string commanyKey, string commandName, string commandIcon, string command)> GetExtensionContextMenuForFiles(string fileExtension)
        {

            var shellList = new List<(string commandKey, string commandName, string commandIcon, string command)>();
            try
            {
                if (IsAdministrator())
                {
                    using RegistryKey classRootShellKey = Registry.ClassesRoot.OpenSubKey("*\\shell");
                    ParseRegistryAndAddToList(shellList, classRootShellKey);

                    using RegistryKey classRootFileExtensionShellKey = Registry.ClassesRoot.OpenSubKey($"{fileExtension}\\shell");
                    ParseRegistryAndAddToList(shellList, classRootFileExtensionShellKey);
                }

                using RegistryKey currentUserShellKey = Registry.CurrentUser.OpenSubKey("Software\\Classes\\*\\shell");
                ParseRegistryAndAddToList(shellList, currentUserShellKey);

                using RegistryKey currentUserFileExtensionShellKey = Registry.CurrentUser.OpenSubKey($"Software\\Classes\\{fileExtension}\\shell");
                ParseRegistryAndAddToList(shellList, currentUserFileExtensionShellKey);


                return shellList;
            }
            catch
            {
                return shellList;
            }
        }
    }
}
