using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation.Collections;

namespace Files.Helpers
{
    class ShellCommandParser
    {
        /*

        %0 or %1 – The first file parameter. For example "C:\Users\Eric\Desktop\New Text Document.txt". Generally this should be in quotes and the applications command line parsing should accept quotes to disambiguate files with spaces in the name and different command line parameters (this is a security best practice and I believe mentioned in MSDN).

        %<n> (where <n> is 2-9) – Replace with the nth parameter.

        %s – Show command.

        %h – Hotkey value.

        %i – IDList stored in a shared memory handle is passed here.

        %l – Long file name form of the first parameter. Note that Win32/64 applications will be passed the long file name, whereas Win16 applications get the short file name. Specifying %l is preferred as it avoids the need to probe for the application type.

        %d – Desktop absolute parsing name of the first parameter (for items that don't have file system paths).

        %v – For verbs that are none implies all. If there is no parameter passed this is the working directory.

        %w – The working directory.
         */
        public async Task<(string command, string arguments)> ParseShellCommand(string command, string itemPath)
        {
            if(string.IsNullOrEmpty(command) || string.IsNullOrEmpty(itemPath))
            {
                return (null, null);
            }    

            var value = new ValueSet
                            {
                                { "Arguments", "ParseAguments" },
                                { "Command", command}
                            };
            var response = await App.Connection.SendMessageAsync(value);
            if (response.Status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success
                && response.Message.ContainsKey("ParsedArguments"))
            {

                var commandToExecute = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>((string)response.Message["ParsedArguments"]);

                var resultCommand = string.Join(" ", commandToExecute.Skip(1));
                var shellFileNameRegex = new Regex("(%[0-9]|%D|%L|%U|%V)", RegexOptions.IgnoreCase);
                resultCommand = shellFileNameRegex.Replace(resultCommand, $"\"{itemPath}\"");

                var shellParentFolderRegex = new Regex("%W", RegexOptions.IgnoreCase);
                var fileInfo = new FileInfo(itemPath);
                resultCommand = shellParentFolderRegex.Replace(resultCommand, $"\"{fileInfo.Directory.FullName}\"");

                var shellHotKeyValueRegex = new Regex("%H", RegexOptions.IgnoreCase);
                resultCommand = shellHotKeyValueRegex.Replace(resultCommand, "0");

                var shelShowCommandRegex = new Regex("%S", RegexOptions.IgnoreCase);
                resultCommand = shelShowCommandRegex.Replace(resultCommand, "1");
                return (commandToExecute[0], resultCommand);
            }
            else
            {
                return (null, null);
            }
        }
    }
}
