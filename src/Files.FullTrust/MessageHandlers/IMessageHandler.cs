using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading.Tasks;

namespace Files.FullTrust.MessageHandlers
{
    public interface IMessageHandler : IDisposable
    {
        Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, JsonElement> message, string arguments);

        void Initialize(PipeStream connection);
    }
}
