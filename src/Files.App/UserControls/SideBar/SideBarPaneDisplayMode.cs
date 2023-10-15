// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.UserControls.SideBar
{
	/// <summary>
	/// Defines constants that specify how the <see cref="SideBarView"/> pane is shown.
	/// </summary>
	public enum SideBarPaneDisplayMode
	{
		/// <summary>
		/// The pane is shown on the left side of the control.
		/// Only the pane menu button is shown by default.
		/// </summary>
		Minimal,

		/// <summary>
		/// The pane is shown on the left side of the control.
		/// Only the pane icons are shown by default.
		/// </summary>
		Compact,

		/// <summary>
		/// The pane is shown on the left side of the control in its fully open state.
		/// </summary>
		Expanded
	}
}
