// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Storage.Storables
{
	public partial interface IHomeFolder : IFolder
	{
		/// <summary>
		/// Gets quick access folders.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A list of the collection.</returns>
		public IAsyncEnumerable<IStorableChild> GetQuickAccessFolderAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets available logical drives.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A list of the collection.</returns>
		public IAsyncEnumerable<IStorableChild> GetLogicalDrivesAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets network locations(shortcuts).
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A list of the collection.</returns>
		public IAsyncEnumerable<IStorableChild> GetNetworkLocationsAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets recent files.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A list of the collection.</returns>
		public IAsyncEnumerable<IStorableChild> GetRecentFilesAsync(CancellationToken cancellationToken = default);
	}
}
