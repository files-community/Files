// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Enums
{
	/// <summary>
	/// Defines constants that specify item type of ContextMenuFlyout on Windows.
	/// </summary>
	public enum ContextMenuFlyoutItemType
	{
		/// <summary>
		/// Button type.
		/// </summary>
		Item,

		/// <summary>
		/// Separator type.
		/// </summary>
		Separator,

		/// <summary>
		/// Toggle Button type.
		/// </summary>
		Toggle,

		/// <summary>
		/// Radio button type, of which one per <see cref="Files.App.Data.Models.ContextMenuFlyoutItemViewModel.GroupName"/> is checked.
		/// </summary>
		Radio,

		/// <summary>
		/// Split Button type.
		/// </summary>
		SplitButton
	}
}
