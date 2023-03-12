using Files.App.Extensions;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class GroupByDateModifiedAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.DateModified;

		public override string Label { get; } = "DateModifiedLowerCase".GetLocalizedResource();
	}
}
