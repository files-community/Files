using Files.App.Extensions;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class GroupBySizeAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.Size;

		public override string Label { get; } = "Size".GetLocalizedResource();
	}
}
