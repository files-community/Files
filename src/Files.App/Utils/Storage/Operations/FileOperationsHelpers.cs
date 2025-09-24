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

		public static Task SetClipboard(string[] filesToCopy, DataPackageOperation operation)
		{
			return Win32Helper.StartSTATask(() =>
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
			});
		}

		public static Task<(bool, ShellOperationResult)> CreateItemAsync(string filePath, string fileOp, long ownerHwnd, bool asAdmin, string template = "", byte[]? dataBytes = null)
		{
			return Win32Helper.StartSTATask(async () =>
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
			});
		}

		public static Task<(bool, ShellOperationResult)> TestRecycleAsync(string[] fileToDeletePath)
		{
			return Win32Helper.StartSTATask(async () =>
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
			});
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

			return Win32Helper.StartSTATask(async () =>
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
			});
		}

		public static Task<(bool, ShellOperationResult)> RenameItemAsync(string fileToRenamePath, string newName, bool overwriteOnRename, long ownerHwnd, bool asAdmin, string operationID = "")
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			progressHandler ??= new();

			return Win32Helper.StartSTATask(async () =>
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
			});
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

			return Win32Helper.StartSTATask(async () =>
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
			});
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

			return Win32Helper.StartSTATask(async () =>
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
			});
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

		private static async Task<(bool success, int exitCode)> RunRobocopyAsync(string arguments, StatusCenterItemProgressModel progressModel, string operationID, CancellationToken cancellationToken)
		{
			try
			{
				App.Logger?.LogInformation($"Robocopy operation {operationID}: Starting with arguments: {arguments}");

				var psi = new ProcessStartInfo
				{
					FileName = "robocopy.exe",
					Arguments = arguments,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					StandardOutputEncoding = System.Text.Encoding.UTF8,
					StandardErrorEncoding = System.Text.Encoding.UTF8
				};

				using var process = new Process { StartInfo = psi };

				int completedFiles = 0;
				int totalFiles = 0;

				process.OutputDataReceived += (sender, e) =>
				{
					if (!string.IsNullOrEmpty(e.Data))
					{
						// Parse for file completion lines (e.g., "100%    12345   filename.ext")
						if (e.Data.Contains('%') && e.Data.Length > 10)
						{
							completedFiles++;
							// Update progress for every few files to avoid too frequent updates
							if (completedFiles % 5 == 0)
							{
								// Estimate total progress within this operation
								// This is a simple approach - we don't know exact total, so we use a rolling estimate
								var estimatedProgress = Math.Min(95, completedFiles * 2); // Conservative estimate
								progressModel.Report(estimatedProgress);
							}
						}
						// Parse summary lines for total count (e.g., "Files : 5  0  5")
						else if (e.Data.StartsWith("Files :", StringComparison.OrdinalIgnoreCase))
						{
							var parts = e.Data.Split(' ', StringSplitOptions.RemoveEmptyEntries);
							if (parts.Length >= 4 && int.TryParse(parts[1], out var copied) &&
								int.TryParse(parts[2], out var skipped) &&
								int.TryParse(parts[3], out var failed))
							{
								totalFiles = copied + skipped + failed;
							}
						}
					}
				};

				process.Start();

				// Verify process started successfully
				if (process.Id <= 0)
				{
					return (false, -4); // Process start failure
				}

				// Begin async reading of output
				process.BeginOutputReadLine();

				// Start reading error stream
				var errorTask = Task.Run(() =>
				{
					try
					{
						return process.StandardError.ReadToEnd();
					}
					catch
					{
						return string.Empty;
					}
				});

				using var registration = cancellationToken.Register(() =>
				{
					try
					{
						if (!process.HasExited)
						{
							process.Kill();
						}
					}
					catch { }
				});

				// Wait for the process to exit with a reasonable timeout
				var exitTask = process.WaitForExitAsync(cancellationToken);
				var timeoutTask = Task.Delay(TimeSpan.FromMinutes(30), cancellationToken);

				var completedTask = await Task.WhenAny(exitTask, timeoutTask);

				if (completedTask == timeoutTask)
				{
					try
					{
						if (!process.HasExited)
							process.Kill();
					}
					catch { }
					return (false, -2); // Timeout error
				}

				// Wait for error reading to complete (with timeout)
				try
				{
					await Task.WhenAny(errorTask, Task.Delay(5000, CancellationToken.None));
				}
				catch { }

				var exitCode = process.ExitCode;
				var success = exitCode >= 0 && exitCode <= 7;

				App.Logger?.LogInformation($"Robocopy operation {operationID}: Completed with exit code {exitCode}, success: {success}, processed {completedFiles} files");

				return (success, exitCode);
			}
			catch (OperationCanceledException)
			{
				App.Logger?.LogWarning($"Robocopy operation {operationID}: Cancelled");
				return (false, -3); // Cancelled
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

			var cts = new CancellationTokenSource();
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

			return Win32Helper.StartSTATask(async () =>
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

					var threads = Math.Clamp(Ioc.Default.GetRequiredService<IUserSettingsService>().DevToolsSettingsService.RobocopyThreads, 1, 128);

					// Create batches for files only (folders will be processed individually)
					(Dictionary<(string sourceDir, string destDir), List<List<string>>> fileBatchesByGroup, int totalFileBatches) = CreateBatchesForFileGroups(fileGroups);

					var totalOperations = totalFileBatches + folderItems.Count;
					App.Logger?.LogInformation($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Created {fileBatchesByGroup.Sum(g => g.Value.Count)} file batches and {folderItems.Count} folder operations (total: {totalOperations})");

					// Execute file batches per source/destination directory combo (8000 chars max)
					var completed = 0;
					foreach (var groupKvp in fileBatchesByGroup)
					{
						if (cts.Token.IsCancellationRequested)
							break;

						(string sourceDir, string destDir) = groupKvp.Key;
						var groupBatches = groupKvp.Value;

						App.Logger?.LogInformation($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Processing file group ({sourceDir}, {destDir}) with {groupBatches.Count} batches");

						foreach (var itemNames in groupBatches)
						{
							if (cts.Token.IsCancellationRequested)
								break;

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
								$"/MT:{threads}"
							};

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
							(batchOk, exitCode) = await RunRobocopyAsync(robocopyArgs, fsProgress, operationID, cts.Token);

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
							var progressPercent = 100.0 * completed / Math.Max(1, totalOperations);
							fsProgress.Report(progressPercent);

							// Refresh UI periodically to show files (every batch for better responsiveness)
							if (shellPage is not null)
							{
								await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
									shellPage.ShellViewModel.RefreshItems(null));
							}
						}
					}

					// Process folders individually
					foreach (var (sourcePath, destPath) in folderItems)
					{
						if (cts.Token.IsCancellationRequested)
							break;

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
							$"/MT:{threads}"
						};

						// Add operation-specific flags
						if (isMoveOperation)
							argsList.Add("/MOVE");

						var robocopyArgs = string.Join(" ", argsList);

						App.Logger?.LogInformation($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Processing folder {sourcePath} -> {destPath}");
						(folderOk, exitCode) = await RunRobocopyAsync(robocopyArgs, fsProgress, operationID, cts.Token);

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
						var progressPercent = 100.0 * completed / Math.Max(1, totalOperations);
						fsProgress.Report(progressPercent);

						// Refresh UI periodically to show folders
						if (shellPage is not null)
						{
							await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
								shellPage.ShellViewModel.RefreshItems(null));
						}
					}

					progressHandler.RemoveOperation(operationID);
					cts.Cancel();

					App.Logger?.LogInformation($"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Completed with overall success: {success}");
				}
				catch (Exception ex)
				{
					App.Logger?.LogError(ex, $"Robocopy {(isMoveOperation ? "move" : "copy")} operation {operationID}: Failed with exception");
					success = false;
					progressHandler.RemoveOperation(operationID);
					cts.Cancel();
				}

				return (success, shellOperationResult);
			});
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

			var cts = new CancellationTokenSource();
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

			return Win32Helper.StartSTATask(async () =>
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

					var threads = Math.Clamp(Ioc.Default.GetRequiredService<IUserSettingsService>().DevToolsSettingsService.RobocopyThreads, 1, 128);

					// Step 2: Move files to temp folder first (reuse existing move logic)
					var tempDestinations = new string[filePaths.Length];
					for (var i = 0; i < filePaths.Length; i++)
					{
						var fileName = Path.GetFileName(filePaths[i]);
						tempDestinations[i] = Path.Combine(tempDeleteFolder, fileName);
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
						var robocopyArgs = $"\"{emptyFolder}\" \"{tempDeleteFolder}\" /MIR /MT:{threads} /R:0 /W:0 /NJH /NJS";

						App.Logger?.LogInformation($"Robocopy delete operation {operationID}: Starting MIR deletion with args: {robocopyArgs}");

						// Create a progress handler that maps MIR progress from 50-100%
						var mirProgressModel = new StatusCenterItemProgressModel(
							new Progress<StatusCenterItemProgressModel>(p =>
							{
								// Map progress from 0-95% (from RunRobocopyAsync) to 50-100%
								var mappedProgress = 50 + (p.Percentage * 0.5);
								fsProgress.Report(mappedProgress);
							}),
							false,
							FileSystemStatusCode.InProgress);

						var (deleteSuccess, exitCode) = await RunRobocopyAsync(robocopyArgs, mirProgressModel, operationID, cts.Token);

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

					progressHandler.RemoveOperation(operationID);
					cts.Cancel();
				}
				catch (Exception ex)
				{
					App.Logger?.LogError(ex, $"Robocopy delete operation {operationID}: Failed with exception");
					success = false;
					progressHandler.RemoveOperation(operationID);
					cts.Cancel();
				}

				return (success, shellOperationResult);
			});
		}

		public static void TryCancelOperation(string operationId)
			=> progressHandler?.TryCancel(operationId);

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
					targetPath = await Win32Helper.StartSTATask(() =>
					{
						var ipf = new Url.IUniformResourceLocator();
						(ipf as System.Runtime.InteropServices.ComTypes.IPersistFile).Load(linkPath, 0);
						ipf.GetUrl(out var retVal);
						return retVal;
					});
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
					return Win32Helper.StartSTATask(() =>
					{
						var ipf = new Url.IUniformResourceLocator();
						ipf.SetUrl(targetPath, Url.IURL_SETURL_FLAGS.IURL_SETURL_FL_GUESS_PROTOCOL);
						(ipf as System.Runtime.InteropServices.ComTypes.IPersistFile).Save(linkSavePath, false); // Overwrite if exists
						return true;
					});
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
			try
			{
				using var link = new ShellLink(filePath, LinkResolution.NoUIWithMsgPump, default, TimeSpan.FromMilliseconds(100));
				link.IconLocation = new IconLocation(iconFile, iconIndex);
				link.SaveAs(filePath); // Overwrite if exists
				return true;
			}
			catch (UnauthorizedAccessException)
			{
				string psScript = $@"
					$FilePath = '{filePath}'
					$IconFile = '{iconFile}'
					$IconIndex = '{iconIndex}'

					$Shell = New-Object -ComObject WScript.Shell
					$Shortcut = $Shell.CreateShortcut($FilePath)
					$Shortcut.IconLocation = ""$IconFile, $IconIndex""
					$Shortcut.Save()
				";

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

		public static Task<string?> OpenObjectPickerAsync(long hWnd)
		{
			return Win32Helper.StartSTATask(() =>
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
			});
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
				filePath = filePathToCheck.Substring(0, filePathToCheck.LastIndexOf("."));

			Func<int, string> genFilePath = x => string.Concat([filePath, " (", x.ToString(), ")", Path.GetExtension(filePathToCheck)]);

			while (Path.Exists(genFilePath(index)))
				index++;

			return Path.GetFileName(genFilePath(index));
		}
	}
}
