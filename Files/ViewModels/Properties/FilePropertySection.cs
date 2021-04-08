using Microsoft.Toolkit.Uwp;
using System.Collections.Generic;
using Windows.UI.Xaml;

namespace Files.ViewModels.Properties
{
    /// <summary>
    /// This class is used for grouping file properties into sections so that it can be used as a grouped ListView data source
    /// </summary>
    public class FilePropertySection : List<FileProperty>
    {
        /// <summary>
        /// This list sets the priorities for the sections
        /// </summary>
        private readonly Dictionary<string, int> sectionPriority = new Dictionary<string, int>()
        {
            // Core should always be last
            {"PropertySectionCore", 1}
        };

        public FilePropertySection(IEnumerable<FileProperty> items) : base(items)
        {
        }

        public string Key { get; set; }
        public int Priority => sectionPriority.ContainsKey(Key) ? sectionPriority[Key] : 0;
        public string Title => Key.GetLocalized();
        public Visibility Visibility { get; set; }
    }
}