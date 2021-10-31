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
                        var netl = new ValueSet();
                        using (var nethood = new ShellFolder(Shell32.KNOWNFOLDERID.FOLDERID_NetHood))
                        {
                            foreach (var link in nethood)
                            {
                                var linkPath = (string)link.Properties["System.Link.TargetParsingPath"];
                                if (linkPath != null)
                                {
                                    netl.Add(link.Name, linkPath);
                                }
                            }
                        }
                        return netl;
                    });
                    networkLocations ??= new ValueSet();
                    networkLocations.Add("Count", networkLocations.Count);
                    await Win32API.SendMessageAsync(connection, networkLocations, message.Get("RequestID", (string)null));
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
