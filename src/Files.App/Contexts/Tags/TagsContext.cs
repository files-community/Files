using Files.App.ViewModels.Widgets;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Files.App.Contexts
{
	internal class TagsContext : ITagsContext
	{
		private static readonly IReadOnlyList<FileTagsItemViewModel> emptyTaggedItemsList = Enumerable.Empty<FileTagsItemViewModel>().ToImmutableList();

		private IEnumerable<FileTagsItemViewModel> taggedItems = emptyTaggedItemsList;
		public IEnumerable<FileTagsItemViewModel> TaggedItems
		{
			get => taggedItems;
			set
			{
				if (value is not null)
					taggedItems = value;
				else
					taggedItems = emptyTaggedItemsList;
			}
		}
	}
}
