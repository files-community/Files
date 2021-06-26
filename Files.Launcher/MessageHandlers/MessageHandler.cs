using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace FilesFullTrust.MessageHandlers
{
    public interface MessageHandler : IDisposable
    {
        Task ParseArgumentsAsync(NamedPipeServerStream connection, Dictionary<string, object> message, string arguments);
    }
}
