using Files.Helpers;
using System.Collections.ObjectModel;

namespace Files.Filesystem
{
    public class LibraryLocationItem : LocationItem
    {
        public string LibraryPath { get; }

        public ReadOnlyCollection<string> Paths { get; }

        public LibraryLocationItem(string path, string name, string defaultSaveFolder, string[] folders, bool isPinned)
        {
            Section = SectionType.Library;
            LibraryPath = path;
            Text = name;
            Path = defaultSaveFolder;
            Glyph = GlyphHelper.GetItemIcon(defaultSaveFolder);
            Paths = folders == null ? null : new ReadOnlyCollection<string>(folders);
            IsDefaultLocation = isPinned;
        }
    }
}
