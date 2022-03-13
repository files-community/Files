using System.Collections.Generic;

namespace Files.Backend.Item
{
    public interface IItemProvider
    {
        IAsyncEnumerable<IItem> ProvideItems();
    }
}
