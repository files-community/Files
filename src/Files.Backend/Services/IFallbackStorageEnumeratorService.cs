using System;
using System.Collections.Generic;

namespace Files.Backend.Services
{
    public interface IFallbackStorageEnumeratorService : IDisposable
    {
        bool IsAvailable(string path);

        IEnumerable<string> Enumerate(string path); // TODO - just an example
    }
}
