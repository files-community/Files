using Files.App.Extensions;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class SortByTagAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.FileTag;

		public override string Label { get; } = "FileTags".GetLocalizedResource();
	}
}
