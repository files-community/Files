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

		/// <summary>
		/// Determines whether the SidebarItem is expanded and the children are visible 
		/// or if it is collapsed and children are not visible.
		/// </summary>
		bool IsExpanded { get; set; }

		/// <summary>
		/// Optional path associated with this sidebar item for drag/drop scenarios.
		/// </summary>
		string? Path { get; }

		/// <summary>
		/// Renders as expandable even when Children is empty (children load lazily on first expansion).
		/// </summary>
		bool HasUnrealizedChildren => false;

		/// <summary>
		/// Expansion participant that keeps the regular row appearance (icon + normal text) instead of the section-header style.
		/// </summary>
		bool IsLeafWithChildren => false;
	}
}
