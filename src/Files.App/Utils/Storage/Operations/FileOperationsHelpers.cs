// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils.Storage.Operations;
using Files.Shared.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO;
using Windows.ApplicationModel.DataTransfer;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
using FILEOPERATION_FLAGS = Windows.Win32.UI.Shell.FILEOPERATION_FLAGS;
using HRESULT = Windows.Win32.Foundation.HRESULT;
using HWND = Windows.Win32.Foundation.HWND;
using PROPERTYKEY = Windows.Win32.Foundation.PROPERTYKEY;
using SLR_FLAGS = Windows.Win32.UI.Shell.SLR_FLAGS;

namespace Files.App.Utils.Storage
{
	public sealed partial class FileOperationsHelpers
	{
		private static readonly PROPERTYKEY PKEY_FilePlaceholderStatus = new() { fmtid = new("B2F9B9D6-FEC4-4DD5-94D7-8957488C807B"), pid = 2 };
		private const uint PS_CLOUDFILE_PLACEHOLDER = 8;

		private static IDevToolsSettingsService DevToolsSettingsService => field ??= Ioc.Default.GetRequiredService<IDevToolsSettingsService>();

		private static ProgressHandler? progressHandler; // Warning: must be initialized from a MTA thread
		private static readonly ConcurrentDictionary<string, CancellationTokenSource> robocopyOperationTokens = new();

		public static Task SetClipboard(string[] filesToCopy, DataPackageOperation operation)
		{
			return STATask.Run(() =>
			{
				uint preferredDropEffect = (uint)(operation == DataPackageOperation.Copy
					? DataPackageOperation.Copy | DataPackageOperation.Link
					: DataPackageOperation.Move);
				ShellDataObject.SetClipboard(filesToCopy, preferredDropEffect);
			}, App.Logger);
		}

		public static Task<(bool, ShellOperationResult)> CreateItemAsync(string filePath, string fileOp, long ownerHwnd, bool asAdmin, string? template = null, byte[]? dataBytes = null)
		{
			return STATask.Run(async () =>
			{
				using var op = new ShellFileOperations2();

				op.Options = FILEOPERATION_FLAGS.FOF_SILENT | FILEOPERATION_FLAGS.FOF_NOCONFIRMMKDIR | FILEOPERATION_FLAGS.FOF_RENAMEONCOLLISION | FILEOPERATION_FLAGS.FOF_NOERRORUI;
				if (asAdmin)
				{
					op.Options |= FILEOPERATION_FLAGS.FOFX_SHOWELEVATIONPROMPT | FILEOPERATION_FLAGS.FOFX_REQUIREELEVATION;
				}
				op.OwnerWindow = (HWND)(nint)ownerHwnd;

				var shellOperationResult = new ShellOperationResult();
				var parentPath = Path.GetDirectoryName(filePath);

				if (parentPath is null || !SafetyExtensions.IgnoreExceptions(() =>
				{
					using var shd = new ShellFolder(parentPath);
					op.QueueNewItemOperation(shd, Path.GetFileName(filePath),
						fileOp == "CreateFolder" ? FileAttributes.Directory : FileAttributes.Normal, template);
				}))
				{
					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = false,
						Destination = filePath,
						HResult = -1
					});
				}

				var createTcs = new TaskCompletionSource<bool>();
				op.PostNewItem += (s, e) =>
				{
					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Destination = e.DestItem.GetParsingPath(),
						HResult = (int)e.Result
					});
				};
				op.FinishOperations += (s, e) => createTcs.TrySetResult(e.Result.Succeeded);

				try
				{
					op.PerformOperations();
				}
				catch
				{
					createTcs.TrySetResult(false);
				}

				if (dataBytes is not null &&
					shellOperationResult.Items.SingleOrDefault() is { Succeeded: true, Destination: { } destination })
				{
					SafetyExtensions.IgnoreExceptions(() =>
					{
						using var fs = new FileStream(destination, FileMode.Open);
						fs.Write(dataBytes, 0, dataBytes.Length);
						fs.Flush();
					}, App.Logger);
				}

