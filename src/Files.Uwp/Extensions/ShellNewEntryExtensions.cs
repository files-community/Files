using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Files.Shared;
using Newtonsoft.Json;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Files.Extensions
{
	public static class ShellNewEntryExtensions
	{
		public static async Task<List<ShellNewEntry>> GetNewContextMenuEntries()
		{
			var shellEntryList = new List<ShellNewEntry>();
			var connection = await AppServiceConnectionHelper.Instance;
			if (connection != null)
			{
				var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
				{
					{ "Arguments", "GetNewContextMenuEntries" }
				});
				if (status == AppServiceResponseStatus.Success && response.ContainsKey("Entries"))
				{
					var entries = JsonConvert.DeserializeObject<List<ShellNewEntry>>((string)response["Entries"]);
					if (entries != null)
					{
						shellEntryList.AddRange(entries);
					}
				}
			}
			return shellEntryList;
		}

		public static async Task<ShellNewEntry> GetNewContextMenuEntryForType(string extension)
		{
			var connection = await AppServiceConnectionHelper.Instance;
			if (connection != null)
			{
				var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
				{
					{ "Arguments", "GetNewContextMenuEntryForType" },
					{ "extension", extension }
				});
				if (status == AppServiceResponseStatus.Success && response.ContainsKey("Entry"))
				{
					return JsonConvert.DeserializeObject<ShellNewEntry>((string)response["Entry"]);
				}
			}
			return null;
		}

		public static async Task<FilesystemResult<BaseStorageFile>> Create(this ShellNewEntry shellEntry, string filePath, IShellPage associatedInstance)
		{
			var parentFolder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(filePath));
			if (parentFolder)
			{
				return await Create(shellEntry, parentFolder, filePath);
			}
			return new FilesystemResult<BaseStorageFile>(null, parentFolder.ErrorCode);
		}

		public static async Task<FilesystemResult<BaseStorageFile>> Create(this ShellNewEntry shellEntry, BaseStorageFolder parentFolder, string filePath)
		{
			FilesystemResult<BaseStorageFile> createdFile = null;
			var fileName = Path.GetFileName(filePath);
			if (!fileName.EndsWith(shellEntry.Extension, StringComparison.Ordinal))
			{
				fileName += shellEntry.Extension;
			}
			if (shellEntry.Command != null)
			{
				var args = CommandLine.CommandLineParser.SplitArguments(shellEntry.Command);
				if (args.Any())
				{
					var connection = await AppServiceConnectionHelper.Instance;
					if (connection != null)
					{
						_ = await connection.SendMessageForResponseAsync(new ValueSet()
						{
							{ "Arguments", "LaunchApp" },
							{ "WorkingDirectory", PathNormalization.GetParentDir(filePath) },
							{ "Application", args[0].Replace("\"", "", StringComparison.Ordinal) },
							{ "Parameters", string.Join(" ", args.Skip(1)).Replace("%1", filePath) }
						});
					}
				}
				createdFile = new FilesystemResult<BaseStorageFile>(null, Shared.Enums.FileSystemStatusCode.Success);
			}
			else if (shellEntry.Template == null)
			{
				createdFile = await FilesystemTasks.Wrap(() => parentFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName).AsTask());
			}
			else
			{
				createdFile = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(shellEntry.Template))
					.OnSuccess(t => t.CopyAsync(parentFolder, fileName, NameCollisionOption.GenerateUniqueName).AsTask());
			}
			if (createdFile)
			{
				if (shellEntry.Data != null)
				{
					//await FileIO.WriteBytesAsync(createdFile.Result, shellEntry.Data); // Calls unsupported OpenTransactedWriteAsync
					await createdFile.Result.WriteBytesAsync(shellEntry.Data);
				}
			}
			return createdFile;
		}
	}
}