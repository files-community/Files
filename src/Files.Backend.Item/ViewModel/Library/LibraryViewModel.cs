using System.Collections.Immutable;

namespace Files.Backend.Item
{
    public class LibraryViewModel : ILibraryViewModel
    {
        private readonly ILibrary library;

        public string DefaultFolderPath => library.DefaultFolderPath;
        public ImmutableArray<string> FolderPaths => library.FolderPaths;

        public LibraryViewModel(ILibrary library) => this.library = library;
    }
}
