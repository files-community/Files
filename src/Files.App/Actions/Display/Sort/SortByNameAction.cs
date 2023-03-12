using Files.App.Extensions;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class SortByNameAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.Name;

		public override string Label { get; } = "Name".GetLocalizedResource();
	}
}
