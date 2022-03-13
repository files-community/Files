using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Files.Backend.Item
{
    public class Library : ILibrary
    {
        public string DefaultFolderPath { get; init; } = string.Empty;

        public ImmutableArray<string> FolderPaths { get; }

        public Library(IEnumerable<string> folderPaths)
            => FolderPaths = ImmutableArray.Create(folderPaths.ToArray());
    }
}
