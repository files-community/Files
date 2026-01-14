// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public interface ISidebarItemModel : INotifyPropertyChanged
	{
		/// <summary>
		/// The children of this item that will be rendered as child elements of the SidebarItem
		/// </summary>
		object? Children { get; }
	}
}
