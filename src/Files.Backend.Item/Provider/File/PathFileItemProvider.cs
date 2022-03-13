using Files.Backend.Item.Tools;
using System.Collections.Generic;
using System.Threading;
using static Files.Backend.Item.Tools.NativeFindStorageItemHelper;
using IO = System.IO;

namespace Files.Backend.Item
{
    public class PathFileItemProvider : IFileItemProvider
    {
        public string ParentPath { get; set; } = string.Empty;

        public bool IncludeHiddens { get; set; } = false;
        public bool IncludeSystems { get; set; } = false;

        public bool ShowFolderSize { get; set; } = false;

        public CancellationToken CancellationToken { get; set; }

        IAsyncEnumerable<IItem> IItemProvider.ProvideItems() => ProvideItems();
        public async IAsyncEnumerable<IFileItem> ProvideItems()
        {
            yield return null;
        }

        private IFileItem BuildFileItem(string path, WIN32_FIND_DATA data)
        {
            return new FileItem
            {
                Path = path,
                Name = data.cFileName,
                FileAttribute = ((IO.FileAttributes)data.dwFileAttributes).ToFileAttribute(),
                Size = data.GetSize(),
                DateCreated = data.ftCreationTime.ToDateTime(),
                DateModified = data.ftLastWriteTime.ToDateTime(),
                DateAccessed = data.ftLastAccessTime.ToDateTime(),
            };
        }
        private IFileItem BuildShortcutItem(string path, WIN32_FIND_DATA data)
        {
            return new ShortcutItem
            {
                Path = path,
                Name = data.cFileName,
                FileAttribute = ((IO.FileAttributes)data.dwFileAttributes).ToFileAttribute(),
                Size = data.GetSize(),
                DateCreated = data.ftCreationTime.ToDateTime(),
                DateModified = data.ftLastWriteTime.ToDateTime(),
                DateAccessed = data.ftLastAccessTime.ToDateTime(),
                Shortcut = new Shortcut { },
            };
        }
    }
}

