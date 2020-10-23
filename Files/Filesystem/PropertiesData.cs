using Microsoft.Toolkit.Mvvm.ComponentModel;
using SQLitePCL;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Files.Filesystem
{
    public class PropertiesData : ObservableObject
    {
        public string Name { get; set; } = "Test:";
        public string Property { get; set; }
        public string Section { get; set; }
        public object Value { get; set; }
        public IValueConverter Converter { get; set; }
        public bool IsReadOnly { get; set; }

        public Visibility Visibility { get; set; } = Visibility.Visible;

        public PropertiesData(string property, string name)
        {
            Property = property;
            Name= name;
        }

        public PropertiesData(string property)
        {
            Property = property;
        }

        public PropertiesData()
        {

        }
    }
}