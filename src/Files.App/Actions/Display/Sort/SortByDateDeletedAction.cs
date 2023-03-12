using Files.App.Contexts;
using Files.App.Extensions;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class SortByDateDeletedAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.DateDeleted;

		public override string Label { get; } = "DateDeleted".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType) => pageType is ContentPageTypes.RecycleBin;
	}
}
