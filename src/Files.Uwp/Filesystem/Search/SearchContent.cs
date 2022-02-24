using System.ComponentModel;

namespace Files.Filesystem.Search
{
    public interface ISearchContent : INotifyPropertyChanged
    {
        bool IsEmpty { get; }

        void Clear();
    }
}
