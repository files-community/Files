using Files.Sdk.Storage.LocatableStorage;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Core.Services
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

		/// <summary>
		/// Awaits the user input and picks single file from the file explorer dialog.
		/// </summary>
		/// <param name="filter">The filter to apply when picking files.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If successful and a file has been picked, returns <see cref="ILocatableFile"/>, otherwise null.</returns>
		Task<ILocatableFile?> PickSingleFileAsync(IEnumerable<string>? filter, CancellationToken cancellationToken = default);

		/// <summary>
		/// Awaits the user input and picks single folder from the file explorer dialog.
		/// </summary>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If successful and a folder has been picked, returns <see cref="ILocatableFolder"/>, otherwise null.</returns>
		Task<ILocatableFolder?> PickSingleFolderAsync(CancellationToken cancellationToken = default);
	}
}
