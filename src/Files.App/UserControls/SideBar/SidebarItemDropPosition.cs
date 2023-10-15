// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.UserControls.SideBar
{
	/// <summary>
	/// Defines constants that specify the position of the item that was dropped on the <see cref="SideBarItem"/>.
	/// </summary>
	public enum SideBarItemDropPosition
	{
		/// <summary>
		/// The item was dropped on the top of the <see cref="SideBarItem"/> indicating it should be moved/inserted above this <see cref="SideBarItem"/>.
		/// </summary>
		Top,

		/// <summary>
		/// The item was dropped on the bottom of the <see cref="SideBarItem"/> indicating it should be moved/inserted below this <see cref="SideBarItem"/>.
		/// </summary>
		Bottom,

		/// <summary>
		/// The item was dropped on the center of the <see cref="SideBarItem"/> indicating it should be moved/inserted as a child of this <see cref="SideBarItem"/>.
		/// </summary>
		Center
	}
}
