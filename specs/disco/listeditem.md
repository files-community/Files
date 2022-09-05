# StorableViewModel

## Code examples
```csharp
    public abstract class StorableViewModel : ObservableObject
    {
        public IStorable Storable { get; }

        public StorageItemViewModel(IStorable storable)
        {
            this.storable = storable;
        }
    }
```

```csharp
    public class StandardItemViewModel : StorableViewModel
    {
        private long _size;

        public long Size
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }

        // ... DateModified, DateCreated as well

        public IStoragePropertiesCollection Properties { get; }

        public StandardItemViewModel(IStorable storable, IStoragePropertiesCollection properties) : base(storable)
        { 
            this.Properties = properties;
        }

        public void 
    }
```

Adds an `ItemPropertiesKind` enum:

```csharp
public enum ItemPropertiesKind
{
    Standard,
    Extended
}
```

Amends the IStoragePropertiesCollection interface to include an enum parameter on `GetStoragePropertiesAsync` method:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SecureFolderFS.Sdk.Storage.StorageProperties
{

    public interface IStoragePropertiesCollection
    {
        DateTime DateCreated { get; }

    
        DateTime DateModified { get; }

 
        ulong? Size { get; }

       
        IAsyncEnumerable<IStorageProperty> GetStoragePropertiesAsync(ItemPropertiesKind propertiesKind = ItemPropertiesKind.Standard, CancellationToken cancellationToken = default);
    }
}
```

