namespace Files.Backend.Item
{
    public interface ILibraryItem : IFileItem
    {
        ILibrary Library { get; }
    }
}
