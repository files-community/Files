// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Sdk.Storage.ExtendableStorage;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Sdk.Storage.Extensions
{
	public static partial class StorageExtensions
	{
		/// <inheritdoc cref="IFileExtended.OpenStreamAsync(FileAccess, FileShare, CancellationToken)"/>
		public static async Task<Stream> OpenStreamAsync(this IFile file, FileAccess access, FileShare share = FileShare.None, CancellationToken cancellationToken = default)
		{
			if (file is IFileExtended fileExtended)
				return await fileExtended.OpenStreamAsync(access, share, cancellationToken);

			// TODO: Check if the file inherits from ILockableStorable and ensure a disposable handle to it via Stream bridge
			return await file.OpenStreamAsync(access, cancellationToken);
		}

		/// <returns>If successful, returns a <see cref="Stream"/>, otherwise null.</returns>
		/// <inheritdoc cref="IFile.OpenStreamAsync"/>
		public static async Task<Stream?> TryOpenStreamAsync(this IFile file, FileAccess access, CancellationToken cancellationToken = default)
		{
			try
			{
				return await file.OpenStreamAsync(access, cancellationToken);
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <returns>If successful, returns a <see cref="Stream"/>, otherwise null.</returns>
		/// <inheritdoc cref="IFile.OpenStreamAsync"/>
		public static async Task<Stream?> TryOpenStreamAsync(this IFile file, FileAccess access, FileShare share = FileShare.None, CancellationToken cancellationToken = default)
		{
			try
			{
				return await OpenStreamAsync(file, access, share, cancellationToken);
			}
			catch (Exception)
			{
				return null;
			}
		}

		#region With Result

		/// <returns>Value is <see cref="IResult"/> depending on whether the stream was successfully opened on the file.</returns>
		/// <inheritdoc cref="IFile.OpenStreamAsync"/>
		public static async Task<IResult<Stream?>> OpenStreamWithResultAsync(this IFile file, FileAccess access, CancellationToken cancellationToken = default)
		{
			try
			{
				return new CommonResult<Stream?>(await file.OpenStreamAsync(access, cancellationToken));
			}
			catch (Exception ex)
			{
				return new CommonResult<Stream?>(ex);
			}
		}

		/// <returns>Value is <see cref="IResult"/> depending on whether the stream was successfully opened on the file.</returns>
		/// <inheritdoc cref="IFile.OpenStreamAsync"/>
		public static async Task<IResult<Stream?>> OpenStreamWithResultAsync(this IFile file, FileAccess access, FileShare share = FileShare.None, CancellationToken cancellationToken = default)
		{
			try
			{
				if (file is IFileExtended fileExtended)
					return new CommonResult<Stream?>(await fileExtended.OpenStreamAsync(access, share, cancellationToken));

				// TODO: Check if the file inherits from ILockableStorable and ensure a disposable handle to it via Stream bridge
				return new CommonResult<Stream?>(await file.OpenStreamAsync(access, cancellationToken));
			}
			catch (Exception ex)
			{
				return new CommonResult<Stream?>(ex);
			}
		}

		#endregion
		/// <summary>
		/// Copies contents of <paramref name="source"/> to <paramref name="destination"/> overwriting existing data.
		/// </summary>
		/// <param name="source">The source file to copy from.</param>
		/// <param name="destination">The destination file to copy to.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
		public static async Task CopyContentsToAsync(this IFile source, IFile destination, CancellationToken cancellationToken = default)
		{
			await using var sourceStream = await source.OpenStreamAsync(FileAccess.Read, cancellationToken);
			await using var destinationStream = await destination.OpenStreamAsync(FileAccess.Read, cancellationToken);
			await sourceStream.CopyToAsync(destinationStream, cancellationToken);
		}
	}
}
