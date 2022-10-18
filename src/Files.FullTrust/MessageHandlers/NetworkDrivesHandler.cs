using Files.Shared;
using Files.Shared.Extensions;
using Files.FullTrust.Helpers;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.Foundation.Collections;

namespace Files.FullTrust.MessageHandlers
{
    [SupportedOSPlatform("Windows10.0.10240")]
    public class NetworkDrivesHandler : Disposable, IMessageHandler
    {
        private readonly JsonElement defaultJson = JsonSerializer.SerializeToElement("{}");

        public void Initialize(PipeStream connection)
        {
        }

        public async Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, JsonElement> message, string arguments)
        {
            switch (arguments)
            {
                case "NetworkDriveOperation":
                    await ParseNetworkDriveOperationAsync(connection, message);
                    break;

                case "DetectCloudDrives":
                    var cloudDrives = await CloudDrivesDetector.DetectCloudDrives();
                    await Win32API.SendMessageAsync(connection, new ValueSet()
                    {
                        { "Drives", JsonSerializer.Serialize(cloudDrives) }
                    }, message.Get("RequestID", defaultJson).GetString());
                    break;
            }
        }

        private async Task ParseNetworkDriveOperationAsync(PipeStream connection, Dictionary<string, JsonElement> message)
        {
            switch (message.Get("netdriveop", defaultJson).GetString())
            {
                case "GetNetworkLocations":
                    var networkLocations = await Win32API.StartSTATask(() =>
                    {
                        var locations = new List<ShellLinkItem>();
                        using (var nethood = new ShellFolder(Shell32.KNOWNFOLDERID.FOLDERID_NetHood))
                        {
                            foreach (var item in nethood)
                            {
                                if (item is ShellLink link)
                                {
                                    locations.Add(ShellFolderHelpers.GetShellLinkItem(link));
                                }
                                else
                                {
                                    var linkPath = (string)item.Properties["System.Link.TargetParsingPath"];
                                    if (linkPath != null)
                                    {
                                        var linkItem = ShellFolderHelpers.GetShellFileItem(item);
                                        locations.Add(new ShellLinkItem(linkItem) { TargetPath = linkPath });
                                    }
                                }
                            }
                        }
                        return locations;
                    });
                    var response = new ValueSet
                    {
                        { "NetworkLocations", JsonSerializer.Serialize(networkLocations) }
                    };
                    await Win32API.SendMessageAsync(connection, response, message.Get("RequestID", defaultJson).GetString());
                    break;

                case "OpenMapNetworkDriveDialog":
                    var hwnd = message["HWND"].GetInt64();
                    _ = NetworkDrivesAPI.OpenMapNetworkDriveDialog(hwnd);
                    break;

                case "DisconnectNetworkDrive":
                    var drivePath = message["drive"].GetString();
                    _ = NetworkDrivesAPI.DisconnectNetworkDrive(drivePath);
                    break;
            }
        }
    }
}
