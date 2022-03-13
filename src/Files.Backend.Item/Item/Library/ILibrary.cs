using System.Collections.Immutable;

namespace Files.Backend.Item
{
    public interface ILibrary
    {
        string DefaultFolderPath { get; }
        ImmutableArray<string> FolderPaths { get; }
    }
}
