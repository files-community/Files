using System.Collections.Immutable;

namespace Files.Backend.Item
{
    public interface ILibraryViewModel
    {
        string DefaultFolderPath { get; }
        ImmutableArray<string> FolderPaths { get; }
    }
}
