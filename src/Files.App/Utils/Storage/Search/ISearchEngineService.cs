using Files.App.Utils;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Utils.Storage.Search
{
    public interface ISearchEngineService
    {
        Task<IList<ListedItem>> SearchAsync(string query, string? path, CancellationToken ct);
        Task<IList<ListedItem>> SuggestAsync(string query, string? path, CancellationToken ct);
        string Name { get; } // "Windows Search", "Everything"
        bool IsAvailable { get; }
    }
}
