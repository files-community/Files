namespace Files.Backend.Item
{
    internal class ShortcutItem : FileItem, IShortcutItem
    {
        public IShortcut Shortcut { get; init; } = new Shortcut();
    }
}
