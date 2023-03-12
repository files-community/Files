using Files.App.Extensions;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class SortByTypeAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.FileType;

		public override string Label { get; } = "Type".GetLocalizedResource();
	}
}
