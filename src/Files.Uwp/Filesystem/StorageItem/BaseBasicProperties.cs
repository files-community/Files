using System;

namespace Files.Uwp.Filesystem.StorageItems
{
    public class BaseBasicProperties : BaseStorageItemExtraProperties
    {
        public virtual ulong Size { get => 0; }

        public virtual DateTimeOffset ItemDate { get => DateTimeOffset.Now; }
        public virtual DateTimeOffset DateModified { get => DateTimeOffset.Now; }
    }
}
