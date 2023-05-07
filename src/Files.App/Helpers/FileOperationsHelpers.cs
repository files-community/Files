// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem;
using Files.App.Filesystem.Security;
using Files.App.Shell;
using Files.Backend.Helpers;
using Files.Shared;
using Files.Shared.Enums;
using Files.Shared.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Tulpep.ActiveDirectoryObjectPicker;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Helpers
{
	public class FileOperationsHelpers
	{
		private static readonly Ole32.PROPERTYKEY PKEY_FilePlaceholderStatus = new Ole32.PROPERTYKEY(new Guid("B2F9B9D6-FEC4-4DD5-94D7-8957488C807B"), 2);
		private const uint PS_CLOUDFILE_PLACEHOLDER = 8;

		private static ProgressHandler? progressHandler; // Warning: must be initialized from a MTA thread

		public static Task SetClipboard(string[] filesToCopy, DataPackageOperation operation)
		{
			return Win32API.StartSTATask(() =>
			{
				System.Windows.Forms.Clipboard.Clear();
				var fileList = new System.Collections.Specialized.StringCollection();
				fileList.AddRange(filesToCopy);
				MemoryStream dropEffect = new MemoryStream(operation == DataPackageOperation.Copy ?
					new byte[] { 5, 0, 0, 0 } : new byte[] { 2, 0, 0, 0 });
				var data = new System.Windows.Forms.DataObject();
				data.SetFileDropList(fileList);
				data.SetData("Preferred DropEffect", dropEffect);
				System.Windows.Forms.Clipboard.SetDataObject(data, true);
			});
		}

		public static Task<(bool, ShellOperationResult)> CreateItemAsync(string filePath, string fileOp, string template = "", byte[]? dataBytes = null)
		{
			return Win32API.StartSTATask(async () =>
			{
				using var op = new ShellFileOperations();

				op.Options = ShellFileOperations.OperationFlags.Silent
							| ShellFileOperations.OperationFlags.NoConfirmMkDir
							| ShellFileOperations.OperationFlags.RenameOnCollision
							| ShellFileOperations.OperationFlags.NoErrorUI;

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
			return Win32API.StartSTATask(async () =>
			{
				using var op = new ShellFileOperations();

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
						var file = SafetyExtensions.IgnoreExceptions(() => GetFirstFile(shi)) ?? shi;
						if (file.Properties.GetProperty<uint>(PKEY_FilePlaceholderStatus) == PS_CLOUDFILE_PLACEHOLDER)
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

		public static Task<(bool, ShellOperationResult)> DeleteItemAsync(string[] fileToDeletePath, bool permanently, long ownerHwnd, IProgress<FileSystemProgress> progress, string operationID = "")
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			FileSystemProgress fsProgress = new(progress, true, FileSystemStatusCode.InProgress);
			fsProgress.Report();
			progressHandler ??= new();

			return Win32API.StartSTATask(async () =>
			{
				using var op = new ShellFileOperations();
				op.Options = ShellFileOperations.OperationFlags.Silent
							| ShellFileOperations.OperationFlags.NoConfirmation
							| ShellFileOperations.OperationFlags.NoErrorUI;
				op.OwnerWindow = (IntPtr)ownerHwnd;
				if (!permanently)
				{
					op.Options |= ShellFileOperations.OperationFlags.RecycleOnDelete
								| ShellFileOperations.OperationFlags.WantNukeWarning;
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
				op.PreDeleteItem += (s, e) =>
				{
					if (!permanently && !e.Flags.HasFlag(ShellFileOperations.TransferFlags.DeleteRecycleIfPossible))
					{
						throw new Win32Exception(HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND); // E_FAIL, stops operation
					}
				};
				op.PostDeleteItem += (s, e) =>
				{
					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = e.SourceItem.GetParsingPath(),
						Destination = e.DestItem.GetParsingPath(),
						HResult = (int)e.Result
					});
				};
				op.PostDeleteItem += (_, e) => UpdateFileTagsDb(e, "delete");
				op.FinishOperations += (s, e) => deleteTcs.TrySetResult(e.Result.Succeeded);
				op.UpdateProgress += (s, e) =>
				{
					if (progressHandler.CheckCanceled(operationID))
					{
						throw new Win32Exception(unchecked((int)0x80004005)); // E_FAIL, stops operation
					}
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

				return (await deleteTcs.Task, shellOperationResult);
			});
		}

		public static Task<(bool, ShellOperationResult)> RenameItemAsync(string fileToRenamePath, string newName, bool overwriteOnRename, string operationID = "")
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			progressHandler ??= new();

			return Win32API.StartSTATask(async () =>
			{
				using var op = new ShellFileOperations();
				var shellOperationResult = new ShellOperationResult();

				op.Options = ShellFileOperations.OperationFlags.Silent
						  | ShellFileOperations.OperationFlags.NoErrorUI;
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

		public static Task<(bool, ShellOperationResult)> MoveItemAsync(string[] fileToMovePath, string[] moveDestination, bool overwriteOnMove, long ownerHwnd, IProgress<FileSystemProgress> progress, string operationID = "")
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			FileSystemProgress fsProgress = new(progress, true, FileSystemStatusCode.InProgress);
			fsProgress.Report();
			progressHandler ??= new();

			return Win32API.StartSTATask(async () =>
			{
				using var op = new ShellFileOperations();
				var shellOperationResult = new ShellOperationResult();

				op.Options = ShellFileOperations.OperationFlags.NoConfirmMkDir
							| ShellFileOperations.OperationFlags.Silent
							| ShellFileOperations.OperationFlags.NoErrorUI;
				op.OwnerWindow = (IntPtr)ownerHwnd;
				op.Options |= !overwriteOnMove ? ShellFileOperations.OperationFlags.PreserveFileExtensions | ShellFileOperations.OperationFlags.RenameOnCollision
					: ShellFileOperations.OperationFlags.NoConfirmation;

				for (var i = 0; i < fileToMovePath.Length; i++)
				{
					if (!SafetyExtensions.IgnoreExceptions(() =>
					{
						using ShellItem shi = new ShellItem(fileToMovePath[i]);
						using ShellFolder shd = new ShellFolder(Path.GetDirectoryName(moveDestination[i]));
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
				op.PostMoveItem += (s, e) =>
				{
					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = e.SourceItem.GetParsingPath(),
						Destination = e.DestFolder.GetParsingPath() is not null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(e.DestFolder.GetParsingPath(), e.Name) : null,
						HResult = (int)e.Result
					});
				};
				op.PostMoveItem += (_, e) => UpdateFileTagsDb(e, "move");
				op.FinishOperations += (s, e) => moveTcs.TrySetResult(e.Result.Succeeded);
				op.UpdateProgress += (s, e) =>
				{
					if (progressHandler.CheckCanceled(operationID))
					{
						throw new Win32Exception(unchecked((int)0x80004005)); // E_FAIL, stops operation
					}
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

				return (await moveTcs.Task, shellOperationResult);
			});
		}

		public static Task<(bool, ShellOperationResult)> CopyItemAsync(string[] fileToCopyPath, string[] copyDestination, bool overwriteOnCopy, long ownerHwnd, IProgress<FileSystemProgress> progress, string operationID = "")
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			FileSystemProgress fsProgress = new(progress, true, FileSystemStatusCode.InProgress);
			fsProgress.Report();
			progressHandler ??= new();

			return Win32API.StartSTATask(async () =>
			{
				using var op = new ShellFileOperations();

				var shellOperationResult = new ShellOperationResult();

				op.Options = ShellFileOperations.OperationFlags.NoConfirmMkDir
							| ShellFileOperations.OperationFlags.Silent
							| ShellFileOperations.OperationFlags.NoErrorUI;
				op.OwnerWindow = (IntPtr)ownerHwnd;
				op.Options |= !overwriteOnCopy ? ShellFileOperations.OperationFlags.PreserveFileExtensions | ShellFileOperations.OperationFlags.RenameOnCollision
					: ShellFileOperations.OperationFlags.NoConfirmation;

				for (var i = 0; i < fileToCopyPath.Length; i++)
				{
					if (!SafetyExtensions.IgnoreExceptions(() =>
					{
						using ShellItem shi = new ShellItem(fileToCopyPath[i]);
						using ShellFolder shd = new ShellFolder(Path.GetDirectoryName(copyDestination[i]));
						op.QueueCopyOperation(shi, shd, Path.GetFileName(copyDestination[i]));
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
				op.PostCopyItem += (s, e) =>
				{
					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = e.SourceItem.GetParsingPath(),
						Destination = e.DestFolder.GetParsingPath() is not null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(e.DestFolder.GetParsingPath(), e.Name) : null,
						HResult = (int)e.Result
					});
				};
				op.PostCopyItem += (_, e) => UpdateFileTagsDb(e, "copy");
				op.FinishOperations += (s, e) => copyTcs.TrySetResult(e.Result.Succeeded);
				op.UpdateProgress += (s, e) =>
				{
					if (progressHandler.CheckCanceled(operationID))
					{
						throw new Win32Exception(unchecked((int)0x80004005)); // E_FAIL, stops operation
					}
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

				return (await copyTcs.Task, shellOperationResult);
			});
		}

		public static void TryCancelOperation(string operationId)
			=> progressHandler?.TryCancel(operationId);

		public static IEnumerable<Win32Process>? CheckFileInUse(string[] fileToCheckPath)
		{
			var processes = SafetyExtensions.IgnoreExceptions(() => FileUtils.WhoIsLocking(fileToCheckPath), App.Logger);

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
					targetPath = await Win32API.StartSTATask(() =>
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

		public static Task<bool> CreateOrUpdateLinkAsync(string linkSavePath, string targetPath, string arguments = "", string workingDirectory = "", bool runAsAdmin = false)
		{
			try
			{
				if (FileExtensionHelpers.IsShortcutFile(linkSavePath))
				{
					using var newLink = new ShellLink(targetPath, arguments, workingDirectory);
					newLink.RunAsAdministrator = runAsAdmin;
					newLink.SaveAs(linkSavePath); // Overwrite if exists
					return Task.FromResult(true);
				}
				else if (FileExtensionHelpers.IsWebLinkFile(linkSavePath))
				{
					return Win32API.StartSTATask(() =>
					{
						var ipf = new Url.IUniformResourceLocator();
						ipf.SetUrl(targetPath, Url.IURL_SETURL_FLAGS.IURL_SETURL_FL_GUESS_PROTOCOL);
						(ipf as System.Runtime.InteropServices.ComTypes.IPersistFile).Save(linkSavePath, false); // Overwrite if exists
						return true;
					});
				}
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
			catch (Exception ex)
			{
				// Could not create shortcut
				App.Logger.LogWarning(ex, ex.Message);
			}

			return false;
		}

		public static AccessControlList GetFilePermissions(string filePath, bool isFolder)
			=> FileSecurityHelpers.GetAccessControlList(filePath, isFolder);

		public static bool SetFileOwner(string filePath, string ownerSid)
			=> FileSecurityHelpers.SetOwner(filePath, ownerSid);

		public static bool SetAccessRuleProtection(string filePath, bool isFolder, bool isProtected, bool preserveInheritance)
			=> FileSecurityHelpers.SetAccessControlProtection(filePath, isFolder, isProtected, preserveInheritance);

		public static Task<string?> OpenObjectPickerAsync(long hWnd)
		{
			return Win32API.StartSTATask(() =>
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
					if (picker.ShowDialog(Win32API.Win32Window.FromLong(hWnd)) == System.Windows.Forms.DialogResult.OK)
					{
						try
						{
							var attribs = picker.SelectedObject.FetchedAttributes;
							if (attribs.Any() && attribs[0] is byte[] objectSid)
							{
								return new SecurityIdentifier(objectSid, 0).Value;
							}
						}
						catch
						{
						}
					}
				}

				return null;
			});
		}

		public static string? ReadCompatOptions(string filePath)
			=> SafetyExtensions.IgnoreExceptions(() =>
			{
				using var compatKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers");
				if (compatKey is null)
				{
					return null;
				}
				return (string?)compatKey.GetValue(filePath, null);
			}, App.Logger);

		public static bool SetCompatOptions(string filePath, string options)
		{
			if (string.IsNullOrEmpty(options) || options == "~")
			{
				return Win32API.RunPowershellCommand(@$"Remove-ItemProperty -Path 'HKCU:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers' -Name '{filePath}' | Out-Null", false);
			}

			return Win32API.RunPowershellCommand(@$"New-ItemProperty -Path 'HKCU:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers' -Name '{filePath}' -Value '{options}' -PropertyType String -Force | Out-Null", false);
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

		private static void UpdateFileTagsDb(ShellFileOperations.ShellFileOpEventArgs e, string operationType)
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
					dbInstance.SetTags(sourcePath, null, null); // remove tag from deleted files
				}
				else
				{
					SafetyExtensions.IgnoreExceptions(() =>
					{
						if (operationType == "copy")
						{
							var tag = dbInstance.GetTags(sourcePath);

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
						tags.ForEach(t => dbInstance.SetTags(t.FilePath, null, null));
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
									dbInstance.SetTags(subPath, FileTagsHelper.GetFileFRN(subPath), t.Tags);
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

		private class ProgressHandler : Disposable
		{
			private readonly ManualResetEvent operationsCompletedEvent;

			private class OperationWithProgress
			{
				public int Progress { get; set; }
				public bool Canceled { get; set; }
			}

			private readonly Shell32.ITaskbarList4 taskbar;
			private readonly ConcurrentDictionary<string, OperationWithProgress> operations;

			public HWND OwnerWindow { get; set; }

			public ProgressHandler()
			{
				taskbar = Win32API.CreateTaskbarObject()!;
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

			public void UpdateOperation(string uid, int progress)
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
					Marshal.ReleaseComObject(taskbar);
				}
			}
		}
	}
}
