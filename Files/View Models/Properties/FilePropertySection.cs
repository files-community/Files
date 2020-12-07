using System.Collections.Generic;
using Windows.UI.Xaml;

namespace Files.View_Models.Properties
{
    /// <summary>
    /// This class is used for grouping file properties into sections so that it can be used as a grouped ListView data source
    /// </summary>
    public class FilePropertySection : List<FileProperty>
    {
        public FilePropertySection(IEnumerable<FileProperty> items) : base(items)
        {
        }

        public Visibility Visibility { get; set; }

        public string Key { get; set; }
    }
}
