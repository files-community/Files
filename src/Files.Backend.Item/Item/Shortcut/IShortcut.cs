namespace Files.Backend.Item
{
    public interface IShortcut
    {
        ShortcutTypes ShortcutType { get; }

        string TargetPath { get; }
        string Arguments { get; }
        string WorkingDirectory { get; }
    }
}
