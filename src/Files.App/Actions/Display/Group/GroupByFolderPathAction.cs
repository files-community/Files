using Files.App.Contexts;
using Files.App.Extensions;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class GroupByFolderPathAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.FolderPath;

		public override string Label { get; } = "FolderPath".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType) => pageType is ContentPageTypes.Library;
	}
}
