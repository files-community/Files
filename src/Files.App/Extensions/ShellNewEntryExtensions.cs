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
			var shellViewModel = associatedInstance.GetRequiredShellViewModel();

			var parentFolder = await shellViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(filePath));
			if (parentFolder)
			{
				return await Create(shellEntry, parentFolder.Result, filePath);
			}

			return new FilesystemResult<BaseStorageFile>(null, parentFolder.ErrorCode);
		}

		public static async Task<FilesystemResult<BaseStorageFile>> Create(this ShellNewEntry shellEntry, BaseStorageFolder? parentFolder, string filePath)
		{
			var fileName = Path.GetFileName(filePath)!;

			FilesystemResult<BaseStorageFile> createdFile;
			if (shellEntry.Template is null)
			{
				createdFile = await FilesystemTasks.WrapNullable(
					() => (parentFolder
						?? throw new InvalidOperationException("The parent folder could not be resolved."))
						.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName).AsTask());
			}
			else
			{
				createdFile = await FilesystemTasks.WrapNullable(
						() => StorageFileExtensions.DangerousGetFileFromPathAsync(shellEntry.Template))
					.OnSuccess(async templateFile =>
					{
						var sourceFile = templateFile
							?? throw new InvalidOperationException("The template file could not be resolved.");
						var destinationFolder = parentFolder
							?? throw new InvalidOperationException("The parent folder could not be resolved.");
						return await sourceFile.CopyAsync(destinationFolder, fileName, NameCollisionOption.GenerateUniqueName)
							?? throw new InvalidOperationException("The template copy did not return a file.");
					});
			}

			if (createdFile && shellEntry.Data is not null)
			{
				// Calls unsupported OpenTransactedWriteAsync
				//await FileIO.WriteBytesAsync(createdFile.Result, shellEntry.Data);

				var createdItem = createdFile.Result
					?? throw new InvalidOperationException("The create operation did not return a file.");
				await createdItem.WriteBytesAsync(shellEntry.Data);
			}

			return createdFile;
		}
	}
}
