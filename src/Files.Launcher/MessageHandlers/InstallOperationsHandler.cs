using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Linq;
using Files.Common;
using System.Runtime.Versioning;

namespace FilesFullTrust.MessageHandlers
{
    [SupportedOSPlatform("Windows10.0.10240")]
    public class InstallOperationsHandler : IMessageHandler
    {
        public void Initialize(PipeStream connection)
        {
        }

        public Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "InstallOperation":
                    ParseInstallOperation(connection, message);
                    break;
            }
            return Task.CompletedTask;
        }

        private static void ParseInstallOperation(PipeStream connection, Dictionary<string, object> message)
        {
            switch (message.Get("installop", ""))
            {
                case "InstallInf":
                {
                    var filePath = (string)message["filepath"];
                    var fileExtension = (string)message["extension"];
                    var isInf = new[] { ".inf" }.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);

                    if (isInf)
                    {
                        Win32API.InfDefaultInstall(filePath);
                    }
                    break;
                }
                case "InstallFont":
                {
                    var filePath = (string)message["filepath"];
                    var fileExtension = (string)message["extension"];
                    var isFont = new[] { ".fon", ".otf", ".ttc", ".ttf" }.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);

                    if (isFont)
                    {
                        var userFontDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Fonts");
                        var destName = Path.Combine(userFontDir, Path.GetFileName(filePath));
                        Win32API.RunPowershellCommand($"-command \"Copy-Item '{filePath}' '{userFontDir}'; New-ItemProperty -Name '{Path.GetFileNameWithoutExtension(filePath)}' -Path 'HKCU:\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Fonts' -PropertyType string -Value '{destName}'\"", false);
                    }
                    break;
                }
            }
        }

        public void Dispose()
        {
        }
    }
}