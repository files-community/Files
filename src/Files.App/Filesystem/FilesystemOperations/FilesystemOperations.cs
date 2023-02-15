using Files.App.Extensions;
using Files.App.Filesystem.FilesystemHistory;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using Files.Backend.Helpers;
using Files.Shared;
using Files.Shared.Enums;
using Files.Shared.Extensions;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using FileAttributes = System.IO.FileAttributes;

namespace Files.App.Filesystem
{
	public enum ImpossibleActionResponseTypes
	{
		Skip,
		Abort
	}

	public class FilesystemOperations : IFilesystemOperations
	{
		private IShellPage associatedInstance;

		#region Constructor

		public FilesystemOperations(IShellPage associatedInstance)
		{
			this.associatedInstance = associatedInstance;
		}

		#endregion Constructor

		#region IFilesystemOperations

		public async Task<(IStorageHistory, IStorageItem)> CreateAsync(IStorageItemWithPath source, IProgress<FileSystemProgress> progress, CancellationToken cancellationToken)
		{
			IStorageItemWithPath item = null;
			FilesystemResult fsResult = (FilesystemResult)false;
			FileSystemProgress fsProgress = new(progress, true, FileSystemStatusCode.InProgress, 1);
			fsProgress.Report();

			try
			{
				switch (source.ItemType)
				{
					case FilesystemItemType.File:
						{
							var newEntryInfo = await ShellNewEntryExtensions.GetNewContextMenuEntryForType(Path.GetExtension(source.Path));
							if (newEntryInfo is null)
							{
								var fsFolderResult = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(source.Path));
								fsResult = fsFolderResult;
								if (fsResult)
								{
									var fsCreateResult = await FilesystemTasks.Wrap(() => fsFolderResult.Result.CreateFileAsync(Path.GetFileName(source.Path), CreationCollisionOption.GenerateUniqueName).AsTask());
									fsResult = fsCreateResult;
									item = fsCreateResult.Result.FromStorageItem();
								}
								if (fsResult == FileSystemStatusCode.Unauthorized)
								{
									// Can't do anything, already tried with admin FTP
								}
							}
							else
							{
								var fsCreateResult = await newEntryInfo.Create(source.Path, associatedInstance);
								fsResult = fsCreateResult;
								if (fsResult)
								{
									item = fsCreateResult.Result.FromStorageItem();
								}
								if (fsResult == FileSystemStatusCode.Unauthorized)
								{
									// Can't do anything, already tried with admin FTP
								}
							}

							break;
						}

					case FilesystemItemType.Directory:
						{
							var fsFolderResult = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(source.Path));
							fsResult = fsFolderResult;
							if (fsResult)
							{
								var fsCreateResult = await FilesystemTasks.Wrap(() => fsFolderResult.Result.CreateFolderAsync(Path.GetFileName(source.Path), CreationCollisionOption.GenerateUniqueName).AsTask());
								fsResult = fsCreateResult;
								item = fsCreateResult.Result.FromStorageItem();
							}
							if (fsResult == FileSystemStatusCode.Unauthorized)
							{
								// Can't do anything, already tried with admin FTP
							}
							break;
						}

					default:
						Debugger.Break();
						break;
				}

