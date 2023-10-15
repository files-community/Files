// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.UserControls.SideBar
{
	/// <summary>
	/// The display mode of the <see cref="SideBarView"/>
	/// </summary>
	public enum SideBarDisplayMode
	{
		/// <summary>
		/// The sidebar is hidden and moves in from the side when the <see cref="SideBarView.IsPaneOpen"/> is set to true.
		/// </summary>
		Minimal,

		/// <summary>
		/// Only the icons of the top most sections are visible.
		/// </summary>
		Compact,

		/// <summary>
		/// The sidebar is expanded and items can also be expanded.
		/// </summary>
		Expanded
	}
}
