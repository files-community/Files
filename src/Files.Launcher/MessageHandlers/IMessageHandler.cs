using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace FilesFullTrust.MessageHandlers
{
    public interface IMessageHandler : IDisposable
    {
        Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, object> message, string arguments);

        void Initialize(PipeStream connection);
    }
}
