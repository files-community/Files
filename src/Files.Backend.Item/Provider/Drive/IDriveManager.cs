using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Files.Backend.Item
{
    public interface IDriveManager : INotifyPropertyChanged, IDisposable
    {
        ReadOnlyObservableCollection<IDriveItem> Drives { get; }
    }
}
