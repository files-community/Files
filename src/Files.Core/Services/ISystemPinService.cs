using Files.Core.Storage;

namespace Files.Core.Services
{
	/// <summary>
	/// A service that allows to manage pinning and unpinning items in common OS places.
	/// </summary>
	public interface ISystemPinService
	{
		// TODO(s)
		[Obsolete("Use IsPinnedAsync instead. This method is used for a workaround in ListedItem class to avoid major refactoring.")]
		bool IsPinned(string folderPath);

		Task<bool> IsPinnedAsync(IFolder folder);

		Task PinAsync(IFolder folder);

		Task UnpinAsync(IFolder folder);
	}
}
