// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	/// <summary>
	/// Defines constants that specifies the <see cref="IRichCommand"/> executable type.
	/// </summary>
	public enum ExecutableContextType
	{
		/// <summary>
		/// Unknown executable context type.
		/// </summary>
		None,

		/// <summary>
		/// executable context type is <see cref="IContentPageContext"/>.
		/// </summary>
		ContentPageContext,

		/// <summary>
		/// executable context type is <see cref="IHomePageContext"/>.
		/// </summary>
		HomePageContext,

		/// <summary>
		/// executable context type is <see cref="ISidebarContext"/>.
		/// </summary>
		SidebarContext,
	}
}
