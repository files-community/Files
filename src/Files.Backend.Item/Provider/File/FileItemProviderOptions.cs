using System;

namespace Files.Backend.Item
{
    [Flags]
    public enum FileItemProviderOptions : ushort
    {
        None,
        IncludeHiddenItems,
        IncludeSystemItems,
        IncludeUnindexedItems,
    }
}
