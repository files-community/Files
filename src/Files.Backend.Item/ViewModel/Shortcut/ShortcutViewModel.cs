namespace Files.Backend.Item
{
    internal class ShortcutViewModel : IShortcutViewModel
    {
        private readonly IShortcut shortcut;

        public bool IsExecutable => shortcut.ShortcutType is ShortcutTypes.Executable;
        public bool IsSymbolicLink => shortcut.ShortcutType is ShortcutTypes.SymbolicLink;
        public bool IsUrl => shortcut.ShortcutType is ShortcutTypes.Url;

        public string TargetPath => shortcut.TargetPath;
        public string Arguments => shortcut.Arguments;
        public string WorkingDirectory => shortcut.WorkingDirectory;

        public ShortcutViewModel(IShortcut shortcut) => this.shortcut = shortcut;
    }
}
