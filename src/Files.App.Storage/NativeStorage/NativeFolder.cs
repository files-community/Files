// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;
using Files.Core.Storage.DirectStorage;
using Files.Core.Storage.Enums;
using Files.Core.Storage.ExtendableStorage;
using Files.Core.Storage.Extensions;
using Files.Core.Storage.LocatableStorage;
using Files.Core.Storage.ModifiableStorage;
using Files.Core.Storage.MutableStorage;
using Files.Core.Storage.NestedStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.NativeStorage
{
	/// <inheritdoc cref="IFolder"/>
	public class NativeFolder : NativeStorable<DirectoryInfo>, ILocatableFolder, IModifiableFolder, IMutableFolder, IFolderExtended, INestedFolder, IDirectCopy, IDirectMove
    {
		public NativeFolder(DirectoryInfo directoryInfo, string? name = null)
		    : base(directoryInfo, name)
	    {
	    }

	    public NativeFolder(string path, string? name = null)
		    : this(new DirectoryInfo(path), name)
	    {
	    }

	    /// <inheritdoc/>
	    public virtual Task<INestedFile> GetFileAsync(string fileName, CancellationToken cancellationToken = default)
	    {
		    var path = System.IO.Path.Combine(Path, fileName);

		    if (!File.Exists(path))
			    throw new FileNotFoundException();

		    return Task.FromResult<INestedFile>(new NativeFile(path));
	    }

	    /// <inheritdoc/>
	    public virtual Task<INestedFolder> GetFolderAsync(string folderName, CancellationToken cancellationToken = default)
	    {
		    var path = System.IO.Path.Combine(Path, folderName);
		    if (!Directory.Exists(path))
			    throw new FileNotFoundException();

		    return Task.FromResult<INestedFolder>(new NativeFolder(path));
	    }

	    /// <inheritdoc/>
	    public virtual async IAsyncEnumerable<INestedStorable> GetItemsAsync(StorableKind kind = StorableKind.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	    {
		    if (kind == StorableKind.Files)
		    {
			    foreach (var item in Directory.EnumerateFiles(Path))
				    yield return new NativeFile(item);
		    }
		    else if (kind == StorableKind.Folders)
		    {
			    foreach (var item in Directory.EnumerateDirectories(Path))
				    yield return new NativeFolder(item);
		    }
		    else
		    {
			    foreach (var item in Directory.EnumerateFileSystemEntries(Path))
			    {
				    if (File.Exists(item))
					    yield return new NativeFile(item);
				    else
					    yield return new NativeFolder(item);
			    }
		    }

		    await Task.CompletedTask;
	    }

		/// <inheritdoc/>
		public virtual Task DeleteAsync(INestedStorable item, bool permanently = false, CancellationToken cancellationToken = default)
        {
            _ = permanently;

            if (item is ILocatableFile locatableFile)
            {
                File.Delete(locatableFile.Path);
            }
            else if (item is ILocatableFolder locatableFolder)
            {
                Directory.Delete(locatableFolder.Path, true);
            }
            else
                throw new ArgumentException($"Could not delete {item}.");

            return Task.CompletedTask;
        }

		/// <inheritdoc/>
		public virtual async Task<INestedStorable> CreateCopyOfAsync(INestedStorable itemToCopy, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			if (itemToCopy is IFile sourceFile)
			{
				if (itemToCopy is ILocatableFile sourceLocatableFile)
				{
					var newPath = System.IO.Path.Combine(Path, itemToCopy.Name);
					File.Copy(sourceLocatableFile.Path, newPath, overwrite);

					return new NativeFile(newPath);
				}

				var copiedFile = await CreateFileAsync(itemToCopy.Name, overwrite, cancellationToken);
				await sourceFile.CopyContentsToAsync(copiedFile, cancellationToken);

				return copiedFile;
			}
			else if (itemToCopy is IFolder sourceFolder)
			{
				// TODO: Implement folder copy
				_ = sourceFolder;
				throw new NotSupportedException();
			}

			throw new ArgumentException($"Could not copy type {itemToCopy.GetType()}");
		}

		/// <inheritdoc/>
		public virtual async Task<INestedStorable> MoveFromAsync(INestedStorable itemToMove, IModifiableFolder source, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			if (itemToMove is IFile sourceFile)
			{
				if (itemToMove is ILocatableFile sourceLocatableFile)
				{
					var newPath = System.IO.Path.Combine(Path, itemToMove.Name);
					File.Move(sourceLocatableFile.Path, newPath, overwrite);

					return new NativeFile(newPath);
				}
				else
				{
					var copiedFile = await CreateFileAsync(itemToMove.Name, overwrite, cancellationToken);
					await sourceFile.CopyContentsToAsync(copiedFile, cancellationToken);
					await source.DeleteAsync(itemToMove, true, cancellationToken);

					return copiedFile;
				}
			}
			else if (itemToMove is IFolder sourceFolder)
			{
				throw new NotImplementedException();
			}

			throw new ArgumentException($"Could not move type {itemToMove.GetType()}");
		}

		/// <inheritdoc/>
		public virtual async Task<INestedFile> CreateFileAsync(string desiredName, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			var path = System.IO.Path.Combine(Path, desiredName);
			if (overwrite || !File.Exists(path))
				await File.Create(path).DisposeAsync();

			return new NativeFile(path);
		}

		/// <inheritdoc/>
		public virtual Task<INestedFolder> CreateFolderAsync(string desiredName, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			var path = System.IO.Path.Combine(Path, desiredName);
			if (overwrite)
				Directory.Delete(path, true);

			_ = Directory.CreateDirectory(path);
			return Task.FromResult<INestedFolder>(new NativeFolder(path));
		}

		/// <inheritdoc/>
		public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IFolderWatcher>(new NativeFolderWatcher(this));
        }
    }
}
