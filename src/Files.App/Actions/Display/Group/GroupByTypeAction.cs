using Files.App.Extensions;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class GroupByTypeAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.FileType;

		public override string Label { get; } = "Type".GetLocalizedResource();
	}
}
