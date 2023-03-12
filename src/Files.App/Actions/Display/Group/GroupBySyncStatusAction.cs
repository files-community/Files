using Files.App.Contexts;
using Files.App.Extensions;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class GroupBySyncStatusAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.SyncStatus;

		public override string Label { get; } = "SyncStatus".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType) => pageType is ContentPageTypes.CloudDrive;
	}
}
