// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage.LocatableStorage;

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// A service that interacts with the system file explorer.
	/// </summary>
	public interface IFileExplorerService
	{
		/// <summary>
		/// Opens the app folder.
		/// </summary>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
		Task OpenAppFolderAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Opens provided <paramref name="folder"/> in file explorer.
		/// </summary>
		/// <param name="folder">The folder to open file explorer in.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
		Task OpenInFileExplorerAsync(ILocatableFolder folder, CancellationToken cancellationToken = default);
	}
}
