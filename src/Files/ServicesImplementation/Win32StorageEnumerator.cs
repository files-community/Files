using System;
using System.Collections.Generic;
using Files.Backend.Services;
using Files.Backend.ViewModels.ItemListing;

namespace Files.ServicesImplementation
{
    internal sealed class Win32StorageEnumerator : IStorageEnumeratorService
    {
        public bool IsAvailable { get; }

        public IEnumerable<ListedItemViewModel> Enumerate()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
