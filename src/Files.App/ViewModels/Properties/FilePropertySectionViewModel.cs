using Files.App.Extensions;
using Microsoft.UI.Xaml;
using System.Collections.Generic;

namespace Files.App.ViewModels.Properties
{
	/// <summary>
	/// This class is used for grouping file properties into sections so that it can be used as a grouped ListView data source
	/// </summary>
	public class FilePropertySectionViewModel : List<FilePropertyViewModel>
	{
		public FilePropertySectionViewModel(IEnumerable<FilePropertyViewModel> items)
			: base(items)
		{
		}

		public Visibility Visibility { get; set; }

		public string Key { get; set; }

		public string Title
			=> Key.GetLocalizedResource();

		public int Priority
			=> sectionPriority.ContainsKey(Key) ? sectionPriority[Key] : 0;

		/// <summary>
		/// This list sets the priorities for the sections
		/// </summary>
		private readonly Dictionary<string, int> sectionPriority = new Dictionary<string, int>()
		{
            // Core should always be last
            {"PropertySectionCore", 1}
		};
	}
}
