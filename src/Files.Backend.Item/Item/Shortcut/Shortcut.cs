namespace Files.Backend.Item
{
    internal class Shortcut : IShortcut
    {
        public ShortcutTypes ShortcutType { get; init; } = ShortcutTypes.Unknown;

        public string TargetPath { get; init; } = string.Empty;
        public string Arguments { get; init; } = string.Empty;
        public string WorkingDirectory { get; init; } = string.Empty;
    }
}
