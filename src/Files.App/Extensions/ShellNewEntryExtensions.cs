// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Windows.Storage;

namespace Files.App.Extensions
{
	public static class ShellNewEntryExtensions
	{
		public static async Task<List<ShellNewEntry>> GetNewContextMenuEntries()
		{
			var shellEntryList = new List<ShellNewEntry>();

			var entries = await SafetyExtensions.IgnoreExceptions(() => ShellNewMenuHelper.GetNewContextMenuEntries(), App.Logger);
			if (entries is not null)
			{
				shellEntryList.AddRange(entries);
			}

			return shellEntryList;
		}

		public static async Task<ShellNewEntry?> GetNewContextMenuEntryForType(string extension)
		{
			return await SafetyExtensions.IgnoreExceptions(() => ShellNewMenuHelper.GetNewContextMenuEntryForType(extension), App.Logger);
		}

		public static async Task<FilesystemResult<BaseStorageFile>> Create(this ShellNewEntry shellEntry, string filePath, IShellPage associatedInstance)
		{
			if (associatedInstance.ShellViewModel is not { } shellViewModel)
				return new FilesystemResult<BaseStorageFile>(null, FileSystemStatusCode.NotFound);

			var parentFolder = await shellViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(filePath));
			if (parentFolder.Result is { } folder)
			{
				return await Create(shellEntry, folder, filePath);
			}

			return new FilesystemResult<BaseStorageFile>(null, parentFolder.ErrorCode);
		}

		public static async Task<FilesystemResult<BaseStorageFile>> Create(this ShellNewEntry shellEntry, BaseStorageFolder parentFolder, string filePath)
		{
			var fileName = Path.GetFileName(filePath);
			if (string.IsNullOrEmpty(fileName))
				return new FilesystemResult<BaseStorageFile>(null, FileSystemStatusCode.NotFound);

			FilesystemResult<BaseStorageFile> createdFile;
			if (shellEntry.Template is null)
			{
				createdFile = await FilesystemTasks.WrapNullable(
					() => parentFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName).AsTask());
			}
			else
			{
				var templateFile = await FilesystemTasks.WrapNullable(
					() => StorageFileExtensions.DangerousGetFileFromPathAsync(shellEntry.Template));
				createdFile = templateFile.Result is { } sourceFile
					? await FilesystemTasks.WrapNullable(
						() => sourceFile.CopyAsync(parentFolder, fileName, NameCollisionOption.GenerateUniqueName).AsTask())
					: new FilesystemResult<BaseStorageFile>(null, templateFile.ErrorCode);
			}

			if (createdFile is { Result: { } result } && shellEntry.Data is not null)
			{
				// Calls unsupported OpenTransactedWriteAsync
				//await FileIO.WriteBytesAsync(createdFile.Result, shellEntry.Data);

				await result.WriteBytesAsync(shellEntry.Data);
			}

			return createdFile;
		}
	}
}
