// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils.Storage.Operations;
using Files.Shared.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Tulpep.ActiveDirectoryObjectPicker;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.ApplicationModel.DataTransfer;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Utils.Storage
{
	public sealed partial class FileOperationsHelpers
	{
		private static readonly Ole32.PROPERTYKEY PKEY_FilePlaceholderStatus = new Ole32.PROPERTYKEY(new Guid("B2F9B9D6-FEC4-4DD5-94D7-8957488C807B"), 2);
		private const uint PS_CLOUDFILE_PLACEHOLDER = 8;

		private static ProgressHandler? progressHandler; // Warning: must be initialized from a MTA thread
		private static readonly ConcurrentDictionary<string, CancellationTokenSource> robocopyOperationTokens = new();

		public static Task SetClipboard(string[] filesToCopy, DataPackageOperation operation)
		{
			return STATask.Run(() =>
			{
				System.Windows.Forms.Clipboard.Clear();
				var fileList = new System.Collections.Specialized.StringCollection();
				fileList.AddRange(filesToCopy);
				MemoryStream dropEffect = new MemoryStream(operation == DataPackageOperation.Copy ?
					[5, 0, 0, 0] : [2, 0, 0, 0]);
				var data = new System.Windows.Forms.DataObject();
				data.SetFileDropList(fileList);
				data.SetData("Preferred DropEffect", dropEffect);
				System.Windows.Forms.Clipboard.SetDataObject(data, true);
			}, App.Logger);
		}

		public static Task<(bool, ShellOperationResult)> CreateItemAsync(string filePath, string fileOp, long ownerHwnd, bool asAdmin, string template = "", byte[]? dataBytes = null)
		{
			return STATask.Run(async () =>
			{
				using var op = new ShellFileOperations2();

				op.Options = ShellFileOperations.OperationFlags.Silent
							| ShellFileOperations.OperationFlags.NoConfirmMkDir
							| ShellFileOperations.OperationFlags.RenameOnCollision
							| ShellFileOperations.OperationFlags.NoErrorUI;
				if (asAdmin)
				{
					op.Options |= ShellFileOperations.OperationFlags.ShowElevationPrompt
								| ShellFileOperations.OperationFlags.RequireElevation;
				}
				op.OwnerWindow = (IntPtr)ownerHwnd;

				var shellOperationResult = new ShellOperationResult();

				if (!SafetyExtensions.IgnoreExceptions(() =>
				{
					using var shd = new ShellFolder(Path.GetDirectoryName(filePath));
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

				if (dataBytes is not null && (shellOperationResult.Items.SingleOrDefault()?.Succeeded ?? false))
				{
					SafetyExtensions.IgnoreExceptions(() =>
					{
						using var fs = new FileStream(shellOperationResult.Items.Single().Destination, FileMode.Open);
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

				op.Options = ShellFileOperations.OperationFlags.Silent
							| ShellFileOperations.OperationFlags.NoConfirmation
							| ShellFileOperations.OperationFlags.NoErrorUI;
				op.Options |= ShellFileOperations.OperationFlags.RecycleOnDelete;

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
								HResult = HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND
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
					if (!e.Flags.HasFlag(ShellFileOperations.TransferFlags.DeleteRecycleIfPossible))
					{
						shellOperationResult.Items.Add(new ShellOperationItemResult()
						{
							Succeeded = false,
							Source = e.SourceItem.GetParsingPath(),
							HResult = HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND
						});
						throw new Win32Exception(HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND); // E_FAIL, stops operation
					}
					else
					{
						shellOperationResult.Items.Add(new ShellOperationItemResult()
						{
							Succeeded = true,
							Source = e.SourceItem.GetParsingPath(),
							HResult = HRESULT.COPYENGINE_E_USER_CANCELLED
						});
						throw new Win32Exception(HRESULT.COPYENGINE_E_USER_CANCELLED); // E_FAIL, stops operation
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

		public static Task<(bool, ShellOperationResult)> DeleteItemAsync(string[] fileToDeletePath, bool permanently, long ownerHwnd, bool asAdmin, IProgress<StatusCenterItemProgressModel> progress, string operationID = "")
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

				op.Options =
					ShellFileOperations.OperationFlags.Silent |
					ShellFileOperations.OperationFlags.NoConfirmation |
					ShellFileOperations.OperationFlags.NoErrorUI;

				if (asAdmin)
				{
					op.Options |=
						ShellFileOperations.OperationFlags.ShowElevationPrompt |
						ShellFileOperations.OperationFlags.RequireElevation;
				}

				op.OwnerWindow = (IntPtr)ownerHwnd;

				if (!permanently)
				{
					op.Options |=
						ShellFileOperations.OperationFlags.RecycleOnDelete |
						ShellFileOperations.OperationFlags.WantNukeWarning;
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
					sizeCalculator.ForceComputeFileSize(e.SourceItem.GetParsingPath());
					fsProgress.FileName = e.SourceItem.Name;
					fsProgress.Report();
				};

				// Right after deleted item
				op.PostDeleteItem += (s, e) =>
				{
					if (!e.SourceItem.IsFolder)
					{
						if (sizeCalculator.TryGetComputedFileSize(e.SourceItem.GetParsingPath(), out _))
							fsProgress.AddProcessedItemsCount(1);
					}

					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = e.SourceItem.GetParsingPath(),
						Destination = e.DestItem.GetParsingPath(),
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

				op.Options = ShellFileOperations.OperationFlags.Silent
						  | ShellFileOperations.OperationFlags.NoErrorUI;
				if (asAdmin)
				{
					op.Options |= ShellFileOperations.OperationFlags.ShowElevationPrompt
							| ShellFileOperations.OperationFlags.RequireElevation;
				}
				op.OwnerWindow = (IntPtr)ownerHwnd;
				op.Options |= !overwriteOnRename ? ShellFileOperations.OperationFlags.RenameOnCollision : 0;

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
					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = e.SourceItem.GetParsingPath(),
						Destination = !string.IsNullOrEmpty(e.Name) ? Path.Combine(Path.GetDirectoryName(e.SourceItem.GetParsingPath()), e.Name) : null,
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

				op.Options =
					ShellFileOperations.OperationFlags.NoConfirmMkDir |
					ShellFileOperations.OperationFlags.Silent |
					ShellFileOperations.OperationFlags.NoErrorUI;

				if (asAdmin)
				{
					op.Options |=
						ShellFileOperations.OperationFlags.ShowElevationPrompt |
						ShellFileOperations.OperationFlags.RequireElevation;
				}

				op.OwnerWindow = (IntPtr)ownerHwnd;

				op.Options |=
					!overwriteOnMove
						? ShellFileOperations.OperationFlags.PreserveFileExtensions | ShellFileOperations.OperationFlags.RenameOnCollision
						: ShellFileOperations.OperationFlags.NoConfirmation;

				for (var i = 0; i < fileToMovePath.Length; i++)
				{
					if (!SafetyExtensions.IgnoreExceptions(() =>
					{
						using ShellItem shi = new(fileToMovePath[i]);
						using ShellFolder shd = new(Path.GetDirectoryName(moveDestination[i]));

						op.QueueMoveOperation(shi, shd, Path.GetFileName(moveDestination[i]));
					}))
					{
						shellOperationResult.Items.Add(new ShellOperationItemResult()
						{
							Succeeded = false,
							Source = fileToMovePath[i],
							Destination = moveDestination[i],
							HResult = -1
						});
					}
				}

				progressHandler.OwnerWindow = op.OwnerWindow;
				progressHandler.AddOperation(operationID);

				var moveTcs = new TaskCompletionSource<bool>();

				op.PreMoveItem += (s, e) =>
				{
					sizeCalculator.ForceComputeFileSize(e.SourceItem.GetParsingPath());
					fsProgress.FileName = e.SourceItem.Name;
					fsProgress.Report();
				};

				op.PostMoveItem += (s, e) =>
				{
					if (!e.SourceItem.IsFolder)
					{
						if (sizeCalculator.TryGetComputedFileSize(e.SourceItem.GetParsingPath(), out _))
							fsProgress.AddProcessedItemsCount(1);
					}

					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = e.SourceItem.GetParsingPath(),
						Destination = e.DestFolder.GetParsingPath() is not null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(e.DestFolder.GetParsingPath(), e.Name) : null,
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

				op.Options =
					ShellFileOperations.OperationFlags.NoConfirmMkDir |
					ShellFileOperations.OperationFlags.Silent |
					ShellFileOperations.OperationFlags.NoErrorUI;

				if (asAdmin)
				{
					op.Options |=
						ShellFileOperations.OperationFlags.ShowElevationPrompt |
						ShellFileOperations.OperationFlags.RequireElevation;
				}

				op.OwnerWindow = (IntPtr)ownerHwnd;

				op.Options |=
					!overwriteOnCopy
						? ShellFileOperations.OperationFlags.PreserveFileExtensions | ShellFileOperations.OperationFlags.RenameOnCollision
						: ShellFileOperations.OperationFlags.NoConfirmation;

				for (var i = 0; i < fileToCopyPath.Length; i++)
				{
					if (!SafetyExtensions.IgnoreExceptions(() =>
					{
						using ShellItem shi = new(fileToCopyPath[i]);
						using ShellFolder shd = new(Path.GetDirectoryName(copyDestination[i]));

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
							HResult = -1
						});
					}
				}

				progressHandler.OwnerWindow = op.OwnerWindow;
				progressHandler.AddOperation(operationID);

				var copyTcs = new TaskCompletionSource<bool>();

				op.PreCopyItem += (s, e) =>
				{
					sizeCalculator.ForceComputeFileSize(e.SourceItem.GetParsingPath());
					fsProgress.FileName = e.SourceItem.Name;
					fsProgress.Report();
				};

				op.PostCopyItem += (s, e) =>
				{
					if (!e.SourceItem.IsFolder)
					{
						if (sizeCalculator.TryGetComputedFileSize(e.SourceItem.GetParsingPath(), out _))
							fsProgress.AddProcessedItemsCount(1);
					}

					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = e.SourceItem.GetParsingPath(),
						Destination = e.DestFolder.GetParsingPath() is not null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(e.DestFolder.GetParsingPath(), e.Name) : null,
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

		public static Task<(bool, ShellOperationResult)> CopyItemWithRobocopyAsync(string[] fileToCopyPath, string[] copyDestination, bool overwriteOnCopy, long ownerHwnd, bool asAdmin, IProgress<StatusCenterItemProgressModel> progress, string operationID = "", IShellPage? shellPage = null)
		{
			return PerformRobocopyOperationAsync(
				fileToCopyPath,
				copyDestination,
				overwriteOnCopy,
				progress,
				operationID,
				shellPage,
				isMoveOperation: false);
		}

		public static Task<(bool, ShellOperationResult)> MoveItemWithRobocopyAsync(string[] fileToMovePath, string[] moveDestination, bool overwriteOnMove, long ownerHwnd, bool asAdmin, IProgress<StatusCenterItemProgressModel> progress, string operationID = "", IShellPage? shellPage = null)
		{
			return PerformRobocopyOperationAsync(
				fileToMovePath,
				moveDestination,
				overwriteOnMove,
				progress,
				operationID,
				shellPage,
				isMoveOperation: true);
		}

		public static Task<(bool, ShellOperationResult)> DeleteItemWithRobocopyAsync(string[] fileToDeletePath, long ownerHwnd, bool asAdmin, IProgress<StatusCenterItemProgressModel> progress, string operationID = "", IShellPage? shellPage = null)
		{
			return PerformRobocopyDeleteOperationAsync(
				fileToDeletePath,
				progress,
				operationID,
				shellPage);
		}

		private static async Task<(bool success, int exitCode)> RunRobocopyAsync(string arguments, StatusCenterItemProgressModel? progressModel, IReadOnlyCollection<string>? expectedItemNames, string operationID, CancellationToken cancellationToken)
		{
			try
			{
				App.Logger?.LogInformation($"Robocopy operation {operationID}: Starting with arguments: {arguments}");

				// Robocopy writes output using the system OEM code page, not UTF-8.
				var oemEncoding = System.Text.Encoding.GetEncoding(
					System.Globalization.CultureInfo.CurrentCulture.TextInfo.OEMCodePage);

				var psi = new ProcessStartInfo
				{
					FileName = "robocopy.exe",
					Arguments = arguments,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					StandardOutputEncoding = oemEncoding,
					StandardErrorEncoding = oemEncoding
				};

				using var process = new Process { StartInfo = psi };
				var outputCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				var remainingItemNames = expectedItemNames is null
					? null
					: new HashSet<string>(expectedItemNames, StringComparer.OrdinalIgnoreCase);
				var initialProcessedSize = progressModel?.ProcessedSize ?? 0;
				long batchProcessedSize = 0;
				process.OutputDataReceived += (_, e) =>
				{
					if (e.Data is null)
					{
						outputCompleted.TrySetResult();
						return;
					}

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
				using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				timeoutCts.CancelAfter(TimeSpan.FromMinutes(30));
				using var registration = timeoutCts.Token.Register(() =>
				{
					try
					{
						if (!process.HasExited)
							process.Kill(entireProcessTree: true);
					}
					catch
					{
					}
				});

				try
				{
					await process.WaitForExitAsync(timeoutCts.Token);
				}
				catch (OperationCanceledException)
				{
					try
					{
						if (!process.HasExited)
							process.Kill(entireProcessTree: true);
					}
					catch
					{
					}

					try
					{
						await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));
						await Task.WhenAll(outputCompleted.Task, errorTask).WaitAsync(TimeSpan.FromSeconds(10));
					}
					catch
					{
						// Do not let a process that resisted termination block later operations.
					}

					if (cancellationToken.IsCancellationRequested)
					{
						App.Logger?.LogWarning($"Robocopy operation {operationID}: Cancelled");
						return (false, -3);
					}

					App.Logger?.LogWarning($"Robocopy operation {operationID}: Timed out");
					return (false, -2);
				}

				await Task.WhenAll(outputCompleted.Task, errorTask);

				var exitCode = process.ExitCode;
				// Bit 2 means mismatched files; treating it as success can hide a partial move.
				var success = exitCode is >= 0 and <= 3;
				if (!success)
				{
					var error = await errorTask;
					App.Logger?.LogWarning($"Robocopy operation {operationID}: Exit code {exitCode}. {error}");
				}
				else
				{
					App.Logger?.LogInformation($"Robocopy operation {operationID}: Completed with exit code {exitCode}");
				}

				return (success, exitCode);
			}
			catch (Exception ex)
			{
				App.Logger?.LogError(ex, $"Robocopy operation {operationID}: Failed with exception");
				return (false, -1);
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
			IProgress<StatusCenterItemProgressModel> progress,
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

			var sizeCalculator = new FileSizeCalculator(filePaths);
			var sizeTask = sizeCalculator.ComputeSizeAsync(cts.Token);
			_ = sizeTask.ContinueWith(task =>
			{
				if (!task.IsCompletedSuccessfully)
					return;

				fsProgress.TotalSize = sizeCalculator.Size;
				fsProgress.ItemsCount = sizeCalculator.ItemsCount;
				fsProgress.EnumerationCompleted = true;
				fsProgress.Report();
			}, TaskScheduler.Default);

			fsProgress.ItemsCount = filePaths.Length;
			fsProgress.Report();
			progressHandler ??= new();

			return Task.Run(async () =>
			{
				var shellOperationResult = new ShellOperationResult();
				var success = true;

				App.Logger?.LogInformation($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Processing {filePaths.Length} items");

				// Initial progress update
				fsProgress.Report(0);

				try
				{
					progressHandler.AddOperation(operationID);

					// Group files and folders separately
					var (fileGroups, folderItems) = GroupFilesAndFolders(filePaths, destinationPaths);

					App.Logger?.LogInformation($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Created {fileGroups.Count} file groups and {folderItems.Count} folder items");

					var threads = Math.Clamp(Ioc.Default.GetRequiredService<IDevToolsSettingsService>().RobocopyThreads, 1, 128);

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
							var exitCode = 0;

							var argsList = new List<string>
							{
								$"\"{sourceDir}\"",
								$"\"{destDir}\"",
								string.Join(" ", itemNames.Select(name =>
									name.Contains(' ') ? $"\"{name}\"" : name)),
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
								argsList.Add("/MOV");

							var robocopyArgs = string.Join(" ", argsList);

							// check if the argsList is longer than 8000 characters
							if (robocopyArgs.Length > 8000)
							{
								App.Logger?.LogWarning($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Args list is longer than 8000 characters, trying anyway");
							}

							App.Logger?.LogInformation($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Executing file batch with {itemNames.Count} items, args length: {robocopyArgs.Length}");
							(batchOk, exitCode) = await RunRobocopyAsync(robocopyArgs, fsProgress, itemNames, operationID, cts.Token);

							// Robocopy exit codes describe the batch, so verify every requested item before
							// reporting success. A skipped move otherwise looks successful while its source remains.
							var batchVerified = true;
							foreach (var itemName in itemNames)
							{
								var sourcePath = Path.Combine(sourceDir, itemName);
								var destinationPath = Path.Combine(destDir, itemName);
								var itemOk = batchOk && StorageHelpers.Exists(destinationPath) &&
									(!isMoveOperation || !StorageHelpers.Exists(sourcePath));
								batchVerified &= itemOk;
								shellOperationResult.Items.Add(new ShellOperationItemResult
								{
									Succeeded = itemOk,
									Source = sourcePath,
									Destination = destinationPath,
									HResult = itemOk ? 0 : exitCode != 0 ? exitCode : -1
								});
							}
							batchOk &= batchVerified;

							if (!batchOk)
							{
								App.Logger?.LogWarning($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: File batch failed with exit code {exitCode}");
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
						var exitCode = 0;

						var argsList = new List<string>
						{
							$"\"{sourcePath}\"",
							$"\"{destPath}\"",
							"/E",
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

						var robocopyArgs = string.Join(" ", argsList);

						App.Logger?.LogInformation($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Processing folder {sourcePath} -> {destPath}");
						(folderOk, exitCode) = await RunRobocopyAsync(robocopyArgs, fsProgress, null, operationID, cts.Token);

						folderOk = folderOk && StorageHelpers.Exists(destPath) &&
							(!isMoveOperation || !StorageHelpers.Exists(sourcePath));
						shellOperationResult.Items.Add(new ShellOperationItemResult
						{
							Succeeded = folderOk,
							Source = sourcePath,
							Destination = destPath,
							HResult = folderOk ? 0 : exitCode != 0 ? exitCode : -1
						});

						if (!folderOk)
						{
							App.Logger?.LogWarning($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Folder operation failed with exit code {exitCode}");
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

					if (shellPage is not null)
					{
						await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
							shellPage.ShellViewModel.RefreshItems(null));
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
					catch (TimeoutException)
					{
					}
					cts.Dispose();
				}

				return (success, shellOperationResult);
			});
		}

		private static string GetUniqueTempDeleteName(string baseName, HashSet<string> usedNames)
		{
			var name = baseName;
			var stem = Path.GetFileNameWithoutExtension(baseName);
			var ext = Path.GetExtension(baseName);
			var i = 1;
			while (!usedNames.Add(name))
				name = $"{stem} ({i++}){ext}";
			return name;
		}

		private static Task<(bool, ShellOperationResult)> PerformRobocopyDeleteOperationAsync(
			string[] filePaths,
			IProgress<StatusCenterItemProgressModel> progress,
			string operationID,
			IShellPage? shellPage)
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			StatusCenterItemProgressModel fsProgress = new(
				progress,
				false,
				FileSystemStatusCode.InProgress);

			CancellationTokenSource cts = new();
			robocopyOperationTokens.TryGetValue(operationID, out var previousCts);
			robocopyOperationTokens[operationID] = cts;

			var sizeCalculator = new FileSizeCalculator(filePaths);
			var sizeTask = sizeCalculator.ComputeSizeAsync(cts.Token);
			sizeTask.ContinueWith(_ =>
			{
				fsProgress.TotalSize = sizeCalculator.Size;
				fsProgress.ItemsCount = filePaths.Length;
				fsProgress.EnumerationCompleted = true;
				fsProgress.Report();
			});

			fsProgress.Report();
			progressHandler ??= new();

			return STATask.Run(async () =>
			{
				var shellOperationResult = new ShellOperationResult();
				var success = true;

				App.Logger?.LogInformation($"Robocopy delete operation {operationID}: Processing {filePaths.Length} items for deletion");

				// Initial progress update
				fsProgress.Report(0);

				try
				{
					progressHandler.AddOperation(operationID);

					// Step 1: Create temp folder structure
					var tempBasePath = Path.GetTempPath();
					var tempDeleteFolder = Path.Combine(tempBasePath, $"Files_Delete_{Guid.NewGuid()}");
					var emptyFolder = Path.Combine(tempBasePath, $"Files_Empty_{Guid.NewGuid()}");

					App.Logger?.LogInformation($"Robocopy delete operation {operationID}: Created temp folders - Delete: {tempDeleteFolder}, Empty: {emptyFolder}");

					// Create temp directories
					Directory.CreateDirectory(tempDeleteFolder);
					Directory.CreateDirectory(emptyFolder);

					var threads = Math.Clamp(Ioc.Default.GetRequiredService<IDevToolsSettingsService>().RobocopyThreads, 1, 128);

					// Step 2: Move files to temp folder first (reuse existing move logic)
					var tempDestinations = new string[filePaths.Length];
					var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					for (var i = 0; i < filePaths.Length; i++)
					{
						var fileName = Path.GetFileName(filePaths[i]);
						var uniqueName = GetUniqueTempDeleteName(fileName, usedNames);
						tempDestinations[i] = Path.Combine(tempDeleteFolder, uniqueName);
					}

					App.Logger?.LogInformation($"Robocopy delete operation {operationID}: Starting move to temp folder");

					// Use existing move functionality to move files to temp
					var (moveSuccess, moveResult) = await PerformRobocopyOperationAsync(
						filePaths,
						tempDestinations,
						true, // overwrite
						new Progress<StatusCenterItemProgressModel>(p => fsProgress.Report(p.Percentage / 2)), // Half progress for move
						operationID,
						shellPage,
						isMoveOperation: true);

					if (!moveSuccess)
					{
						App.Logger?.LogWarning($"Robocopy delete operation {operationID}: Move to temp folder failed");
						success = false;
					}
					else
					{
						App.Logger?.LogInformation($"Robocopy delete operation {operationID}: Move to temp folder completed successfully");
						fsProgress.Report(50); // 50% complete after move

						// Step 3: Use robocopy MIR to delete all data (single command, no batching)
						var robocopyArgs = $"\"{emptyFolder}\" \"{tempDeleteFolder}\" /MIR /MT:{threads} /R:0 /W:0 /NJH /NJS /NP";

						App.Logger?.LogInformation($"Robocopy delete operation {operationID}: Starting MIR deletion with args: {robocopyArgs}");

						var (deleteSuccess, exitCode) = await RunRobocopyAsync(robocopyArgs, null, null, operationID, cts.Token);

						if (!deleteSuccess)
						{
							App.Logger?.LogWarning($"Robocopy delete operation {operationID}: MIR deletion failed with exit code {exitCode}");
							success = false;
						}
						else
						{
							App.Logger?.LogInformation($"Robocopy delete operation {operationID}: MIR deletion completed successfully");
						}

						fsProgress.Report(100); // 100% complete after robocopy MIR
					}

					// Step 4: Clean up temp folders
					try
					{
						App.Logger?.LogInformation($"Robocopy delete operation {operationID}: Cleaning up temp folders");
						if (Directory.Exists(tempDeleteFolder))
							Directory.Delete(tempDeleteFolder, true);
						if (Directory.Exists(emptyFolder))
							Directory.Delete(emptyFolder, true);
						App.Logger?.LogInformation($"Robocopy delete operation {operationID}: Temp folder cleanup completed");
					}
					catch (Exception ex)
					{
						App.Logger?.LogWarning(ex, $"Robocopy delete operation {operationID}: Temp folder cleanup failed");
						// Ignore cleanup errors
					}

					fsProgress.Report(100); // 100% complete

					App.Logger?.LogInformation($"Robocopy delete operation {operationID}: Completed with overall success: {success}");

					// Refresh UI if shellPage is provided
					if (shellPage is not null)
					{
						await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
							shellPage.ShellViewModel.RefreshItems(null));
					}

				}
				catch (Exception ex)
				{
					App.Logger?.LogError(ex, $"Robocopy delete operation {operationID}: Failed with exception");
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
					cts.Dispose();
				}

				return (success, shellOperationResult);
			}, App.Logger);
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
				catch
				{
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

		public static async Task<ShellLinkItem?> ParseLinkAsync(string linkPath)
		{
			if (string.IsNullOrEmpty(linkPath))
				return null;

			string targetPath = string.Empty;

			try
			{
				if (FileExtensionHelpers.IsShortcutFile(linkPath))
				{
					using var link = new ShellLink(linkPath, LinkResolution.NoUIWithMsgPump, default, TimeSpan.FromMilliseconds(100));
					targetPath = link.TargetPath;
					return ShellFolderExtensions.GetShellLinkItem(link);
				}
				else if (FileExtensionHelpers.IsWebLinkFile(linkPath))
				{
					targetPath = await STATask.Run(() =>
					{
						var ipf = new Url.IUniformResourceLocator();
						(ipf as System.Runtime.InteropServices.ComTypes.IPersistFile).Load(linkPath, 0);
						ipf.GetUrl(out var retVal);
						return retVal;
					}, App.Logger);
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

		public static Task<bool> CreateOrUpdateLinkAsync(string linkSavePath, string targetPath, string arguments = "", string workingDirectory = "", bool runAsAdmin = false, SHOW_WINDOW_CMD showWindowCommand = SHOW_WINDOW_CMD.SW_NORMAL)
		{
			try
			{
				if (FileExtensionHelpers.IsShortcutFile(linkSavePath))
				{
					using var newLink = new ShellLink(targetPath, arguments, workingDirectory);

					// Check if the target is a file
					if (File.Exists(targetPath))
						newLink.RunAsAdministrator = runAsAdmin;

					newLink.SaveAs(linkSavePath); // Overwrite if exists

					// ShowState has to be set after SaveAs has been called, otherwise an UnauthorizedAccessException gets thrown in some cases
					newLink.ShowState = (ShowWindowCommand)showWindowCommand;

					return Task.FromResult(true);
				}
				else if (FileExtensionHelpers.IsWebLinkFile(linkSavePath))
				{
					return STATask.Run(() =>
					{
						var ipf = new Url.IUniformResourceLocator();
						ipf.SetUrl(targetPath, Url.IURL_SETURL_FLAGS.IURL_SETURL_FL_GUESS_PROTOCOL);
						(ipf as System.Runtime.InteropServices.ComTypes.IPersistFile).Save(linkSavePath, false); // Overwrite if exists
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

		public static bool SetLinkIcon(string filePath, string iconFile, int iconIndex)
		{
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

			if(insertedIndex > 0)
			{
				lines.Insert(insertedIndex, $"IconFile={iconFile}");
				lines.Insert(insertedIndex + 1, $"IconIndex={iconIndex}");
			}

			File.WriteAllLines(filePath, lines);

			return true;
		}

		private static bool TrySetLnkShortcutIcon(string filePath, string iconFile, int iconIndex)
		{
			using var link = new ShellLink(filePath, LinkResolution.NoUIWithMsgPump, default, TimeSpan.FromMilliseconds(100));
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
		{
			return STATask.Run(() =>
			{
				var picker = new DirectoryObjectPickerDialog()
				{
					AllowedObjectTypes = ObjectTypes.All,
					DefaultObjectTypes = ObjectTypes.Users | ObjectTypes.Groups,
					AllowedLocations = Locations.All,
					DefaultLocations = Locations.LocalComputer,
					MultiSelect = false,
					ShowAdvancedView = true
				};

				picker.AttributesToFetch.Add("objectSid");

				using (picker)
				{
					if (picker.ShowDialog(Win32Helper.Win32Window.FromLong(hWnd)) == System.Windows.Forms.DialogResult.OK)
					{
						try
						{
							var attribs = picker.SelectedObject.FetchedAttributes;
							if (attribs.Any() && attribs[0] is byte[] objectSid)
								return new SecurityIdentifier(objectSid, 0).Value;
						}
						catch { }
					}
				}

				return null;
			}, App.Logger);
		}

		private static ShellItem? GetFirstFile(ShellItem shi)
		{
			if (!shi.IsFolder || shi.Attributes.HasFlag(ShellItemAttribute.Stream))
			{
				return shi;
			}
			using var shf = new ShellFolder(shi);
			if (shf.FirstOrDefault(x => !x.IsFolder || x.Attributes.HasFlag(ShellItemAttribute.Stream)) is ShellItem item)
			{
				return item;
			}
			foreach (var shsfi in shf.Where(x => x.IsFolder && !x.Attributes.HasFlag(ShellItemAttribute.Stream)))
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
			if (e.Result.Succeeded)
			{
				var sourcePath = e.SourceItem.GetParsingPath();
				var destPath = e.DestFolder.GetParsingPath();
				var destination = operationType switch
				{
					"delete" => e.DestItem.GetParsingPath(),
					"rename" => !string.IsNullOrEmpty(e.Name) ? Path.Combine(Path.GetDirectoryName(sourcePath), e.Name) : null,
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
								FileTagsHelper.WriteFileTag(destination, tag);
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

			private readonly Shell32.ITaskbarList4? taskbar;
			private readonly ConcurrentDictionary<string, OperationWithProgress> operations;

			public HWND OwnerWindow { get; set; }

			public ProgressHandler()
			{
				taskbar = Win32Helper.CreateTaskbarObject();
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
				UpdateTaskbarProgress();
				operationsCompletedEvent.Reset();
			}

			public void RemoveOperation(string uid)
			{
				operations.TryRemove(uid, out _);
				UpdateTaskbarProgress();
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
					UpdateTaskbarProgress();
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
					UpdateTaskbarProgress();
				}
			}

			private void UpdateTaskbarProgress()
			{
				if (OwnerWindow == HWND.NULL || taskbar is null)
				{
					return;
				}
				if (operations.Any())
				{
					taskbar.SetProgressValue(OwnerWindow, (ulong)Progress, 100);
				}
				else
				{
					taskbar.SetProgressState(OwnerWindow, Shell32.TBPFLAG.TBPF_NOPROGRESS);
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
					if (taskbar is not null)
						Marshal.ReleaseComObject(taskbar);
				}
			}
		}

		private static string GetIncrementalName(bool overWriteOnCopy, string? filePathToCheck, string? filePathToCopy)
		{
			if (filePathToCheck == null)
				return null;

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
	}
}
