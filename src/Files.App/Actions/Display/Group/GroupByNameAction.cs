using Files.App.Extensions;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class GroupByNameAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.Name;

		public override string Label { get; } = "Name".GetLocalizedResource();
	}
}
