using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Linq;
using Files.Common;

namespace FilesFullTrust.MessageHandlers
{
    public class InstallOperationsHandler : IMessageHandler
    {
        public void Initialize(PipeStream connection)
        {
        }

        public async Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "InstallOperation":
                    await ParseInstallOperationAsync(connection, message);
                    break;
            }
        }

        private async Task ParseInstallOperationAsync(PipeStream connection, Dictionary<string, object> message)
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
                        Win32API.InstallHinfSection(IntPtr.Zero, IntPtr.Zero, filePath, 0);
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