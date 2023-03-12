using Files.App.Extensions;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class GroupByNoneAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.None;

		public override string Label { get; } = "None".GetLocalizedResource();
	}
}
