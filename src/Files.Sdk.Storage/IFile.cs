using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Sdk.Storage
{
	/// <summary>
	/// Represents a file on the file system.
	/// </summary>
	public interface IFile : IStorable
	{
		/// <summary>
		/// Opens and returns a stream to the file.
		/// </summary>
		/// <param name="access">The file access to open the file with.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If successful, returns a <see cref="Stream"/>, otherwise null.</returns>
		Task<Stream> OpenStreamAsync(FileAccess access, FileShare share = FileShare.None, CancellationToken cancellationToken = default);
	}
}
