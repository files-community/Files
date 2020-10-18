using Microsoft.Toolkit.Mvvm.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Files.Filesystem
{
    public class PropertiesData : ObservableObject
    {
        public string Name { get; set; } = "Test:";
        public string Property { get; set; }
        public object Data { get; set; }
        public IValueConverter Converter { get; set; }
        public bool IsReadOnly { get; set; }

        public Visibility Visibility { get; set; } = Visibility.Visible;

        public PropertiesData(string property, string name)
        {
            Property = property;
            Name= name;
        }

        public PropertiesData(string property, object data)
        {
            Property = property;
            Data = data;
        }

        public Properties()
        {

        }
    }
}