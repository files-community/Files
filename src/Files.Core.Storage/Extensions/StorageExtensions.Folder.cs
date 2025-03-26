// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.Core.Storage.Extensions
{
	public static partial class StorageExtensions
	{
		#region Without Result

		/// <returns>If file was found, returns the requested <see cref="OwlCore.Storage.IFile"/>, otherwise null.</returns>
		public static async Task<IFile?> TryGetFileByNameAsync(this IFolder folder, string fileName, CancellationToken cancellationToken = default)
		{
			try
			{
				return await folder.GetFirstByNameAsync(fileName, cancellationToken) switch
				{
					IChildFile childFile => childFile,
					_ => throw new InvalidOperationException("The provided name does not point to a file.")
				};
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <returns>If folder was found, returns the requested <see cref="IFolder"/>, otherwise null.</returns>
		public static async Task<IFolder?> TryGetFolderByNameAsync(this IFolder folder, string folderName, CancellationToken cancellationToken = default)
		{
			try
			{
				return await folder.GetFirstByNameAsync(folderName, cancellationToken) switch
				{
					IChildFolder childFolder => childFolder,
					_ => throw new InvalidOperationException("The provided name does not point to a folder.")
				};
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <returns>If file was created, returns the requested <see cref="IFile"/>, otherwise null.</returns>
		/// <inheritdoc cref="IModifiableFolder.CreateFileAsync"/>
		public static async Task<IFile?> TryCreateFileAsync(this IModifiableFolder folder, string desiredName, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			try
			{
				return await folder.CreateFileAsync(desiredName, overwrite, cancellationToken);
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <returns>If folder was created, returns the requested <see cref="IFolder"/>, otherwise null.</returns>
		/// <inheritdoc cref="IModifiableFolder.CreateFolderAsync"/>
		public static async Task<IFolder?> TryCreateFolderAsync(this IModifiableFolder folder, string desiredName, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			try
			{
				return await folder.CreateFolderAsync(desiredName, overwrite, cancellationToken);
			}
			catch (Exception)
			{
				return null;
			}
		}

		#endregion
	}
}
