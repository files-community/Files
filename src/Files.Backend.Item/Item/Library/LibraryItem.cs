using System.Linq;

namespace Files.Backend.Item
{
    internal class LibraryItem : FileItem, ILibraryItem
    {
        public ILibrary Library { get; init; } = new Library(Enumerable.Empty<string>());
    }
}
