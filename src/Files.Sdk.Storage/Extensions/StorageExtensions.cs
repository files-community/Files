using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Sdk.Storage.Extensions
{
	public static class StorageExtensions
	{
		public static async Task CopyContentsToAsync(this IFile source, IFile destination, CancellationToken cancellationToken = default)
		{
			// Internal Stream.GetCopyBufferSize() - https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/IO/Stream.cs
			const int DEFAULT_COPY_BUFFER_SIZE = 81920;

			using var sourceStream = await source.OpenStreamAsync(FileAccess.Read, FileShare.Read, cancellationToken);
			using var destinationStream = await destination.OpenStreamAsync(FileAccess.Read, FileShare.None, cancellationToken);

			await sourceStream.CopyToAsync(destinationStream, DEFAULT_COPY_BUFFER_SIZE, cancellationToken);
		}
	}
}
