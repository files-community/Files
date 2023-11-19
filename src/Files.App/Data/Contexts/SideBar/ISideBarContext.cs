// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contexts
{
	public interface ISideBarContext
	{
		/// <summary>
		/// The last right clicked item
		/// </summary>
		ILocatableSideBarItem? RightClickedItem { get; }

		/// <summary>
		/// Tells whether any item has been right clicked
		/// </summary>
		bool IsAnyItemRightClicked { get; }
	}
}
