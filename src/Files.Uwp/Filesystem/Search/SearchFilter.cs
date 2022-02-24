using Files.Enums;
using System.Collections.Generic;

namespace Files.Filesystem.Search
{
    public interface ISearchFilter : ISearchContent
    {
        ISearchHeader Header { get; }

        IEnumerable<ISearchTag> Tags { get; }

        string ToAdvancedQuerySyntax();
    }

    public interface IMultiSearchFilter : ISearchFilter
    {
        SearchKeys Key { get; set; }
    }
}
