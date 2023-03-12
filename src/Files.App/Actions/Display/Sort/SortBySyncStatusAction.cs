using Files.App.Contexts;
using Files.App.Extensions;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class SortBySyncStatusAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.SyncStatus;

		public override string Label { get; } = "SyncStatus".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType) => pageType is ContentPageTypes.CloudDrive;
	}
}
