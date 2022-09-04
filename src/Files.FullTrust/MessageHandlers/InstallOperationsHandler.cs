using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Linq;
using Files.Shared.Extensions;
using System.Runtime.Versioning;
using System.Text.Json;

namespace Files.FullTrust.MessageHandlers
{
    [SupportedOSPlatform("Windows10.0.10240")]
    public class InstallOperationsHandler : Disposable, IMessageHandler
    {
        private static readonly JsonElement defaultJson = JsonSerializer.SerializeToElement("{}");

        public void Initialize(PipeStream connection)
        {
        }

        public Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, JsonElement> message, string arguments)
        {
            switch (arguments)
            {
                case "InstallOperation":
                    ParseInstallOperation(connection, message);
                    break;
            }
            return Task.CompletedTask;
        }

        private static void ParseInstallOperation(PipeStream connection, Dictionary<string, JsonElement> message)
        {
            switch (message.Get("installop", defaultJson).GetString())
            {
                case "InstallInf":
                {
                    var filePath = message["filepath"].GetString();
                    var fileExtension = message["extension"].GetString();
                    var isInf = new[] { ".inf" }.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);

                    if (isInf)
                    {
                        Win32API.InfDefaultInstall(filePath);
                    }
                    break;
                }
                case "InstallFont":
                {
                    var filePath = message["filepath"].GetString();
                    var fileExtension = message["extension"].GetString();
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
    }
}