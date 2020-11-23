using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Files.Filesystem
{
    /// <summary>
    /// This class is used for grouping file properties into section so that it can be used as a ListView data source
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
