using Files.Core.Storage;

namespace Files.Core.Services
{
	/// <summary>
	/// A service that manages actions associated with system Start Menu.
	/// </summary>
	public interface IStartMenuService
	{
		// TODO(s)
		[Obsolete("Use IsPinnedAsync instead. This method is used for a workaround in ListedItem class to avoid major refactoring.")]
		bool IsPinned(string folderPath);

		/// <summary>
		/// Checks if the provided <paramref name="folder"/> is pinned to the Start Menu.
		/// </summary>
		/// <param name="folder">The folder to check for.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If the folder is pinned, returns true; otherwise false.</returns>
		Task<bool> IsPinnedAsync(IFolder folder);

		/// <summary>
		/// Adds the provided <paramref name="folder"/> to the pinned items list in the Start Menu.
		/// </summary>
		/// <param name="folder">The folder to pin.</param>
		/// <param name="displayName">The optional name to use when pinning an item.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
		Task PinAsync(IFolder folder, string? displayName = null);

		/// <summary>
		/// Removes the provided <paramref name="folder"/> from the pinned items list in the Start Menu.
		/// </summary>
		/// <param name="folder">The folder to unpin.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
		Task UnpinAsync(IFolder folder);
	}
}
