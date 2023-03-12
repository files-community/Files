using Files.App.Contexts;
using Files.App.Extensions;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class SortByOriginalFolderAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.OriginalFolder;

		public override string Label { get; } = "OriginalFolder".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType) => pageType is ContentPageTypes.CloudDrive;
	}
}
