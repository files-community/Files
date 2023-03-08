using Files.App.ViewModels.Widgets;
using System.Collections.Generic;

namespace Files.App.Contexts
{
	public interface ITagsContext
	{
		IEnumerable<FileTagsItemViewModel> TaggedItems { get; set; }
	}
}
