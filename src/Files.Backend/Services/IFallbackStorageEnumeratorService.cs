using System;
using System.Collections.Generic;
using Files.Backend.ViewModels.ItemListing;

namespace Files.Backend.Services
{
    public interface IFallbackStorageEnumeratorService : IDisposable
    {
        IEnumerable<ListedItemViewModel> Enumerate(); // TODO - just an example
    }
}
