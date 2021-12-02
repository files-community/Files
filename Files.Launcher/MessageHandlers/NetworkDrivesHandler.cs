using Files.Common;
using FilesFullTrust.Helpers;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.Foundation.Collections;

namespace FilesFullTrust.MessageHandlers
{
    public class NetworkDrivesHandler : IMessageHandler
    {
        public void Initialize(PipeStream connection)
        {
        }

        public async Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, object> message, string arguments)
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
                        { "Drives", JsonConvert.SerializeObject(cloudDrives) }
                    }, message.Get("RequestID", (string)null));
                    break;
            }
        }

        private async Task ParseNetworkDriveOperationAsync(PipeStream connection, Dictionary<string, object> message)
        {
            switch (message.Get("netdriveop", ""))
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
                                    locations.Add(ShellFolderExtensions.GetShellLinkItem(link));
                                }
                                else
                                {
                                    var linkPath = (string)item.Properties["System.Link.TargetParsingPath"];
                                    if (linkPath != null)
                                    {
                                        var linkItem = ShellFolderExtensions.GetShellFileItem(item);
                                        locations.Add(new ShellLinkItem(linkItem) { TargetPath = linkPath });
                                    }
                                }
                            }
                        }
                        return locations;
                    });
                    var response = new ValueSet();
                    response.Add("NetworkLocations", JsonConvert.SerializeObject(networkLocations));
                    await Win32API.SendMessageAsync(connection, response, message.Get("RequestID", (string)null));
                    break;

                case "OpenMapNetworkDriveDialog":
                    var hwnd = (long)message["HWND"];
                    _ = NetworkDrivesAPI.OpenMapNetworkDriveDialog(hwnd);
                    break;

                case "DisconnectNetworkDrive":
                    var drivePath = (string)message["drive"];
                    _ = NetworkDrivesAPI.DisconnectNetworkDrive(drivePath);
                    break;
            }
        }

        public void Dispose()
        {
        }
    }
}
