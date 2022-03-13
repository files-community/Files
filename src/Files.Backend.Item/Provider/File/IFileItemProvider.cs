using System.Collections.Generic;

namespace Files.Backend.Item
{
    public interface IFileItemProvider : IItemProvider
    {
        new IAsyncEnumerable<IFileItem> ProvideItems();
    }
}
