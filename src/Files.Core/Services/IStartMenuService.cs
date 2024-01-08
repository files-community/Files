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
		bool IsPinned(string itemPath);

		/// <summary>
		/// Checks if the provided <paramref name="storable"/> is pinned to the Start Menu.
		/// </summary>
		/// <param name="storable">The <see cref="IStorable"/> object to check for.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If the folder is pinned, returns true; otherwise false.</returns>
		Task<bool> IsPinnedAsync(IStorable storable);

		/// <summary>
		/// Adds the provided <paramref name="storable"/> to the pinned items list in the Start Menu.
		/// </summary>
		/// <param name="storable">The <see cref="IStorable"/> object to pin.</param>
		/// <param name="displayName">The optional name to use when pinning an item.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
		Task PinAsync(IStorable storable, string? displayName = null);

		/// <summary>
		/// Removes the provided <paramref name="storable"/> from the pinned items list in the Start Menu.
		/// </summary>
		/// <param name="storable">The <see cref="IStorable"/> object to unpin.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
		Task UnpinAsync(IStorable storable);
	}
}
