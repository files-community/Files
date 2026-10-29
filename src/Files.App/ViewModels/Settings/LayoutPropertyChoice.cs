// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.ViewModels.Settings
{
	/// <summary>
	/// Represents a property that folders can be sorted or grouped by.
	/// </summary>
	/// <param name="BuiltInOption">The built-in sort or group option, or null when the choice uses a Shell property or Git column.</param>
	/// <param name="PropertyId">The Shell property or Git column, used when the choice isn't built in.</param>
	public sealed record LayoutPropertyChoice(string DisplayName, int? BuiltInOption, string PropertyId)
	{
		public override string ToString()
			=> DisplayName;
	}
}
