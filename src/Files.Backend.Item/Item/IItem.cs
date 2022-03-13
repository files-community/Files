using System.ComponentModel;

namespace Files.Backend.Item
{
    public interface IItem : INotifyPropertyChanged
    {
        string Path { get; }
        string Name { get; }
    }
}
