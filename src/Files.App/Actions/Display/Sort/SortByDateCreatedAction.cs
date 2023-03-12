using Files.App.Extensions;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class SortByDateCreatedAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.DateCreated;

		public override string Label { get; } = "DateCreated".GetLocalizedResource();
	}
}
