using System;
using System.Collections.Generic;
using Files.Backend.ViewModels.ItemListing;

namespace Files.Backend.Services
{
    public interface IStorageEnumeratorService : IDisposable
    {
        bool IsAvailable { get; }

        IEnumerable<ListedItemViewModel> Enumerate(); // TODO - just an example
    }
}
