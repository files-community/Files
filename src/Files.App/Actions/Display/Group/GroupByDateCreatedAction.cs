using Files.App.Extensions;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class GroupByDateCreatedAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.DateCreated;

		public override string Label { get; } = "DateCreated".GetLocalizedResource();
	}
}
