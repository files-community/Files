using Files.App.Extensions;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class SortByDateModifiedAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.DateModified;

		public override string Label { get; } = "DateModifiedLowerCase".GetLocalizedResource();
	}
}