				return (await createTcs.Task, shellOperationResult);
			}, App.Logger);
		}

		public static Task<(bool, ShellOperationResult)> TestRecycleAsync(string[] fileToDeletePath)
		{
			return STATask.Run(async () =>
			{
				using var op = new ShellFileOperations2();

				op.Options = FILEOPERATION_FLAGS.FOF_SILENT | FILEOPERATION_FLAGS.FOF_NOCONFIRMATION | FILEOPERATION_FLAGS.FOF_NOERRORUI;
				op.Options |= FILEOPERATION_FLAGS.FOFX_RECYCLEONDELETE;

				var shellOperationResult = new ShellOperationResult();
				var tryDelete = false;

				for (var i = 0; i < fileToDeletePath.Length; i++)
				{
					if (!SafetyExtensions.IgnoreExceptions(() =>
					{
						using var shi = new ShellItem(fileToDeletePath[i]);
						using var file = SafetyExtensions.IgnoreExceptions(() => GetFirstFile(shi)) ?? shi;
						if (file.Properties.TryGetValue<uint>(PKEY_FilePlaceholderStatus, out var value) && value == PS_CLOUDFILE_PLACEHOLDER)
						{
							// Online only files cannot be tried for deletion, so they are treated as to be permanently deleted.
							shellOperationResult.Items.Add(new ShellOperationItemResult()
							{
								Succeeded = false,
								Source = fileToDeletePath[i],
								HResult = (int)HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND
							});
						}
						else
						{
							op.QueueDeleteOperation(file);
							tryDelete = true;
						}
					}))
					{
						shellOperationResult.Items.Add(new ShellOperationItemResult()
						{
							Succeeded = false,
							Source = fileToDeletePath[i],
							HResult = -1
						});
					}
				}

				if (!tryDelete)
					return (true, shellOperationResult);

				var deleteTcs = new TaskCompletionSource<bool>();
				op.PreDeleteItem += [DebuggerHidden] (s, e) =>
				{
					if ((e.Flags & Windows.Win32.UI.Shell._TRANSFER_SOURCE_FLAGS.TSF_DELETE_RECYCLE_IF_POSSIBLE) is 0)
					{
						shellOperationResult.Items.Add(new ShellOperationItemResult()
						{
							Succeeded = false,
							Source = e.SourceItem.GetParsingPath(),
							HResult = (int)HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND
						});
						throw new Win32Exception((int)HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND); // E_FAIL, stops operation
					}
					else
					{
						shellOperationResult.Items.Add(new ShellOperationItemResult()
						{
							Succeeded = true,
							Source = e.SourceItem.GetParsingPath(),
							HResult = (int)HRESULT.COPYENGINE_E_USER_CANCELLED
						});
						throw new Win32Exception((int)HRESULT.COPYENGINE_E_USER_CANCELLED); // E_FAIL, stops operation
					}
				};
				op.FinishOperations += (s, e) => deleteTcs.TrySetResult(e.Result.Succeeded);

				try
				{
					op.PerformOperations();
				}
				catch
				{
					deleteTcs.TrySetResult(false);
				}

				return (await deleteTcs.Task, shellOperationResult);
			}, App.Logger);
		}

		public static Task<(bool, ShellOperationResult)> DeleteItemAsync(string[] fileToDeletePath, bool permanently, long ownerHwnd, bool asAdmin, IProgress<StatusCenterItemProgressModel>? progress, string operationID = "")
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			StatusCenterItemProgressModel fsProgress = new(
				progress,
				false,
				FileSystemStatusCode.InProgress);

			var cts = new CancellationTokenSource();
			var sizeCalculator = new FileSizeCalculator(fileToDeletePath);

			// Track the count and update the progress
			sizeCalculator.ItemsCountChanged += (newCount) =>
			{
				fsProgress.ItemsCount = newCount;
				fsProgress.Report();
			};

			var sizeTask = sizeCalculator.ComputeSizeAsync(cts.Token);
			sizeTask.ContinueWith(_ =>
			{
				fsProgress.TotalSize = 0;
				fsProgress.ItemsCount = sizeCalculator.ItemsCount;
				fsProgress.EnumerationCompleted = true;
				fsProgress.Report();
			});

			fsProgress.Report();
			progressHandler ??= new();

			return STATask.Run(async () =>
			{
				using var op = new ShellFileOperations2();

				op.Options = FILEOPERATION_FLAGS.FOF_SILENT | FILEOPERATION_FLAGS.FOF_NOCONFIRMATION | FILEOPERATION_FLAGS.FOF_NOERRORUI;

				if (asAdmin)
				{
					op.Options |= FILEOPERATION_FLAGS.FOFX_SHOWELEVATIONPROMPT | FILEOPERATION_FLAGS.FOFX_REQUIREELEVATION;
				}

				op.OwnerWindow = (HWND)(nint)ownerHwnd;

				if (!permanently)
				{
					op.Options |= FILEOPERATION_FLAGS.FOFX_RECYCLEONDELETE | FILEOPERATION_FLAGS.FOF_WANTNUKEWARNING;
				}

				var shellOperationResult = new ShellOperationResult();

				for (var i = 0; i < fileToDeletePath.Length; i++)
				{
					if (!SafetyExtensions.IgnoreExceptions(() =>
					{
						using var shi = new ShellItem(fileToDeletePath[i]);

						op.QueueDeleteOperation(shi);
					}))
					{
						shellOperationResult.Items.Add(new ShellOperationItemResult()
						{
							Succeeded = false,
							Source = fileToDeletePath[i],
							HResult = -1
						});
					}
				}

				progressHandler.OwnerWindow = op.OwnerWindow;
				progressHandler.AddOperation(operationID);

				var deleteTcs = new TaskCompletionSource<bool>();

				// Right before deleting item
				op.PreDeleteItem += (s, e) =>
				{
					if (e.SourceItem is not { } sourceItem)
						return;

					if (sourceItem.GetParsingPath() is { } sourcePath)
						sizeCalculator.ForceComputeFileSize(sourcePath);
					fsProgress.FileName = sourceItem.Name ?? string.Empty;
					fsProgress.Report();
				};

				// Right after deleted item
				op.PostDeleteItem += (s, e) =>
				{
					var sourceItem = e.SourceItem;
					var sourcePath = sourceItem?.GetParsingPath();
					if (sourceItem is { IsFolder: false } && sourcePath is not null)
					{
						if (sizeCalculator.TryGetComputedFileSize(sourcePath, out _))
							fsProgress.AddProcessedItemsCount(1);
					}

					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = sourcePath,
						Destination = e.DestItem?.GetParsingPath(),
						HResult = (int)e.Result
					});

					UpdateFileTagsDb(e, "delete");
				};

				op.FinishOperations += (s, e)
					=> deleteTcs.TrySetResult(e.Result.Succeeded);

				op.UpdateProgress += (s, e) =>
				{
					// E_FAIL, stops operation
					if (progressHandler.CheckCanceled(operationID))
						throw new Win32Exception(unchecked((int)0x80004005));

					fsProgress.Report(e.ProgressPercentage);
					progressHandler.UpdateOperation(operationID, e.ProgressPercentage);
				};

				try
				{
					op.PerformOperations();
				}
				catch
				{
					deleteTcs.TrySetResult(false);
				}

				progressHandler.RemoveOperation(operationID);

				cts.Cancel();

				return (await deleteTcs.Task, shellOperationResult);
			}, App.Logger);
		}

		public static Task<(bool, ShellOperationResult)> RenameItemAsync(string fileToRenamePath, string newName, bool overwriteOnRename, long ownerHwnd, bool asAdmin, string operationID = "")
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			progressHandler ??= new();

			return STATask.Run(async () =>
			{
				using var op = new ShellFileOperations2();
				var shellOperationResult = new ShellOperationResult();

				op.Options = FILEOPERATION_FLAGS.FOF_SILENT | FILEOPERATION_FLAGS.FOF_NOERRORUI;
				if (asAdmin)
				{
					op.Options |= FILEOPERATION_FLAGS.FOFX_SHOWELEVATIONPROMPT | FILEOPERATION_FLAGS.FOFX_REQUIREELEVATION;
				}
				op.OwnerWindow = (HWND)(nint)ownerHwnd;
				op.Options |= !overwriteOnRename ? FILEOPERATION_FLAGS.FOF_RENAMEONCOLLISION : 0;

				if (!SafetyExtensions.IgnoreExceptions(() =>
				{
					using var shi = new ShellItem(fileToRenamePath);
					op.QueueRenameOperation(shi, newName);
				}))
				{
					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = false,
						Source = fileToRenamePath,
						HResult = -1
					});
				}

				progressHandler.OwnerWindow = op.OwnerWindow;
				progressHandler.AddOperation(operationID);

				var renameTcs = new TaskCompletionSource<bool>();
				op.PostRenameItem += (s, e) =>
				{
					var sourcePath = e.SourceItem.GetParsingPath();
					var sourceFolderPath = sourcePath is null ? null : Path.GetDirectoryName(sourcePath);
					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = sourcePath,
						Destination = sourceFolderPath is not null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(sourceFolderPath, e.Name) : null,
						HResult = (int)e.Result
					});
				};
				op.PostRenameItem += (_, e) => UpdateFileTagsDb(e, "rename");
				op.FinishOperations += (s, e) => renameTcs.TrySetResult(e.Result.Succeeded);

				try
				{
					op.PerformOperations();
				}
				catch
				{
					renameTcs.TrySetResult(false);
				}

				progressHandler.RemoveOperation(operationID);

				return (await renameTcs.Task, shellOperationResult);
			}, App.Logger);
		}

		public static Task<(bool, ShellOperationResult)> MoveItemAsync(string[] fileToMovePath, string[] moveDestination, bool overwriteOnMove, long ownerHwnd, bool asAdmin, IProgress<StatusCenterItemProgressModel> progress, string operationID = "")
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			StatusCenterItemProgressModel fsProgress = new(
				progress,
				false,
				FileSystemStatusCode.InProgress);

			var cts = new CancellationTokenSource();
			var sizeCalculator = new FileSizeCalculator(fileToMovePath);

			// Track the count and update the progress
			sizeCalculator.ItemsCountChanged += (newCount) =>
			{
				fsProgress.ItemsCount = newCount;
				fsProgress.Report();
			};

			var sizeTask = sizeCalculator.ComputeSizeAsync(cts.Token);
			sizeTask.ContinueWith(_ =>
			{
				fsProgress.TotalSize = sizeCalculator.Size;
				fsProgress.ItemsCount = sizeCalculator.ItemsCount;
				fsProgress.EnumerationCompleted = true;
				fsProgress.Report();
			});

			fsProgress.Report();
			progressHandler ??= new();

			return STATask.Run(async () =>
			{
				using var op = new ShellFileOperations2();
				var shellOperationResult = new ShellOperationResult();

				op.Options = FILEOPERATION_FLAGS.FOF_NOCONFIRMMKDIR | FILEOPERATION_FLAGS.FOF_SILENT | FILEOPERATION_FLAGS.FOF_NOERRORUI;

				if (asAdmin)
				{
					op.Options |= FILEOPERATION_FLAGS.FOFX_SHOWELEVATIONPROMPT | FILEOPERATION_FLAGS.FOFX_REQUIREELEVATION;
				}

				op.OwnerWindow = (HWND)(nint)ownerHwnd;

				op.Options |= !overwriteOnMove ? FILEOPERATION_FLAGS.FOFX_PRESERVEFILEEXTENSIONS | FILEOPERATION_FLAGS.FOF_RENAMEONCOLLISION : FILEOPERATION_FLAGS.FOF_NOCONFIRMATION;

				for (var i = 0; i < fileToMovePath.Length; i++)
				{
					if (!SafetyExtensions.IgnoreExceptions(() =>
					{
						using ShellItem shi = new(fileToMovePath[i]);
						var destinationFolderPath = Path.GetDirectoryName(moveDestination[i])
							?? throw new ArgumentException("The move destination must include a parent folder.", nameof(moveDestination));
						using ShellFolder shd = new(destinationFolderPath);

						op.QueueMoveOperation(shi, shd, Path.GetFileName(moveDestination[i]));
					}))
					{
						shellOperationResult.Items.Add(new ShellOperationItemResult()
						{
							Succeeded = false,
							Source = fileToMovePath[i],
							Destination = moveDestination[i],
							// HResult -1 makes the caller retry with the StorageFile API (ADS); a missing source must map to NotFound instead
							HResult = MainStreamExists(fileToMovePath[i]) ? -1 : CopyEngineResult.COPYENGINE_E_PATH_NOT_FOUND_SRC
						});
					}
				}

				progressHandler.OwnerWindow = op.OwnerWindow;
				progressHandler.AddOperation(operationID);

				var moveTcs = new TaskCompletionSource<bool>();

				op.PreMoveItem += (s, e) =>
				{
					if (e.SourceItem is not { } sourceItem)
						return;

					if (sourceItem.GetParsingPath() is { } sourcePath)
						sizeCalculator.ForceComputeFileSize(sourcePath);
					fsProgress.FileName = sourceItem.Name ?? string.Empty;
					fsProgress.Report();
				};

				op.PostMoveItem += (s, e) =>
				{
					var sourceItem = e.SourceItem;
					var sourcePath = sourceItem?.GetParsingPath();
					if (sourceItem is { IsFolder: false } && sourcePath is not null)
					{
						if (sizeCalculator.TryGetComputedFileSize(sourcePath, out _))
							fsProgress.AddProcessedItemsCount(1);
					}

					var destinationFolderPath = e.DestFolder?.GetParsingPath();
					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = sourcePath,
						Destination = destinationFolderPath is not null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(destinationFolderPath, e.Name) : null,
						HResult = (int)e.Result
					});

					UpdateFileTagsDb(e, "move");
				};

				op.FinishOperations += (s, e)
					=> moveTcs.TrySetResult(e.Result.Succeeded);

				op.UpdateProgress += (s, e) =>
				{
					// E_FAIL, stops operation
					if (progressHandler.CheckCanceled(operationID))
						throw new Win32Exception(unchecked((int)0x80004005));

					fsProgress.Report(e.ProgressPercentage);
					progressHandler.UpdateOperation(operationID, e.ProgressPercentage);
				};

				try
				{
					op.PerformOperations();
				}
				catch
				{
					moveTcs.TrySetResult(false);
				}

				progressHandler.RemoveOperation(operationID);

				cts.Cancel();

				return (await moveTcs.Task, shellOperationResult);
			}, App.Logger);
		}

		public static Task<(bool, ShellOperationResult)> CopyItemAsync(string[] fileToCopyPath, string[] copyDestination, bool overwriteOnCopy, long ownerHwnd, bool asAdmin, IProgress<StatusCenterItemProgressModel> progress, string operationID = "")
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			StatusCenterItemProgressModel fsProgress = new(
				progress,
				false,
				FileSystemStatusCode.InProgress);

			var cts = new CancellationTokenSource();
			var sizeCalculator = new FileSizeCalculator(fileToCopyPath);

			// Track the count and update the progress
			sizeCalculator.ItemsCountChanged += (newCount) =>
			{
				fsProgress.ItemsCount = newCount;
				fsProgress.Report();
			};

			var sizeTask = sizeCalculator.ComputeSizeAsync(cts.Token);
			sizeTask.ContinueWith(_ =>
			{
				fsProgress.TotalSize = sizeCalculator.Size;
				fsProgress.ItemsCount = sizeCalculator.ItemsCount;
				fsProgress.EnumerationCompleted = true;
				fsProgress.Report();
			});

			fsProgress.Report();
			progressHandler ??= new();

			return STATask.Run(async () =>
			{
				using var op = new ShellFileOperations2();

				var shellOperationResult = new ShellOperationResult();

				op.Options = FILEOPERATION_FLAGS.FOF_NOCONFIRMMKDIR | FILEOPERATION_FLAGS.FOF_SILENT | FILEOPERATION_FLAGS.FOF_NOERRORUI;

				if (asAdmin)
				{
					op.Options |= FILEOPERATION_FLAGS.FOFX_SHOWELEVATIONPROMPT | FILEOPERATION_FLAGS.FOFX_REQUIREELEVATION;
				}

				op.OwnerWindow = (HWND)(nint)ownerHwnd;

				op.Options |= !overwriteOnCopy ? FILEOPERATION_FLAGS.FOFX_PRESERVEFILEEXTENSIONS | FILEOPERATION_FLAGS.FOF_RENAMEONCOLLISION : FILEOPERATION_FLAGS.FOF_NOCONFIRMATION;

				for (var i = 0; i < fileToCopyPath.Length; i++)
				{
					if (!SafetyExtensions.IgnoreExceptions(() =>
					{
						using ShellItem shi = new(fileToCopyPath[i]);
						var destinationFolderPath = Path.GetDirectoryName(copyDestination[i])
							?? throw new ArgumentException("The copy destination must include a parent folder.", nameof(copyDestination));
						using ShellFolder shd = new(destinationFolderPath);

						var fileName = GetIncrementalName(overwriteOnCopy, copyDestination[i], fileToCopyPath[i]);
						// Perform a copy operation
						op.QueueCopyOperation(shi, shd, fileName);
					}))
					{
						shellOperationResult.Items.Add(new ShellOperationItemResult()
						{
							Succeeded = false,
							Source = fileToCopyPath[i],
							Destination = copyDestination[i],
							// HResult -1 makes the caller retry with the StorageFile API (ADS); a missing source must map to NotFound instead
							HResult = MainStreamExists(fileToCopyPath[i]) ? -1 : CopyEngineResult.COPYENGINE_E_PATH_NOT_FOUND_SRC
						});
					}
				}

				progressHandler.OwnerWindow = op.OwnerWindow;
				progressHandler.AddOperation(operationID);

				var copyTcs = new TaskCompletionSource<bool>();

				op.PreCopyItem += (s, e) =>
				{
					if (e.SourceItem is not { } sourceItem)
						return;

					if (sourceItem.GetParsingPath() is { } sourcePath)
						sizeCalculator.ForceComputeFileSize(sourcePath);
					fsProgress.FileName = sourceItem.Name ?? string.Empty;
					fsProgress.Report();
				};

				op.PostCopyItem += (s, e) =>
				{
					var sourceItem = e.SourceItem;
					var sourcePath = sourceItem?.GetParsingPath();
					if (sourceItem is { IsFolder: false } && sourcePath is not null)
					{
						if (sizeCalculator.TryGetComputedFileSize(sourcePath, out _))
							fsProgress.AddProcessedItemsCount(1);
					}

					var destinationFolderPath = e.DestFolder?.GetParsingPath();
					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = sourcePath,
						Destination = destinationFolderPath is not null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(destinationFolderPath, e.Name) : null,
						HResult = (int)e.Result
					});

					UpdateFileTagsDb(e, "copy");
				};

				op.FinishOperations += (s, e)
					=> copyTcs.TrySetResult(e.Result.Succeeded);

				op.UpdateProgress += (s, e) =>
				{
					// E_FAIL, stops operation
					if (progressHandler.CheckCanceled(operationID))
						throw new Win32Exception(unchecked((int)0x80004005));

					fsProgress.Report(e.ProgressPercentage);
					progressHandler.UpdateOperation(operationID, e.ProgressPercentage);
				};

				try
				{
					op.PerformOperations();
				}
				catch
				{
					copyTcs.TrySetResult(false);
				}

				progressHandler.RemoveOperation(operationID);

				cts.Cancel();

				return (await copyTcs.Task, shellOperationResult);
			}, App.Logger);
		}

		public static Task<(bool, ShellOperationResult)> CopyItemWithRobocopyAsync(string[] fileToCopyPath, string[] copyDestination, bool overwriteOnCopy, long ownerHwnd, bool asAdmin, IProgress<StatusCenterItemProgressModel>? progress, string operationID = "", IShellPage? shellPage = null)
		{
			return PerformRobocopyOperationAsync(
				fileToCopyPath,
				copyDestination,
				overwriteOnCopy,
				ownerHwnd,
				asAdmin,
				progress,
				operationID,
				shellPage,
				isMoveOperation: false);
		}

		public static Task<(bool, ShellOperationResult)> MoveItemWithRobocopyAsync(string[] fileToMovePath, string[] moveDestination, bool overwriteOnMove, long ownerHwnd, bool asAdmin, IProgress<StatusCenterItemProgressModel>? progress, string operationID = "", IShellPage? shellPage = null)
		{
			return PerformRobocopyOperationAsync(
				fileToMovePath,
				moveDestination,
				overwriteOnMove,
				ownerHwnd,
				asAdmin,
				progress,
				operationID,
				shellPage,
				isMoveOperation: true);
		}

		/// <summary>
		/// Checks all source descendants for reparse points without following them.
		/// </summary>
		private static bool AreRobocopySourcesSafe(IEnumerable<string> sourcePaths, CancellationToken cancellationToken)
		{
			try
			{
				var pending = new Stack<FileSystemInfo>(sourcePaths.Select<string, FileSystemInfo>(path =>
					Win32Helper.HasFileAttribute(path, FileAttributes.Directory)
						? new DirectoryInfo(path)
						: new FileInfo(path)));
				while (pending.TryPop(out var item))
				{
					cancellationToken.ThrowIfCancellationRequested();
					if (item.Attributes.HasFlag(FileAttributes.ReparsePoint))
						return false;

					if (item is DirectoryInfo directory)
					{
						foreach (var child in directory.EnumerateFileSystemInfos())
						{
							cancellationToken.ThrowIfCancellationRequested();
							pending.Push(child);
						}
					}
				}
				return true;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				App.Logger?.LogWarning(ex, "Unable to safely enumerate Robocopy sources");
				return false;
			}
		}

		private static async Task<(bool success, int hResult)> RunRobocopyAsync(IReadOnlyList<string> arguments, StatusCenterItemProgressModel? progressModel, IReadOnlyCollection<string>? expectedItemNames, string operationID, CancellationToken cancellationToken)
		{
			try
			{
				App.Logger?.LogInformation($"Robocopy operation {operationID}: Starting with arguments: {string.Join(" ", arguments)}");

				// Robocopy writes output using the system OEM code page, not UTF-8.
				var oemEncoding = System.Text.Encoding.GetEncoding(
					System.Globalization.CultureInfo.CurrentCulture.TextInfo.OEMCodePage);

				var psi = new ProcessStartInfo
				{
					FileName = "robocopy.exe",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					StandardOutputEncoding = oemEncoding,
					StandardErrorEncoding = oemEncoding
				};
				foreach (var argument in arguments)
					psi.ArgumentList.Add(argument);

				using var process = new Process { StartInfo = psi };
				var outputCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				var remainingItemNames = expectedItemNames is null
					? null
					: new HashSet<string>(expectedItemNames, StringComparer.OrdinalIgnoreCase);
				var initialProcessedSize = progressModel?.ProcessedSize ?? 0;
				long batchProcessedSize = 0;
				var hResult = -1;
				process.OutputDataReceived += (_, e) =>
				{
					if (e.Data is null)
					{
						outputCompleted.TrySetResult();
						return;
					}

					if (e.Data.Contains("(0x00000020)", StringComparison.OrdinalIgnoreCase))
						hResult = CopyEngineResult.HRESULT_ERROR_SHARING_VIOLATION;
					else if (e.Data.Contains("(0x00000005)", StringComparison.OrdinalIgnoreCase))
						hResult = CopyEngineResult.HRESULT_ERROR_ACCESS_DENIED;

					var fields = e.Data.Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
					var completedItemName = fields.Length > 0 ? Path.GetFileName(fields[^1]) : string.Empty;
					var itemSize = 0L;
					var hasItemSize = fields.Length > 1 && long.TryParse(fields[^2], out itemSize);
					var isCompletedItem = remainingItemNames is null
						? hasItemSize
						: remainingItemNames.Remove(completedItemName);
					if (progressModel is not null && isCompletedItem)
					{
						if (hasItemSize)
						{
							var processedSize = initialProcessedSize + Interlocked.Add(ref batchProcessedSize, itemSize);
							progressModel.SetProcessedSize(processedSize);
						}

						progressModel.FileName = completedItemName;
						progressModel.AddProcessedItemsCount(1);
						var percentage = progressModel.TotalSize > 0 && progressModel.ProcessedSize > 0
							? Math.Min(99, progressModel.ProcessedSize * 100.0 / progressModel.TotalSize)
							: Math.Min(99, progressModel.ProcessedItemsCount * 100.0 / Math.Max(1, progressModel.ItemsCount));
						progressModel.Report(percentage);
					}
				};

				process.Start();
				process.BeginOutputReadLine();

				var errorTask = process.StandardError.ReadToEndAsync();
				using var registration = cancellationToken.Register(() =>
				{
					try
					{
						if (!process.HasExited)
							process.Kill(entireProcessTree: true);
					}
					catch (Exception ex)
					{
						App.Logger?.LogWarning(ex, "Robocopy operation {OperationId}: Failed to terminate process", operationID);
					}
				});

				try
				{
					await process.WaitForExitAsync(cancellationToken);
				}
				catch (OperationCanceledException)
				{
					try
					{
						if (!process.HasExited)
							process.Kill(entireProcessTree: true);
					}
					catch (Exception ex)
					{
						App.Logger?.LogWarning(ex, "Robocopy operation {OperationId}: Failed to terminate cancelled process", operationID);
					}

					try
					{
						await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));
						await Task.WhenAll(outputCompleted.Task, errorTask).WaitAsync(TimeSpan.FromSeconds(10));
					}
					catch (Exception ex)
					{
						App.Logger?.LogWarning(ex, "Robocopy operation {OperationId}: Process did not finish cleanly after cancellation", operationID);
					}

					App.Logger?.LogWarning($"Robocopy operation {operationID}: Cancelled");
					return (false, -3);
				}

				await Task.WhenAll(outputCompleted.Task, errorTask);

				var standardError = await errorTask;
				if (standardError.Contains("(0x00000020)", StringComparison.OrdinalIgnoreCase))
					hResult = CopyEngineResult.HRESULT_ERROR_SHARING_VIOLATION;
				else if (standardError.Contains("(0x00000005)", StringComparison.OrdinalIgnoreCase))
					hResult = CopyEngineResult.HRESULT_ERROR_ACCESS_DENIED;

				var exitCode = process.ExitCode;
				// Bit 4 means mismatched files; treating it as success can hide a partial move.
				// An inaccessible directory can exhaust retries without setting an exit-code error bit.
				var success = exitCode is >= 0 and <= 3 && (exitCode != 0 || hResult == -1);
				if (!success)
				{
					App.Logger?.LogWarning($"Robocopy operation {operationID}: Exit code {exitCode}. {standardError}");
				}
				else
				{
					App.Logger?.LogInformation($"Robocopy operation {operationID}: Completed with exit code {exitCode}");
				}

				return (success, success ? 0 : hResult);
			}
			catch (Exception ex)
			{
				App.Logger?.LogError(ex, $"Robocopy operation {operationID}: Failed with exception");
				return (false, -1);
			}
		}

		/// <summary>
		/// Enumerates item names for batch result verification.
		/// </summary>
		private static HashSet<string>? EnumerateItemNames(string directoryPath)
		{
			try
			{
				return Directory.EnumerateFileSystemEntries(directoryPath)
					.Select(path => Path.GetFileName(path))
					.ToHashSet(StringComparer.OrdinalIgnoreCase);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				App.Logger?.LogWarning(ex, "Unable to verify Robocopy results in {DirectoryPath}", directoryPath);
				return null;
			}
		}

		private static (Dictionary<(string sourceDir, string destDir), List<string>> fileGroups, List<(string sourcePath, string destPath)> folderItems) GroupFilesAndFolders(
			string[] filePaths,
			string[] destinationPaths)
		{
			var fileGroups = new Dictionary<(string sourceDir, string destDir), List<string>>();
			var folderItems = new List<(string sourcePath, string destPath)>();

			for (var i = 0; i < filePaths.Length; i++)
			{
				var sourcePath = filePaths[i];
				var destPath = destinationPaths[i];
				var isDirectory = Win32Helper.HasFileAttribute(sourcePath, FileAttributes.Directory);

				if (isDirectory)
				{
					// For directories: store full source and destination paths for individual processing
					folderItems.Add((sourcePath, destPath));
				}
				else
				{
					// For files: group by sourceDir/destDir for batching
					var sourceDir = Path.GetDirectoryName(sourcePath)!;
					var itemName = Path.GetFileName(sourcePath);
					var destDir = Path.GetDirectoryName(destPath)!;

					var key = (sourceDir, destDir);
					if (!fileGroups.TryGetValue(key, out var list))
					{
						list = new List<string>();
						fileGroups[key] = list;
					}
					list.Add(itemName);
				}
			}
			return (fileGroups, folderItems);
		}

		private static (Dictionary<(string sourceDir, string destDir), List<List<string>>> batchesByGroup, int totalBatches) CreateBatchesForFileGroups(
			Dictionary<(string sourceDir, string destDir), List<string>> fileGroups)
		{
			var batchesByGroup = new Dictionary<(string sourceDir, string destDir), List<List<string>>>();
			var totalBatches = 0;

			foreach (var group in fileGroups)
			{
				var groupBatches = new List<List<string>>();
				var currentBatch = new List<string>();
				int currentBatchSize = 0;
				const int maxBatchSize = 8000;

				foreach (var itemName in group.Value)
				{
					// Calculate the size this item would add to the batch
					// Include quotes if the item name contains spaces, plus space separator
					int itemSize = itemName.Contains(' ') ?
						itemName.Length + 2 + 1 : // +2 for quotes, +1 for space
						itemName.Length + 1;     // +1 for space

					// If adding this item would exceed the batch size limit, start a new batch
					if (currentBatch.Count > 0 && currentBatchSize + itemSize > maxBatchSize)
					{
						groupBatches.Add(currentBatch);
						currentBatch = new List<string>();
						currentBatchSize = 0;
						totalBatches++;
					}

					// Add the item to the current batch
					currentBatch.Add(itemName);
					currentBatchSize += itemSize;
				}

				// Add the final batch for this group if it has items
				if (currentBatch.Count > 0)
				{
					groupBatches.Add(currentBatch);
					totalBatches++;
				}

				batchesByGroup[group.Key] = groupBatches;
			}

			return (batchesByGroup, totalBatches);
		}

		private static Task<(bool, ShellOperationResult)> PerformRobocopyOperationAsync(
			string[] filePaths,
			string[] destinationPaths,
			bool overwriteOnOperation,
			long ownerHwnd,
			bool asAdmin,
			IProgress<StatusCenterItemProgressModel>? progress,
			string operationID,
			IShellPage? shellPage,
			bool isMoveOperation)
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			StatusCenterItemProgressModel fsProgress = new(
				progress,
				false,
				FileSystemStatusCode.InProgress);

			CancellationTokenSource cts = new();
			robocopyOperationTokens.TryGetValue(operationID, out var previousCts);
			robocopyOperationTokens[operationID] = cts;

			fsProgress.ItemsCount = filePaths.Length;
			fsProgress.Report();
			progressHandler ??= new();

			return Task.Run(async () =>
			{
				var shellOperationResult = new ShellOperationResult();
				var success = true;

				Task sizeTask = Task.CompletedTask;
				App.Logger?.LogInformation($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Processing {filePaths.Length} items");

				// Initial progress update
				fsProgress.Report(0);

				try
				{
					if (asAdmin || !AreRobocopySourcesSafe(filePaths, cts.Token))
					{
						return isMoveOperation
							? await MoveItemAsync(filePaths, destinationPaths, overwriteOnOperation, ownerHwnd, asAdmin, progress!, operationID)
							: await CopyItemAsync(filePaths, destinationPaths, overwriteOnOperation, ownerHwnd, asAdmin, progress!, operationID);
					}

					var sizeCalculator = new FileSizeCalculator(filePaths);
					sizeTask = sizeCalculator.ComputeSizeAsync(cts.Token);
					_ = sizeTask.ContinueWith(task =>
					{
						if (!task.IsCompletedSuccessfully)
							return;

						fsProgress.TotalSize = sizeCalculator.Size;
						fsProgress.ItemsCount = sizeCalculator.ItemsCount;
						fsProgress.EnumerationCompleted = true;
						fsProgress.Report();
					}, TaskScheduler.Default);

					progressHandler.AddOperation(operationID);

					// Group files and folders separately
					var (fileGroups, folderItems) = GroupFilesAndFolders(filePaths, destinationPaths);

					App.Logger?.LogInformation($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Created {fileGroups.Count} file groups and {folderItems.Count} folder items");

					var threads = Math.Clamp(DevToolsSettingsService.RobocopyThreads, 1, 128);

					// Create batches for files only (folders will be processed individually)
					(Dictionary<(string sourceDir, string destDir), List<List<string>>> fileBatchesByGroup, int totalFileBatches) = CreateBatchesForFileGroups(fileGroups);

					var totalOperations = totalFileBatches + folderItems.Count;
					App.Logger?.LogInformation($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Created {fileBatchesByGroup.Sum(g => g.Value.Count)} file batches and {folderItems.Count} folder operations (total: {totalOperations})");

					// Execute file batches per source/destination directory combo (8000 chars max)
					var completed = 0;
					foreach (var groupKvp in fileBatchesByGroup)
					{
						if (cts.Token.IsCancellationRequested || progressHandler.CheckCanceled(operationID))
						{
							success = false;
							cts.Cancel();
							break;
						}

						(string sourceDir, string destDir) = groupKvp.Key;
						var groupBatches = groupKvp.Value;

						App.Logger?.LogInformation($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Processing file group ({sourceDir}, {destDir}) with {groupBatches.Count} batches");

						foreach (var itemNames in groupBatches)
						{
							if (cts.Token.IsCancellationRequested || progressHandler.CheckCanceled(operationID))
							{
								success = false;
								cts.Cancel();
								break;
							}

							var batchOk = true;
							var hResult = 0;

							var argsList = new List<string>
							{
								sourceDir,
								destDir
							};
							argsList.AddRange(itemNames);
							argsList.AddRange([
								"/R:3",
								"/W:1",
								"/NJH",
								"/NJS",
								"/NDL",
								"/NP",
								"/BYTES",
								$"/MT:{threads}"
							]);

							if (!overwriteOnOperation)
							{
								argsList.Add("/XN");
								argsList.Add("/XO");
								argsList.Add("/XC");
							}
							else
							{
								// A move with replace semantics must process files Robocopy considers unchanged.
								argsList.Add("/IS");
								argsList.Add("/IT");
							}

							// Add operation-specific flags
							if (isMoveOperation)
								argsList.Add("/MOV");

							var robocopyArgs = argsList;

							// check if the argsList is longer than 8000 characters
							if (robocopyArgs.Sum(argument => argument.Length + 3) > 8000)
							{
								App.Logger?.LogWarning($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Args list is longer than 8000 characters, trying anyway");
							}

							App.Logger?.LogInformation($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Executing file batch with {itemNames.Count} items, args length: {robocopyArgs.Sum(argument => argument.Length + 3)}");
							(batchOk, hResult) = await RunRobocopyAsync(robocopyArgs, fsProgress, itemNames, operationID, cts.Token);

							// Robocopy exit codes describe the batch, so verify every requested item before
							// reporting success. A skipped move otherwise looks successful while its source remains.
							var batchVerified = true;
							var destinationNames = EnumerateItemNames(destDir);
							var remainingSourceNames = isMoveOperation ? EnumerateItemNames(sourceDir) : null;
							foreach (var itemName in itemNames)
							{
								var sourcePath = Path.Combine(sourceDir, itemName);
								var destinationPath = Path.Combine(destDir, itemName);
								var itemOk = batchOk && destinationNames is not null && destinationNames.Contains(itemName) &&
									(!isMoveOperation || remainingSourceNames is not null && !remainingSourceNames.Contains(itemName));
								batchVerified &= itemOk;
								shellOperationResult.Items.Add(new ShellOperationItemResult
								{
									Succeeded = itemOk,
									Source = sourcePath,
									Destination = destinationPath,
									HResult = itemOk ? 0 : hResult != 0 ? hResult : -1
								});
							}
							batchOk &= batchVerified;

							if (!batchOk)
							{
								App.Logger?.LogWarning($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: File batch failed with HRESULT {hResult}");
								success = false;
							}
							else
							{
								App.Logger?.LogInformation($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: File batch completed successfully");
							}

							completed++;
							fsProgress.Report();

						}
					}

					// Process folders individually
					foreach (var (sourcePath, destPath) in folderItems)
					{
						if (cts.Token.IsCancellationRequested || progressHandler.CheckCanceled(operationID))
						{
							success = false;
							cts.Cancel();
							break;
						}

						var folderOk = true;
						var hResult = 0;

						var argsList = new List<string>
						{
							sourcePath,
							destPath,
							"/E",
							"/XJ",
							"/SL",
							"/R:3",
							"/W:1",
							"/NJH",
							"/NJS",
							"/NDL",
							"/NP",
							"/BYTES",
							$"/MT:{threads}"
						};

						if (!overwriteOnOperation)
						{
							argsList.Add("/XN");
							argsList.Add("/XO");
							argsList.Add("/XC");
						}
						else
						{
							// A move with replace semantics must process files Robocopy considers unchanged.
							argsList.Add("/IS");
							argsList.Add("/IT");
						}

						// Add operation-specific flags
						if (isMoveOperation)
							argsList.Add("/MOVE");

						var robocopyArgs = argsList;

						App.Logger?.LogInformation($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Processing folder {sourcePath} -> {destPath}");
						(folderOk, hResult) = await RunRobocopyAsync(robocopyArgs, fsProgress, null, operationID, cts.Token);

						folderOk = folderOk && StorageHelpers.Exists(destPath) &&
							(!isMoveOperation || !StorageHelpers.Exists(sourcePath));
						shellOperationResult.Items.Add(new ShellOperationItemResult
						{
							Succeeded = folderOk,
							Source = sourcePath,
							Destination = destPath,
							HResult = folderOk ? 0 : hResult != 0 ? hResult : -1
						});

						if (!folderOk)
						{
							App.Logger?.LogWarning($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Folder operation failed with HRESULT {hResult}");
							success = false;
						}
						else
						{
							App.Logger?.LogInformation($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Folder operation completed successfully");
						}

						completed++;
						fsProgress.Report();

					}

					if (success)
						fsProgress.Report(100);

					if (shellPage is { } page)
					{
						await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
							page.ShellViewModel!.RefreshItems(null));
					}

					App.Logger?.LogInformation($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Completed with overall success: {success}");
				}
				catch (Exception ex)
				{
					App.Logger?.LogError(ex, $"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Failed with exception");
					success = false;
				}
				finally
				{
					progressHandler.RemoveOperation(operationID);
					if (robocopyOperationTokens.TryGetValue(operationID, out var trackedCts)
						&& ReferenceEquals(trackedCts, cts))
					{
						if (previousCts is not null)
							robocopyOperationTokens[operationID] = previousCts;
						else
							robocopyOperationTokens.TryRemove(operationID, out _);
					}
					cts.Cancel();
					try
					{
						await sizeTask.WaitAsync(TimeSpan.FromSeconds(2));
					}
					catch (OperationCanceledException)
					{
					}
					catch (Exception ex)
					{
						App.Logger?.LogWarning(ex, "Robocopy size calculation did not finish cleanly");
					}
					cts.Dispose();
				}

				return (success, shellOperationResult);
			});
		}

		public static void TryCancelOperation(string operationId)
		{
			progressHandler?.TryCancel(operationId);
			if (robocopyOperationTokens.TryGetValue(operationId, out var cts))
			{
				try
				{
					cts.Cancel();
				}
				catch (Exception ex)
				{
					App.Logger?.LogWarning(ex, "Unable to cancel file operation {OperationId}", operationId);
				}
			}
		}

		public static IEnumerable<Win32Process>? CheckFileInUse(string[] fileToCheckPath)
		{
			var processes = SafetyExtensions.IgnoreExceptions(() => Win32Helper.WhoIsLocking(fileToCheckPath), App.Logger);

			if (processes is not null)
			{
				var win32proc = processes.Select(x => new Win32Process()
				{
					Name = x.ProcessName,
					Pid = x.Id,
					FileName = SafetyExtensions.IgnoreExceptions(() => x.MainModule?.FileName),
					AppName = SafetyExtensions.IgnoreExceptions(() => x.MainModule?.FileVersionInfo?.FileDescription)
				}).ToList();
				processes.ForEach(x => x.Dispose());

				return win32proc;
			}
			else
			{
				return null;
			}
		}

		/// <param name="resolveTarget">
		/// Whether to run the shell's link resolution (which may search for moved targets and touch the
		/// network). Pass <see langword="false"/> when only the data stored in the link file is needed,
		/// e.g. for listing items.
		/// </param>
		public static async Task<ShellLinkItem?> ParseLinkAsync(string linkPath, bool resolveTarget = true)
		{
			if (string.IsNullOrEmpty(linkPath))
				return null;

			string targetPath = string.Empty;

			try
			{
				if (FileExtensionHelpers.IsShortcutFile(linkPath))
				{
					using var link = resolveTarget
						? new ShellLink(linkPath, SLR_FLAGS.SLR_NO_UI_WITH_MSG_PUMP, timeout: TimeSpan.FromMilliseconds(100))
						: new ShellLink(linkPath, resolve: false);
					targetPath = link.TargetPath;

					// Broken shortcut (rooted target that's gone) keeps the delete prompt; app/shell targets aren't rooted
					if (resolveTarget && Path.IsPathRooted(targetPath) &&
						!targetPath.StartsWith(@"\\", StringComparison.Ordinal) && !Path.Exists(targetPath))
					{
						return new ShellLinkItem { TargetPath = targetPath, InvalidTarget = true };
					}

					return ShellFolderExtensions.GetShellLinkItem(link);
				}
				else if (FileExtensionHelpers.IsWebLinkFile(linkPath))
				{
					targetPath = await STATask.Run(() => InternetShortcut.Load(linkPath), App.Logger);
					return string.IsNullOrEmpty(targetPath) ?
						new ShellLinkItem
						{
							TargetPath = string.Empty,
							InvalidTarget = true
						} : new ShellLinkItem { TargetPath = targetPath };
				}
				return null;
			}
			catch (FileNotFoundException ex) // Could not parse shortcut
			{
				App.Logger?.LogWarning(ex, ex.Message);
				// Return a item containing the invalid target path
				return new ShellLinkItem
				{
					TargetPath = string.IsNullOrEmpty(targetPath) ? string.Empty : targetPath,
					InvalidTarget = true
				};
			}
			catch (Exception ex)
			{
				// Could not parse shortcut
				App.Logger.LogWarning(ex, ex.Message);
				return null;
			}
		}

		public static Task<bool> CreateOrUpdateLinkAsync(string linkSavePath, string? targetPath, string? arguments = "", string? workingDirectory = "", bool runAsAdmin = false, SHOW_WINDOW_CMD showWindowCommand = SHOW_WINDOW_CMD.SW_NORMAL)
		{
			try
			{
				ArgumentNullException.ThrowIfNull(targetPath);
				if (FileExtensionHelpers.IsShortcutFile(linkSavePath))
				{
					using var newLink = new ShellLink(targetPath, arguments, workingDirectory);

					// Check if the target is a file
					if (File.Exists(targetPath))
						newLink.RunAsAdministrator = runAsAdmin;

					newLink.SaveAs(linkSavePath); // Overwrite if exists

					// ShowState has to be set after SaveAs has been called, otherwise an UnauthorizedAccessException gets thrown in some cases
					newLink.ShowState = showWindowCommand;

					return Task.FromResult(true);
				}
				else if (FileExtensionHelpers.IsWebLinkFile(linkSavePath))
				{
					return STATask.Run(() =>
					{
						InternetShortcut.Save(linkSavePath, targetPath);
						return true;
					}, App.Logger);
				}
			}
			catch (UnauthorizedAccessException ex)
			{
				// Could not create shortcut
				App.Logger.LogInformation(ex, "Failed to create shortcut");
			}
			catch (Exception ex)
			{
				// Could not create shortcut
				App.Logger.LogWarning(ex, ex.Message);
			}

			return Task.FromResult(false);
		}

		public static bool SetLinkIcon(string filePath, string? iconFile, int iconIndex)
		{
			iconFile ??= string.Empty;
			var ext = Path.GetExtension(filePath).ToLowerInvariant();

			try
			{
				return ext switch
				{
					".lnk" => TrySetLnkShortcutIcon(filePath, iconFile, iconIndex),
					".url" => TrySetUrlShortcutIcon(filePath, iconFile, iconIndex),
					_ => false,
				};
			}
			catch (UnauthorizedAccessException)
			{
				string psScript;
				filePath = filePath.Replace("'", "''");
				iconFile = iconFile.Replace("'", "''");

				if(ext == ".url")
				{
					psScript = $@"
						$path = '{filePath}'
						$iconFile = '{iconFile}'
						$iconIndex = '{iconIndex}'
						$content = Get-Content -LiteralPath $path
                
						$content = $content | Where-Object {{ $_ -notmatch '^IconFile=' -and $_ -notmatch '^IconIndex=' }}
                
						$newContent = foreach ($line in $content) {{
							$line
							if ($line -eq '[InternetShortcut]') {{
								""IconFile=$iconFile""
								""IconIndex=$iconIndex""
							}}
						}}
						$newContent | Set-Content -LiteralPath $path -Encoding UTF8
					";
				}
				else
				{
					psScript = $@"
						$FilePath = '{filePath}'
						$IconFile = '{iconFile}'
						$IconIndex = '{iconIndex}'

						$Shell = New-Object -ComObject WScript.Shell
						$Shortcut = $Shell.CreateShortcut($FilePath)
						$Shortcut.IconLocation = ""$IconFile, $IconIndex""
						$Shortcut.Save()
					";
				}

				var base64EncodedScript = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(psScript));

				ProcessStartInfo startInfo = new ProcessStartInfo()
				{
					FileName = "powershell.exe",
					Arguments = $"-NoProfile -EncodedCommand {base64EncodedScript}",
					Verb = "runas",
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					UseShellExecute = true
				};

				// Start the process
				Process process = new Process() { StartInfo = startInfo };
				process.Start();
				process.WaitForExit();

				return true;
			}
			catch (Exception ex)
			{
				// Could not create shortcut
				App.Logger.LogWarning(ex, ex.Message);
			}

			return false;
		}

		private static bool TrySetUrlShortcutIcon(string filePath, string iconFile, int iconIndex)
		{
			var fileExist = File.Exists(filePath);
			if (!fileExist)
			{
				return false;
			}

			var lines = File.ReadAllLines(filePath).ToList();
			var hasInternetShortcutHeader = lines.Any(l => l.Trim().Equals("[InternetShortcut]", StringComparison.OrdinalIgnoreCase));

			if (!hasInternetShortcutHeader)
			{
				return false;
			}

			lines.RemoveAll(l =>
				l.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase) ||
				l.StartsWith("IconIndex=", StringComparison.OrdinalIgnoreCase));

			int index = 0;
			int insertedIndex = 0;
			foreach(var line in lines)
			{
				var isInternetShortcutHeader = line.Trim().Equals("[InternetShortcut]", StringComparison.OrdinalIgnoreCase);
				if(isInternetShortcutHeader)
				{
					insertedIndex = index + 1;
					break;
				}

				index++;
			}

			if (insertedIndex > 0 && !string.IsNullOrEmpty(iconFile))
			{
				lines.Insert(insertedIndex, $"IconFile={iconFile}");
				lines.Insert(insertedIndex + 1, $"IconIndex={iconIndex}");
			}

			File.WriteAllLines(filePath, lines);

			return true;
		}

		private static bool TrySetLnkShortcutIcon(string filePath, string iconFile, int iconIndex)
		{
			using var link = new ShellLink(filePath, SLR_FLAGS.SLR_NO_UI_WITH_MSG_PUMP, timeout: TimeSpan.FromMilliseconds(100));
			if (string.IsNullOrWhiteSpace(iconFile))
			{
				link.IconLocation = new IconLocation(string.Empty, 0);
			}
			else
			{
				link.IconLocation = new IconLocation(iconFile, iconIndex);
			}
			link.SaveAs(filePath); // Overwrite if exists

			return true;
		}

		public static Task<string?> OpenObjectPickerAsync(long hWnd)
			=> WindowsObjectPicker.OpenObjectPickerAsync((nint)hWnd, App.Logger);

		private static ShellItem? GetFirstFile(ShellItem shi)
		{
			if (!shi.IsFolder || shi.IsStream)
			{
				return shi;
			}
			using var shf = new ShellFolder(shi);
			if (shf.FirstOrDefault(x => !x.IsFolder || x.IsStream) is ShellItem item)
			{
				return item;
			}
			foreach (var shsfi in shf.Where(x => x.IsFolder && !x.IsStream))
			{
				using var shsf = new ShellFolder(shsfi);
				if (GetFirstFile(shsf) is ShellItem item2)
				{
					return item2;
				}
			}
			return null;
		}

		private static void UpdateFileTagsDb(ShellFileOperations2.ShellFileOpEventArgs e, string operationType)
		{
			var dbInstance = FileTagsHelper.GetDbInstance();
			if (e.Result.Succeeded && e.SourceItem.GetParsingPath() is { } sourcePath)
			{
				var destPath = e.DestFolder.GetParsingPath();
				var sourceFolderPath = Path.GetDirectoryName(sourcePath);
				var destination = operationType switch
				{
					"delete" => e.DestItem.GetParsingPath(),
					"rename" => sourceFolderPath is not null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(sourceFolderPath, e.Name) : null,
					"copy" => destPath is not null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(destPath, e.Name) : null,
					_ => destPath is not null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(destPath, e.Name) : null
				};
				if (destination is null)
				{
					dbInstance.SetTags(sourcePath, null, []); // remove tag from deleted files
				}
				else
				{
					SafetyExtensions.IgnoreExceptions(() =>
					{
						if (operationType == "copy")
						{
							var tag = dbInstance.GetTags(sourcePath, null);

							dbInstance.SetTags(destination, FileTagsHelper.GetFileFRN(destination), tag); // copy tag to new files
							using var si = new ShellItem(destination);
							if (si.IsFolder) // File tag is not copied automatically for folders
							{
								_ = FileTagsHelper.WriteFileTagAsync(destination, tag);
							}
						}
						else
						{
							dbInstance.UpdateTag(sourcePath, FileTagsHelper.GetFileFRN(destination), destination); // move tag to new files
						}
					}, App.Logger);
				}
				if (e.Result == HRESULT.COPYENGINE_S_DONT_PROCESS_CHILDREN) // child items not processed, update manually
				{
					var tags = dbInstance.GetAllUnderPath(sourcePath).ToList();
					if (destination is null) // remove tag for items contained in the folder
					{
						tags.ForEach(t => dbInstance.SetTags(t.FilePath, null, []));
					}
					else
					{
						if (operationType == "copy") // copy tag for items contained in the folder
						{
							tags.ForEach(t =>
							{
								SafetyExtensions.IgnoreExceptions(() =>
								{
									var subPath = t.FilePath.Replace(sourcePath, destination, StringComparison.Ordinal);
									dbInstance.SetTags(subPath, FileTagsHelper.GetFileFRN(subPath), t.Tags ?? []);
								}, App.Logger);
							});
						}
						else // move tag to new files
						{
							tags.ForEach(t =>
							{
								SafetyExtensions.IgnoreExceptions(() =>
								{
									var subPath = t.FilePath.Replace(sourcePath, destination, StringComparison.Ordinal);
									dbInstance.UpdateTag(t.FilePath, FileTagsHelper.GetFileFRN(subPath), subPath);
								}, App.Logger);
							});
						}
					}
				}
			}
		}

		public static void WaitForCompletion()
			=> progressHandler?.WaitForCompletion();

		private sealed partial class ProgressHandler : Disposable
		{
			private readonly ManualResetEvent operationsCompletedEvent;

			public sealed class OperationWithProgress
			{
				public double Progress { get; set; }
				public bool Canceled { get; set; }
			}

			private readonly ConcurrentDictionary<string, OperationWithProgress> operations;

			public HWND OwnerWindow { get; set; }

			public ProgressHandler()
			{
				operations = new ConcurrentDictionary<string, OperationWithProgress>();
				operationsCompletedEvent = new ManualResetEvent(true);
			}

			public int Progress
			{
				get
				{
					var ongoing = operations.ToArray().Where(x => !x.Value.Canceled);
					return ongoing.Any() ? (int)ongoing.Average(x => x.Value.Progress) : 0;
				}
			}

			public void AddOperation(string uid)
			{
				operations.TryAdd(uid, new OperationWithProgress());
				operationsCompletedEvent.Reset();
			}

			public void RemoveOperation(string uid)
			{
				operations.TryRemove(uid, out _);
				if (!operations.Any())
				{
					operationsCompletedEvent.Set();
				}
			}

			public void UpdateOperation(string uid, double progress)
			{
				if (operations.TryGetValue(uid, out var op))
				{
					op.Progress = progress;
				}
			}

			public bool CheckCanceled(string uid)
			{
				return !operations.TryGetValue(uid, out var op) || op.Canceled;
			}

			public void TryCancel(string uid)
			{
				if (operations.TryGetValue(uid, out var op))
				{
					op.Canceled = true;
				}
			}

			public void WaitForCompletion()
			{
				operationsCompletedEvent.WaitOne();
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					operationsCompletedEvent?.Dispose();
				}
			}
		}

		private static string GetIncrementalName(bool overWriteOnCopy, string filePathToCheck, string filePathToCopy)
		{
			if ((!Path.Exists(filePathToCheck)) || overWriteOnCopy || filePathToCheck == filePathToCopy)
				return Path.GetFileName(filePathToCheck);

			var index = 2;
			var filePath = filePathToCheck;
			if (Path.HasExtension(filePathToCheck))
				filePath = filePathToCheck.Substring(0, filePathToCheck.LastIndexOf('.'));

			Func<int, string> genFilePath = x => string.Concat([filePath, " (", x.ToString(), ")", Path.GetExtension(filePathToCheck)]);

			while (Path.Exists(genFilePath(index)))
				index++;

			return Path.GetFileName(genFilePath(index));
		}

		private static bool MainStreamExists(string path)
		{
			// Path.Exists is always false for ADS paths (file.txt:stream), so check the main stream's path
			var fileName = Path.GetFileName(path);
			var colonIndex = fileName.IndexOf(':');
			if (colonIndex is not -1)
				path = path[..(path.Length - fileName.Length + colonIndex)];

			return Path.Exists(path);
		}
	}
}
