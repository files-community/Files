using Files.App.Filesystem;

namespace Files.App.Contexts
{
	public interface ISidebarContext
	{
		/// <summary>
		/// The last sidebar right clicked item
		/// </summary>
		INavigationControlItem? RightClickedItem { get; }

		/// <summary>
		/// Tells whether any item has been right clicked
		/// </summary>
		bool IsAnyItemRightClicked { get; }

		/// <summary>
		/// Tells whether right clicked item is a favorite item
		/// </summary>
		bool IsFavoriteItem { get; }

		/// <summary>
		/// Tells whether right clicked item is a drive
		/// </summary>
		bool IsDriveItem { get; }

		/// <summary>
		/// Tells wheter right clicked item is a drive and is pinned
		/// </summary>
		bool IsDriveItemPinned { get; }
	}
}
