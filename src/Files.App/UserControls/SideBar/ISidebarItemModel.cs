// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.SideBar
{
	public interface ISideBarItemModel : INotifyPropertyChanged
	{
		/// <summary>
		/// Gets the children of this item that will be rendered as child elements of the <see cref="SideBarItem"/>
		/// </summary>
		object? Children { get; }

		/// <summary>
		/// Gets the icon source used to generate the icon for the <see cref="SideBarItem"/>
		/// </summary>
		IconSource? IconSource { get; }

		/// <summary>
		/// Gets the item decorator for the given item.
		/// </summary>
		FrameworkElement? ItemDecorator { get => null; }

		/// <summary>
		/// Gets or sets a value that indicates whether the <see cref="SideBarItem"/> is expanded
		/// and the children are visible, or if it is collapsed and children are not visible.
		/// </summary>
		bool IsExpanded { get; set; }

		/// <summary>
		/// Gets the text of this item that will be rendered as the label of the SidebarItem.
		/// </summary>
		string Text { get; }

		/// <summary>
		/// Gets the tooltip used when hovering over this item in the sidebar.
		/// </summary>
		object ToolTip { get; }
	}
}
