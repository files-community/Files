using Windows.UI.Xaml;

namespace Files.Filesystem
{
    public class PropertiesData
    {
        public string Text { get; set; } = "Test:";
        public string Property { get; set; }
        public object Data { get; set; }
        public Visibility Visibility { get; set; } = Visibility.Visible;

        public PropertiesData(string property, string text)
        {
            Property = property;
            Text = text;
        }

        public PropertiesData(string property, object data)
        {
            Property = property;
            Data = data;
        }
    }
}