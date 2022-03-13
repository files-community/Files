namespace Files.Backend.Item
{
    public interface IShortcutItem : IFileItem
    {
        IShortcut Shortcut { get; }
    }
}
