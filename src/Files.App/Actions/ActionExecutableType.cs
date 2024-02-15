// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	/// <summary>
	/// Defines constants that specifies what context the action can be executed through.
	/// </summary>
	public enum ActionExecutableType
	{
		/// <summary>
		/// The action is executable in <see cref="IDisplayPageContext"/>.
		/// </summary>
		DisplayPageContext,

		/// <summary>
		/// The action is executable in <see cref="IHomePageContext"/>.
		/// </summary>
		HomePageContext,

		/// <summary>
		/// The action is executable in <see cref="ISidebarContext"/>.
		/// </summary>
		SidebarContext
	}
}
