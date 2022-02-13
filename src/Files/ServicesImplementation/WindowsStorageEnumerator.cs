using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Files.Backend.Services;
using Files.Backend.ViewModels.ItemListing;

namespace Files.ServicesImplementation
{
    internal sealed class WindowsStorageEnumerator : IFallbackStorageEnumeratorService
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ListedItemViewModel> Enumerate()
        {
            throw new NotImplementedException();
        }
    }
}
