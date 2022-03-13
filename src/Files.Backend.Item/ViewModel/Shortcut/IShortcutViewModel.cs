namespace Files.Backend.Item
{
    public interface IShortcutViewModel
    {
        bool IsExecutable { get; }
        bool IsSymbolicLink { get; }
        bool IsUrl { get; }

        string TargetPath { get; }
        string Arguments { get; }
        string WorkingDirectory { get; }
    }
}
