using Microsoft.UI.Xaml;

namespace Files.App.ViewModels.Properties
{
	/// <summary>
	/// This class is used for grouping file properties into sections so that it can be used as a grouped ListView data source
	/// </summary>
	public sealed partial class FilePropertySection : List<FileProperty>
	{
		public FilePropertySection(IEnumerable<FileProperty> items)
			: base(items)
		{
		}

		public Visibility Visibility { get; set; }

		public string? Key { get; set; }

		public string Title
			=> (Key ?? throw new InvalidOperationException("The property section key has not been initialized.")).GetLocalizedResource();

		public int Priority
			=> Key is null
				? throw new InvalidOperationException("The property section key has not been initialized.")
				: sectionPriority.TryGetValue(Key, out int priority) ? priority : 0;

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
