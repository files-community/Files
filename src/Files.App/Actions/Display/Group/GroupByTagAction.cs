using Files.App.Extensions;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class GroupByTagAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.FileTag;

		public override string Label { get; } = "FileTags".GetLocalizedResource();
	}
}
