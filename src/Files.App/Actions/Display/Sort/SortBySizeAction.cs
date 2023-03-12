using Files.App.Extensions;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class SortBySizeAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.Size;

		public override string Label { get; } = "Size".GetLocalizedResource();
	}
}
