// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.ViewModels.Settings
{
	/// <summary>
	/// Represents a property that folders can be sorted or grouped by.
	/// </summary>
	/// <param name="BuiltInOption">The built-in sort or group option, or null when the choice uses a Shell property or Git column.</param>
	/// <param name="PropertyId">The Shell property or Git column, used when the choice isn't built in.</param>
	/// <param name="DateUnit">The unit to group a date by, when the choice groups by one.</param>
	public sealed record LayoutPropertyChoice(string DisplayName, int? BuiltInOption, string PropertyId, GroupByDateUnit? DateUnit = null)
	{
		/// <summary>
		/// Gets the column of the built-in option, which decides the group the choice is listed in.
		/// </summary>
		public string? ColumnId { get; init; }

		public override string ToString()
			=> DisplayName;
	}
}
