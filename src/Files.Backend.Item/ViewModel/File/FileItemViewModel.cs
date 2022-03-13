using System;

namespace Files.Backend.Item
{
    internal class FileItemViewModel : IFileItemViewModel
    {
        private readonly IFileItem item;

        public string Path => item.Path;
        public string Name => item.Path;

        public bool IsArchive => item.FileAttribute.HasFlag(FileAttributes.Archive);
        public bool IsCompressed => item.FileAttribute.HasFlag(FileAttributes.Compressed);
        public bool IsDevice => item.FileAttribute.HasFlag(FileAttributes.Device);
        public bool IsDirectory => item.FileAttribute.HasFlag(FileAttributes.Directory);
        public bool IsEncrypted => item.FileAttribute.HasFlag(FileAttributes.Encrypted);
        public bool IsHidden => item.FileAttribute.HasFlag(FileAttributes.Hidden);
        public bool IsOffline => item.FileAttribute.HasFlag(FileAttributes.Offline);
        public bool IsReadOnly => item.FileAttribute.HasFlag(FileAttributes.ReadOnly);
        public bool IsSystem => item.FileAttribute.HasFlag(FileAttributes.System);
        public bool IsTemporary => item.FileAttribute.HasFlag(FileAttributes.Temporary);

        public ByteSize Size => item.Size;

        public DateTime DateCreated => item.DateCreated;
        public DateTime DateModified => item.DateModified;
        public DateTime DateAccessed => item.DateAccessed;

        public bool IsShortcutItem => item is IShortcutItem;
        public bool IsExecutableShortcutItem => Shortcut?.IsExecutable ?? false;
        public bool IsSymbolicLinkShortcutItem => Shortcut?.IsSymbolicLink ?? false;
        public bool IsUrlShortcutItem => Shortcut?.IsUrl ?? false;
        public bool IsLibraryItem => item is ILibraryItem;
        public bool IsFtpItem => item is IFtpItem;
        public bool IsZipItem => item is IZipItem;

        public IShortcutViewModel? Shortcut { get; }
        public ILibraryViewModel? Library { get; }

        public FileItemViewModel(IFileItem item)
        {
            this.item = item;

            if (item is IShortcutItem shortcutItem)
            {
                Shortcut = new ShortcutViewModel(shortcutItem.Shortcut);
            }
            if (item is ILibraryItem libraryItem)
            {
                Library = new LibraryViewModel(libraryItem.Library);
            }
        }
    }
}
