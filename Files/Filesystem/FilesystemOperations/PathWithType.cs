using System;
using Windows.Storage;

namespace Files.Filesystem
{
    public class PathWithType : IDisposable
    {
        #region Public Properties

        public string Path { get; private set; }

        public FilesystemItemType ItemType { get; private set; }

        #endregion Public Properties

        #region Constructor

        public PathWithType(string path, FilesystemItemType itemType)
        {
            Path = path;
            ItemType = itemType;
        }

        #endregion Constructor

        #region Operators

        public static explicit operator string(PathWithType pathWithType) => pathWithType.Path;

        public static explicit operator FilesystemItemType(PathWithType pathWithType) => pathWithType.ItemType;

        public static explicit operator PathWithType(StorageFile storageFile)
        {
            return new PathWithType(storageFile.Path, storageFile.IsOfType(StorageItemTypes.File) ? FilesystemItemType.File : FilesystemItemType.Directory);
        }

        public static explicit operator PathWithType(StorageFolder storageFolder)
        {
            return new PathWithType(storageFolder.Path, storageFolder.IsOfType(StorageItemTypes.File) ? FilesystemItemType.File : FilesystemItemType.Directory);
        }

        #endregion Operators

        #region Override

        public override string ToString()
        {
            return Path;
        }

        #endregion Override

        #region IDisposable

        public void Dispose()
        {
            Path ??= string.Empty;

            Path = null;
        }

        #endregion IDisposable
    }
}