using Files.FullTrust.Helpers;
using Files.Shared.Extensions;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation.Collections;

namespace Files.FullTrust.MessageHandlers
{
    [SupportedOSPlatform("Windows10.0.10240")]
    public class DriveHandler : Disposable, IMessageHandler
    {
        private readonly JsonElement defaultJson = JsonSerializer.SerializeToElement("{}");

        public void Initialize(PipeStream connection) {}

        public async Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, JsonElement> message, string arguments)
        {
            if (arguments is "VolumeID")
            {
                string driveName = message["DriveName"].GetString();
                var volumeId = DriveHelpers.GetVolumeId(driveName);

                var response = new ValueSet{ ["VolumeID"] = volumeId };
                await Win32API.SendMessageAsync(connection, response, message.Get("RequestID", defaultJson).GetString());
            }
        }
    }
}