				fsProgress.ProcessedItemsCount = 1;
				fsProgress.ReportStatus(fsResult);
				return item is not null
					? (new StorageHistory(FileOperationType.CreateNew, item.CreateList(), null), item.Item)
					: (null, null);
			}
			catch (Exception e)
			{
				fsProgress.ReportStatus(FilesystemTasks.GetErrorCode(e));
				return (null, null);
			}
		}

		public Task<IStorageHistory> CopyAsync(IStorageItem source,
													 string destination,
													 NameCollisionOption collision,
													 IProgress<FileSystemProgress> progress,
													 CancellationToken cancellationToken)
			=> CopyAsync(source.FromStorageItem(), destination, collision, progress, cancellationToken);

		public async Task<IStorageHistory> CopyAsync(IStorageItemWithPath source,
													 string destination,
													 NameCollisionOption collision,
													 IProgress<FileSystemProgress> progress,
													 CancellationToken cancellationToken)
		{
			FileSystemProgress fsProgress = new(progress, true, FileSystemStatusCode.InProgress);
			fsProgress.Report();

			if (destination.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal))
			{
				fsProgress.ReportStatus(FileSystemStatusCode.Unauthorized);

				// Do not paste files and folders inside the recycle bin
				await DialogDisplayHelper.ShowDialogAsync(
					"ErrorDialogThisActionCannotBeDone".GetLocalizedResource(),
					"ErrorDialogUnsupportedOperation".GetLocalizedResource());
				return null;
			}

			IStorageItem copiedItem = null;
			//long itemSize = await FilesystemHelpers.GetItemSize(await source.ToStorageItem(associatedInstance));

			if (source.ItemType == FilesystemItemType.Directory)
			{
				if (!string.IsNullOrWhiteSpace(source.Path) &&
					PathNormalization.GetParentDir(destination).IsSubPathOf(source.Path)) // We check if user tried to copy anything above the source.ItemPath
				{
					var destinationName = destination.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();
					var sourceName = source.Path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();
					ContentDialog dialog = new ContentDialog()
					{
						Title = "ErrorDialogThisActionCannotBeDone".GetLocalizedResource(),
						Content = $"{"ErrorDialogTheDestinationFolder".GetLocalizedResource()} ({destinationName}) {"ErrorDialogIsASubfolder".GetLocalizedResource()} ({sourceName})",
						//PrimaryButtonText = "Skip".GetLocalizedResource(),
						CloseButtonText = "Cancel".GetLocalizedResource()
					};

					ContentDialogResult result = await SetContentDialogRoot(dialog).ShowAsync();

					if (result == ContentDialogResult.Primary)
					{
						fsProgress.ReportStatus(FileSystemStatusCode.InProgress | FileSystemStatusCode.Success);
					}
					else
					{
						fsProgress.ReportStatus(FileSystemStatusCode.InProgress | FileSystemStatusCode.Generic);
					}
					return null;
				}
				else
				{
					// CopyFileFromApp only works on file not directories
					var fsSourceFolder = await source.ToStorageItemResult();
					var fsDestinationFolder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(destination));
					var fsResult = (FilesystemResult)(fsSourceFolder.ErrorCode | fsDestinationFolder.ErrorCode);

					if (fsResult)
					{
						var fsCopyResult = await FilesystemTasks.Wrap(() => CloneDirectoryAsync((BaseStorageFolder)fsSourceFolder, (BaseStorageFolder)fsDestinationFolder, fsSourceFolder.Result.Name, collision.Convert()));

						if (fsCopyResult == FileSystemStatusCode.AlreadyExists)
						{
							fsProgress.ReportStatus(FileSystemStatusCode.AlreadyExists);
							return null;
						}

						if (fsCopyResult)
						{
							if (NativeFileOperationsHelper.HasFileAttribute(source.Path, FileAttributes.Hidden))
							{
								// The source folder was hidden, apply hidden attribute to destination
								NativeFileOperationsHelper.SetFileAttribute(fsCopyResult.Result.Path, FileAttributes.Hidden);
							}
							copiedItem = (BaseStorageFolder)fsCopyResult;
						}
						fsResult = fsCopyResult;
					}
					if (fsResult == FileSystemStatusCode.Unauthorized)
					{
						// Can't do anything, already tried with admin FTP
					}
					fsProgress.ReportStatus(fsResult.ErrorCode);
					if (!fsResult)
					{
						return null;
					}
				}
			}
			else if (source.ItemType == FilesystemItemType.File)
			{
				var fsResult = (FilesystemResult)await Task.Run(() => NativeFileOperationsHelper.CopyFileFromApp(source.Path, destination, true));

				if (!fsResult)
				{
					Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());

					FilesystemResult<BaseStorageFolder> destinationResult = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(destination));
					var sourceResult = await source.ToStorageItemResult();
					fsResult = sourceResult.ErrorCode | destinationResult.ErrorCode;

					if (fsResult)
					{
						var file = (BaseStorageFile)sourceResult;
						var fsResultCopy = new FilesystemResult<BaseStorageFile>(null, FileSystemStatusCode.Generic);
						if (string.IsNullOrEmpty(file.Path) && collision == NameCollisionOption.GenerateUniqueName)
						{
							// If collision is GenerateUniqueName we will manually check for existing file and generate a new name
							// HACK: If file is dragged from zip file in windows explorer for example. The file path is empty and
							// GenerateUniqueName isn't working correctly. Below is a possible solution.
							var desiredNewName = Path.GetFileName(file.Name);
							string nameWithoutExt = Path.GetFileNameWithoutExtension(desiredNewName);
							string extension = Path.GetExtension(desiredNewName);
							ushort attempt = 1;
							do
							{
								fsResultCopy = await FilesystemTasks.Wrap(() => file.CopyAsync(destinationResult.Result, desiredNewName, NameCollisionOption.FailIfExists).AsTask());
								desiredNewName = $"{nameWithoutExt} ({attempt}){extension}";
							} while (fsResultCopy.ErrorCode == FileSystemStatusCode.AlreadyExists && ++attempt < 1024);
						}
						else
						{
							fsResultCopy = await FilesystemTasks.Wrap(() => file.CopyAsync(destinationResult.Result, Path.GetFileName(file.Name), collision).AsTask());
						}

						if (fsResultCopy == FileSystemStatusCode.AlreadyExists)
						{
							fsProgress.ReportStatus(FileSystemStatusCode.AlreadyExists);
							return null;
						}

						if (fsResultCopy)
						{
							copiedItem = fsResultCopy.Result;
						}
						fsResult = fsResultCopy;
					}
					if (fsResult == FileSystemStatusCode.Unauthorized)
					{
						// Can't do anything, already tried with admin FTP
					}
				}
				fsProgress.ReportStatus(fsResult.ErrorCode);
				if (!fsResult)
				{
					return null;
				}
			}

			if (collision == NameCollisionOption.ReplaceExisting)
			{
				fsProgress.ReportStatus(FileSystemStatusCode.Success);

				return null; // Cannot undo overwrite operation
			}

			var pathWithType = copiedItem.FromStorageItem(destination, source.ItemType);

			return new StorageHistory(FileOperationType.Copy, source, pathWithType);
		}

		// WINUI3
		private ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				contentDialog.XamlRoot = App.Window.Content.XamlRoot;
			}
			return contentDialog;
		}

		public Task<IStorageHistory> MoveAsync(IStorageItem source,
													 string destination,
													 NameCollisionOption collision,
													 IProgress<FileSystemProgress> progress,
													 CancellationToken cancellationToken)
			=> MoveAsync(source.FromStorageItem(), destination, collision, progress, cancellationToken);

		public async Task<IStorageHistory> MoveAsync(IStorageItemWithPath source,
													 string destination,
													 NameCollisionOption collision,
													 IProgress<FileSystemProgress> progress,
													 CancellationToken cancellationToken)
		{
			FileSystemProgress fsProgress = new(progress, true, FileSystemStatusCode.InProgress);
			fsProgress.Report();

			if (source.Path == destination)
			{
				fsProgress.ReportStatus(FileSystemStatusCode.Success);
				return null;
			}

			if (string.IsNullOrWhiteSpace(source.Path))
			{
				// Can't move (only copy) files from MTP devices because:
				// StorageItems returned in DataPackageView are read-only
				// The item.Path property will be empty and there's no way of retrieving a new StorageItem with R/W access
				return await CopyAsync(source, destination, collision, progress, cancellationToken);
			}

			if (destination.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal))
			{
				fsProgress.ReportStatus(FileSystemStatusCode.Unauthorized);

				// Do not paste files and folders inside the recycle bin
				await DialogDisplayHelper.ShowDialogAsync(
					"ErrorDialogThisActionCannotBeDone".GetLocalizedResource(),
					"ErrorDialogUnsupportedOperation".GetLocalizedResource());
				return null;
			}

			IStorageItem movedItem = null;
			//long itemSize = await FilesystemHelpers.GetItemSize(await source.ToStorageItem(associatedInstance));

			if (source.ItemType == FilesystemItemType.Directory)
			{
				if (!string.IsNullOrWhiteSpace(source.Path) &&
					PathNormalization.GetParentDir(destination).IsSubPathOf(source.Path)) // We check if user tried to move anything above the source.ItemPath
				{
					var destinationName = destination.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();
					var sourceName = source.Path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();
					ContentDialog dialog = new ContentDialog()
					{
						Title = "ErrorDialogThisActionCannotBeDone".GetLocalizedResource(),
						Content = $"{"ErrorDialogTheDestinationFolder".GetLocalizedResource()} ({destinationName}) {"ErrorDialogIsASubfolder".GetLocalizedResource()} ({sourceName})",
						//PrimaryButtonText = "Skip".GetLocalizedResource(),
						CloseButtonText = "Cancel".GetLocalizedResource()
					};

					ContentDialogResult result = await SetContentDialogRoot(dialog).ShowAsync();

					if (result == ContentDialogResult.Primary)
					{
						fsProgress.ReportStatus(FileSystemStatusCode.InProgress | FileSystemStatusCode.Success);
					}
					else
					{
						fsProgress.ReportStatus(FileSystemStatusCode.InProgress | FileSystemStatusCode.Generic);
					}
					return null;
				}
				else
				{
					var fsResult = (FilesystemResult)await Task.Run(() => NativeFileOperationsHelper.MoveFileFromApp(source.Path, destination));

					if (!fsResult)
					{
						Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());

						var fsSourceFolder = await source.ToStorageItemResult();
						var fsDestinationFolder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(destination));
						fsResult = fsSourceFolder.ErrorCode | fsDestinationFolder.ErrorCode;

						if (fsResult)
						{
							// Moving folders using Storage API can result in data loss, copy instead
							//var fsResultMove = await FilesystemTasks.Wrap(() => MoveDirectoryAsync((BaseStorageFolder)fsSourceFolder, (BaseStorageFolder)fsDestinationFolder, fsSourceFolder.Result.Name, collision.Convert(), true));
							var fsResultMove = new FilesystemResult<BaseStorageFolder>(null, FileSystemStatusCode.Generic);
							if (await DialogDisplayHelper.ShowDialogAsync("ErrorDialogThisActionCannotBeDone".GetLocalizedResource(), "ErrorDialogUnsupportedMoveOperation".GetLocalizedResource(), "OK", "Cancel".GetLocalizedResource()))
							{
								fsResultMove = await FilesystemTasks.Wrap(() => CloneDirectoryAsync((BaseStorageFolder)fsSourceFolder, (BaseStorageFolder)fsDestinationFolder, fsSourceFolder.Result.Name, collision.Convert()));
							}

							if (fsResultMove == FileSystemStatusCode.AlreadyExists)
							{
								fsProgress.ReportStatus(FileSystemStatusCode.AlreadyExists);
								return null;
							}

							if (fsResultMove)
							{
								if (NativeFileOperationsHelper.HasFileAttribute(source.Path, FileAttributes.Hidden))
								{
									// The source folder was hidden, apply hidden attribute to destination
									NativeFileOperationsHelper.SetFileAttribute(fsResultMove.Result.Path, FileAttributes.Hidden);
								}
								movedItem = (BaseStorageFolder)fsResultMove;
							}
							fsResult = fsResultMove;
						}
						if (fsResult == FileSystemStatusCode.Unauthorized || fsResult == FileSystemStatusCode.ReadOnly)
						{
							// Can't do anything, already tried with admin FTP
						}
					}
					fsProgress.ReportStatus(fsResult.ErrorCode);
				}
			}
			else if (source.ItemType == FilesystemItemType.File)
			{
				var fsResult = (FilesystemResult)await Task.Run(() => NativeFileOperationsHelper.MoveFileFromApp(source.Path, destination));

				if (!fsResult)
				{
					Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());

					FilesystemResult<BaseStorageFolder> destinationResult = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(destination));
					var sourceResult = await source.ToStorageItemResult();
					fsResult = sourceResult.ErrorCode | destinationResult.ErrorCode;

					if (fsResult)
					{
						var file = (BaseStorageFile)sourceResult;
						var fsResultMove = await FilesystemTasks.Wrap(() => file.MoveAsync(destinationResult.Result, Path.GetFileName(file.Name), collision).AsTask());

						if (fsResultMove == FileSystemStatusCode.AlreadyExists)
						{
							fsProgress.ReportStatus(FileSystemStatusCode.AlreadyExists);
							return null;
						}

						if (fsResultMove)
						{
							movedItem = file;
						}
						fsResult = fsResultMove;
					}
					if (fsResult == FileSystemStatusCode.Unauthorized || fsResult == FileSystemStatusCode.ReadOnly)
					{
						// Can't do anything, already tried with admin FTP
					}
				}
				fsProgress.ReportStatus(fsResult.ErrorCode);
			}

			if (collision == NameCollisionOption.ReplaceExisting)
			{
				return null; // Cannot undo overwrite operation
			}

			var pathWithType = movedItem.FromStorageItem(destination, source.ItemType);

			return new StorageHistory(FileOperationType.Move, source, pathWithType);
		}

		public Task<IStorageHistory> DeleteAsync(IStorageItem source,
													   IProgress<FileSystemProgress> progress,
													   bool permanently,
													   CancellationToken cancellationToken)
			=> DeleteAsync(source.FromStorageItem(), progress, permanently, cancellationToken);

		public async Task<IStorageHistory> DeleteAsync(IStorageItemWithPath source,
													   IProgress<FileSystemProgress> progress,
													   bool permanently,
													   CancellationToken cancellationToken)
		{
			FileSystemProgress fsProgress = new(progress, true, FileSystemStatusCode.InProgress);
			fsProgress.Report();

			bool deleteFromRecycleBin = RecycleBinHelpers.IsPathUnderRecycleBin(source.Path);

			FilesystemResult fsResult = FileSystemStatusCode.InProgress;

			if (permanently)
			{
				fsResult = (FilesystemResult)NativeFileOperationsHelper.DeleteFileFromApp(source.Path);
			}
			if (!fsResult)
			{
				if (source.ItemType == FilesystemItemType.File)
				{
					fsResult = await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(source.Path)
						.OnSuccess((t) => t.DeleteAsync(permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default).AsTask());
				}
				else if (source.ItemType == FilesystemItemType.Directory)
				{
					fsResult = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(source.Path)
						.OnSuccess((t) => t.DeleteAsync(permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default).AsTask());
				}
			}

			fsProgress.ReportStatus(fsResult);

			if (fsResult == FileSystemStatusCode.Unauthorized)
			{
				// Can't do anything, already tried with admin FTP
			}
			else if (fsResult == FileSystemStatusCode.InUse)
			{
				// TODO: retry
				await DialogDisplayHelper.ShowDialogAsync(DynamicDialogFactory.GetFor_FileInUseDialog());
			}

			if (deleteFromRecycleBin)
			{
				// Recycle bin also stores a file starting with $I for each item
				string iFilePath = Path.Combine(Path.GetDirectoryName(source.Path), Path.GetFileName(source.Path).Replace("$R", "$I", StringComparison.Ordinal));
				await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(iFilePath)
					.OnSuccess(iFile => iFile.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask());
			}
			fsProgress.ReportStatus(fsResult);

			if (fsResult)
			{
				await associatedInstance.FilesystemViewModel.RemoveFileOrFolderAsync(source.Path);

				if (!permanently)
				{
					// Enumerate Recycle Bin
					IEnumerable<ShellFileItem> nameMatchItems, items = await RecycleBinHelpers.EnumerateRecycleBin();

					// Get name matching files
					if (FileExtensionHelpers.IsShortcutOrUrlFile(source.Path)) // We need to check if it is a shortcut file
					{
						nameMatchItems = items.Where((item) => item.FilePath == Path.Combine(Path.GetDirectoryName(source.Path), Path.GetFileNameWithoutExtension(source.Path)));
					}
					else
					{
						nameMatchItems = items.Where((item) => item.FilePath == source.Path);
					}

					// Get newest file
					ShellFileItem item = nameMatchItems.OrderBy((item) => item.RecycleDate).FirstOrDefault();

					return new StorageHistory(FileOperationType.Recycle, source, StorageHelpers.FromPathAndType(item?.RecyclePath, source.ItemType));
				}

				return new StorageHistory(FileOperationType.Delete, source, null);
			}
			else
			{
				// Stop at first error
				return null;
			}
		}

		public Task<IStorageHistory> RenameAsync(IStorageItem source,
													   string newName,
													   NameCollisionOption collision,
													   IProgress<FileSystemProgress> progress,
													   CancellationToken cancellationToken)
			=> RenameAsync(StorageHelpers.FromStorageItem(source), newName, collision, progress, cancellationToken);

		public async Task<IStorageHistory> RenameAsync(IStorageItemWithPath source,
													   string newName,
													   NameCollisionOption collision,
													   IProgress<FileSystemProgress> progress,
													   CancellationToken cancellationToken)
		{
			FileSystemProgress fsProgress = new FileSystemProgress(progress, true, FileSystemStatusCode.InProgress);
			fsProgress.Report();

			if (Path.GetFileName(source.Path) == newName && collision == NameCollisionOption.FailIfExists)
			{
				fsProgress.ReportStatus(FileSystemStatusCode.AlreadyExists);
				return null;
			}

			if (!string.IsNullOrWhiteSpace(newName)
				&& !FilesystemHelpers.ContainsRestrictedCharacters(newName)
				&& !FilesystemHelpers.ContainsRestrictedFileName(newName))
			{
				var renamed = await source.ToStorageItemResult()
					.OnSuccess(async (t) =>
					{
						if (t.Name.Equals(newName, StringComparison.CurrentCultureIgnoreCase))
						{
							await t.RenameAsync(newName, NameCollisionOption.ReplaceExisting);
						}
						else
						{
							await t.RenameAsync(newName, collision);
						}
						return t;
					});

				if (renamed)
				{
					fsProgress.ReportStatus(FileSystemStatusCode.Success);
					return new StorageHistory(FileOperationType.Rename, source, renamed.Result.FromStorageItem());
				}
				else if (renamed == FileSystemStatusCode.Unauthorized)
				{
					// Try again with MoveFileFromApp
					var destination = Path.Combine(Path.GetDirectoryName(source.Path), newName);
					if (NativeFileOperationsHelper.MoveFileFromApp(source.Path, destination))
					{
						fsProgress.ReportStatus(FileSystemStatusCode.Success);
						return new StorageHistory(FileOperationType.Rename, source, StorageHelpers.FromPathAndType(destination, source.ItemType));
					}
					else
					{
						// Can't do anything, already tried with admin FTP
					}
				}
				else if (renamed == FileSystemStatusCode.NotAFile || renamed == FileSystemStatusCode.NotAFolder)
				{
					await DialogDisplayHelper.ShowDialogAsync("RenameError/NameInvalid/Title".GetLocalizedResource(), "RenameError/NameInvalid/Text".GetLocalizedResource());
				}
				else if (renamed == FileSystemStatusCode.NameTooLong)
				{
					await DialogDisplayHelper.ShowDialogAsync("RenameError/TooLong/Title".GetLocalizedResource(), "RenameError/TooLong/Text".GetLocalizedResource());
				}
				else if (renamed == FileSystemStatusCode.InUse)
				{
					// TODO: retry
					await DialogDisplayHelper.ShowDialogAsync(DynamicDialogFactory.GetFor_FileInUseDialog());
				}
				else if (renamed == FileSystemStatusCode.NotFound)
				{
					await DialogDisplayHelper.ShowDialogAsync("RenameError/ItemDeleted/Title".GetLocalizedResource(), "RenameError/ItemDeleted/Text".GetLocalizedResource());
				}
				else if (renamed == FileSystemStatusCode.AlreadyExists)
				{
					var ItemAlreadyExistsDialog = new ContentDialog()
					{
						Title = "ItemAlreadyExistsDialogTitle".GetLocalizedResource(),
						Content = "ItemAlreadyExistsDialogContent".GetLocalizedResource(),
						PrimaryButtonText = "GenerateNewName".GetLocalizedResource(),
						SecondaryButtonText = "ItemAlreadyExistsDialogSecondaryButtonText".GetLocalizedResource(),
						CloseButtonText = "Cancel".GetLocalizedResource()
					};

					ContentDialogResult result = await ItemAlreadyExistsDialog.TryShowAsync();

					if (result == ContentDialogResult.Primary)
					{
						return await RenameAsync(source, newName, NameCollisionOption.GenerateUniqueName, progress, cancellationToken);
					}
					else if (result == ContentDialogResult.Secondary)
					{
						return await RenameAsync(source, newName, NameCollisionOption.ReplaceExisting, progress, cancellationToken);
					}
				}
				fsProgress.ReportStatus(renamed);
			}

			return null;
		}

		public async Task<IStorageHistory> RestoreItemsFromTrashAsync(IList<IStorageItem> source,
																	 IList<string> destination,
																	 IProgress<FileSystemProgress> progress,
																	 CancellationToken cancellationToken)
		{
			return await RestoreItemsFromTrashAsync(await source.Select((item) => item.FromStorageItem()).ToListAsync(), destination, progress, cancellationToken);
		}

		public async Task<IStorageHistory> RestoreItemsFromTrashAsync(IList<IStorageItemWithPath> source,
																	 IList<string> destination,
																	 IProgress<FileSystemProgress> progress,
																	 CancellationToken token)
		{
			FileSystemProgress fsProgress = new(progress, true, FileSystemStatusCode.InProgress, source.Count);
			fsProgress.Report();

			var rawStorageHistory = new List<IStorageHistory>();

			for (int i = 0; i < source.Count; i++)
			{
				if (token.IsCancellationRequested)
				{
					break;
				}

				rawStorageHistory.Add(await RestoreFromTrashAsync(
					source[i],
					destination[i],
					null,
					token));

				fsProgress.ProcessedItemsCount++;
				fsProgress.Report();

			}

			if (rawStorageHistory.Any() && rawStorageHistory.All((item) => item is not null))
			{
				return new StorageHistory(
					rawStorageHistory[0].OperationType,
					await rawStorageHistory.SelectMany((item) => item.Source).ToListAsync(),
					await rawStorageHistory.SelectMany((item) => item.Destination).ToListAsync());
			}
			return null;
		}

		public Task<IStorageHistory> RestoreFromTrashAsync(IStorageItem source,
																 string destination,
																 IProgress<FileSystemProgress> progress,
																 CancellationToken cancellationToken)
			=> RestoreFromTrashAsync(source.FromStorageItem(), destination, progress, cancellationToken);

		public async Task<IStorageHistory> RestoreFromTrashAsync(IStorageItemWithPath source,
																 string destination,
																 IProgress<FileSystemProgress> progress,
																 CancellationToken cancellationToken)
		{
			FileSystemProgress fsProgress = new(progress, true, FileSystemStatusCode.InProgress);
			fsProgress.Report();

			FilesystemResult fsResult = FileSystemStatusCode.InProgress;

			fsResult = (FilesystemResult)await Task.Run(() => NativeFileOperationsHelper.MoveFileFromApp(source.Path, destination));

			if (!fsResult)
			{
				if (source.ItemType == FilesystemItemType.Directory)
				{
					FilesystemResult<BaseStorageFolder> sourceFolder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(source.Path);
					FilesystemResult<BaseStorageFolder> destinationFolder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(destination));

					fsResult = sourceFolder.ErrorCode | destinationFolder.ErrorCode;
					fsProgress.ReportStatus(fsResult);

					if (fsResult)
					{
						// Moving folders using Storage API can result in data loss, copy instead
						//fsResult = await FilesystemTasks.Wrap(() => MoveDirectoryAsync(sourceFolder.Result, destinationFolder.Result, Path.GetFileName(destination), CreationCollisionOption.FailIfExists, true));
						if (await DialogDisplayHelper.ShowDialogAsync("ErrorDialogThisActionCannotBeDone".GetLocalizedResource(), "ErrorDialogUnsupportedMoveOperation".GetLocalizedResource(), "OK", "Cancel".GetLocalizedResource()))
						{
							fsResult = await FilesystemTasks.Wrap(() => CloneDirectoryAsync(sourceFolder.Result, destinationFolder.Result, Path.GetFileName(destination), CreationCollisionOption.FailIfExists));
						}
						// TODO: we could use here FilesystemHelpers with registerHistory false?
					}
					fsProgress.ReportStatus(fsResult);
				}
				else
				{
					FilesystemResult<BaseStorageFile> sourceFile = await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(source.Path);
					FilesystemResult<BaseStorageFolder> destinationFolder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(destination));

					fsResult = sourceFile.ErrorCode | destinationFolder.ErrorCode;
					fsProgress.ReportStatus(fsResult);

					if (fsResult)
					{
						fsResult = await FilesystemTasks.Wrap(() => sourceFile.Result.MoveAsync(destinationFolder.Result, Path.GetFileName(destination), NameCollisionOption.GenerateUniqueName).AsTask());
					}
					fsProgress.ReportStatus(fsResult);
				}
				if (fsResult == FileSystemStatusCode.Unauthorized || fsResult == FileSystemStatusCode.ReadOnly)
				{
					// Can't do anything, already tried with admin FTP
				}
			}

			if (fsResult)
			{
				// Recycle bin also stores a file starting with $I for each item
				string iFilePath = Path.Combine(Path.GetDirectoryName(source.Path), Path.GetFileName(source.Path).Replace("$R", "$I", StringComparison.Ordinal));
				await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(iFilePath)
					.OnSuccess(iFile => iFile.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask());
			}

			fsProgress.ReportStatus(fsResult);
			if (fsResult != FileSystemStatusCode.Success)
			{
				if (((FileSystemStatusCode)fsResult).HasFlag(FileSystemStatusCode.Unauthorized))
				{
					await DialogDisplayHelper.ShowDialogAsync("AccessDenied".GetLocalizedResource(), "AccessDeniedDeleteDialog/Text".GetLocalizedResource());
				}
				else if (((FileSystemStatusCode)fsResult).HasFlag(FileSystemStatusCode.NotFound))
				{
					await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalizedResource(), "FileNotFoundDialog/Text".GetLocalizedResource());
				}
				else if (((FileSystemStatusCode)fsResult).HasFlag(FileSystemStatusCode.AlreadyExists))
				{
					await DialogDisplayHelper.ShowDialogAsync("ItemAlreadyExistsDialogTitle".GetLocalizedResource(), "ItemAlreadyExistsDialogContent".GetLocalizedResource());
				}
			}

			return new StorageHistory(FileOperationType.Restore, source, StorageHelpers.FromPathAndType(destination, source.ItemType));
		}

		#endregion IFilesystemOperations

		#region Helpers

		private async static Task<BaseStorageFolder> CloneDirectoryAsync(BaseStorageFolder sourceFolder, BaseStorageFolder destinationFolder, string sourceRootName, CreationCollisionOption collision = CreationCollisionOption.FailIfExists)
		{
			BaseStorageFolder createdRoot = await destinationFolder.CreateFolderAsync(sourceRootName, collision);
			destinationFolder = createdRoot;

			foreach (BaseStorageFile fileInSourceDir in await sourceFolder.GetFilesAsync())
			{
				await fileInSourceDir.CopyAsync(destinationFolder, fileInSourceDir.Name, NameCollisionOption.GenerateUniqueName);
			}

			foreach (BaseStorageFolder folderinSourceDir in await sourceFolder.GetFoldersAsync())
			{
				await CloneDirectoryAsync(folderinSourceDir, destinationFolder, folderinSourceDir.Name);
			}

			return createdRoot;
		}

		#endregion Helpers

		#region IDisposable

		public void Dispose()
		{
			associatedInstance = null;
		}

		#endregion IDisposable

		public async Task<IStorageHistory> CopyItemsAsync(IList<IStorageItem> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<FileSystemProgress> progress, CancellationToken cancellationToken)
		{
			return await CopyItemsAsync(await source.Select((item) => item.FromStorageItem()).ToListAsync(), destination, collisions, progress, cancellationToken);
		}

		public async Task<IStorageHistory> CopyItemsAsync(IList<IStorageItemWithPath> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<FileSystemProgress> progress, CancellationToken token)
		{
			FileSystemProgress fsProgress = new(progress, true, FileSystemStatusCode.InProgress, source.Count);
			fsProgress.Report();

			var rawStorageHistory = new List<IStorageHistory>();

			for (int i = 0; i < source.Count; i++)
			{
				if (token.IsCancellationRequested)
				{
					break;
				}

				if (collisions[i] != FileNameConflictResolveOptionType.Skip)
				{
					rawStorageHistory.Add(await CopyAsync(
						source[i],
						destination[i],
						collisions[i].Convert(),
						null,
						token));
				}

				fsProgress.ProcessedItemsCount++;
				fsProgress.Report();

			}

			if (rawStorageHistory.Any() && rawStorageHistory.All((item) => item is not null))
			{
				return new StorageHistory(
					rawStorageHistory[0].OperationType,
					await rawStorageHistory.SelectMany((item) => item.Source).ToListAsync(),
					await rawStorageHistory.SelectMany((item) => item.Destination).ToListAsync());
			}
			return null;
		}

		public async Task<IStorageHistory> MoveItemsAsync(IList<IStorageItem> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<FileSystemProgress> progress, CancellationToken cancellationToken)
		{
			return await MoveItemsAsync(await source.Select((item) => item.FromStorageItem()).ToListAsync(), destination, collisions, progress, cancellationToken);
		}

		public async Task<IStorageHistory> MoveItemsAsync(IList<IStorageItemWithPath> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<FileSystemProgress> progress, CancellationToken token)
		{
			FileSystemProgress fsProgress = new(progress, true, FileSystemStatusCode.InProgress, source.Count);
			fsProgress.Report();

			var rawStorageHistory = new List<IStorageHistory>();

			for (int i = 0; i < source.Count; i++)
			{
				if (token.IsCancellationRequested)
				{
					break;
				}

				if (collisions[i] != FileNameConflictResolveOptionType.Skip)
				{
					rawStorageHistory.Add(await MoveAsync(
						source[i],
						destination[i],
						collisions[i].Convert(),
						null,
						token));
				}

				fsProgress.ProcessedItemsCount++;
				fsProgress.Report();
			}

			if (rawStorageHistory.Any() && rawStorageHistory.All((item) => item is not null))
			{
				return new StorageHistory(
					rawStorageHistory[0].OperationType,
					await rawStorageHistory.SelectMany((item) => item.Source).ToListAsync(),
					await rawStorageHistory.SelectMany((item) => item.Destination).ToListAsync());
			}
			return null;
		}

		public async Task<IStorageHistory> DeleteItemsAsync(IList<IStorageItem> source, IProgress<FileSystemProgress> progress, bool permanently, CancellationToken cancellationToken)
		{
			return await DeleteItemsAsync(await source.Select((item) => item.FromStorageItem()).ToListAsync(), progress, permanently, cancellationToken);
		}

		public async Task<IStorageHistory> DeleteItemsAsync(IList<IStorageItemWithPath> source, IProgress<FileSystemProgress> progress, bool permanently, CancellationToken token)
		{
			FileSystemProgress fsProgress = new(progress, true, FileSystemStatusCode.InProgress, source.Count);
			fsProgress.Report();

			bool originalPermanently = permanently;
			var rawStorageHistory = new List<IStorageHistory>();

			for (int i = 0; i < source.Count; i++)
			{
				if (token.IsCancellationRequested)
					break;

				permanently = RecycleBinHelpers.IsPathUnderRecycleBin(source[i].Path) || originalPermanently;

				rawStorageHistory.Add(await DeleteAsync(source[i], null, permanently, token));
				fsProgress.ProcessedItemsCount++;
				fsProgress.Report();
			}

			if (rawStorageHistory.Any() && rawStorageHistory.All((item) => item is not null))
			{
				return new StorageHistory(
					rawStorageHistory[0].OperationType,
					await rawStorageHistory.SelectMany((item) => item.Source).ToListAsync(),
					await rawStorageHistory.SelectMany((item) => item.Destination).ToListAsync());
			}
			return null;
		}

		public Task<IStorageHistory> CreateShortcutItemsAsync(IList<IStorageItemWithPath> source, IList<string> destination, IProgress<FileSystemProgress> progress, CancellationToken token)
		{
			throw new NotImplementedException("Cannot create shortcuts in UWP.");
		}
	}
}
