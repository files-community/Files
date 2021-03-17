using System.Collections.ObjectModel;

namespace Files.Filesystem
{
    public class LibraryItem : LocationItem
    {
        public string LibraryPath { get; }

        public ReadOnlyCollection<string> Paths { get; }

        public LibraryItem(string path, string name, string defaultSaveFolder, string[] folders, bool isPinned)
        {
            Section = SectionType.Library;
            LibraryPath = path;
            Text = name;
            Path = defaultSaveFolder;
            Paths = folders == null ? null : new ReadOnlyCollection<string>(folders);
            IsDefaultLocation = isPinned;
        }
    }
}
