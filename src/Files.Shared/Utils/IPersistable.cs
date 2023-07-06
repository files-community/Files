using System.Threading;
using System.Threading.Tasks;

namespace Files.Shared.Utils
{
	/// <summary>
	/// Allows for data to be saved and loaded from a persistence store.
	/// </summary>
	public interface IPersistable
	{
		/// <summary>
		/// Asynchronously loads persisted data into memory.
		/// </summary>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If successful returns true, otherwise false.</returns>
		Task<bool> LoadAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously saves data stored in memory.
		/// </summary>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If successful returns true, otherwise false.</returns>
		Task<bool> SaveAsync(CancellationToken cancellationToken = default);
	}
}
