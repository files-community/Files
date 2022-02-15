﻿using Common;
using Files.Common;
using FilesFullTrust.Helpers;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;

namespace FilesFullTrust.MessageHandlers
{
    [SupportedOSPlatform("Windows10.0.10240")]
    public class FileOperationsHandler : Disposable, IMessageHandler
    {
        private FileTagsDb dbInstance;
        private ProgressHandler progressHandler;

        public void Initialize(PipeStream connection)
        {
            string fileTagsDbPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "filetags.db");
            dbInstance = new FileTagsDb(fileTagsDbPath, true);
            progressHandler = new ProgressHandler(connection);
        }

        public async Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "FileOperation":
                    await ParseFileOperationAsync(connection, message);
                    break;
            }
        }

        private async Task ParseFileOperationAsync(PipeStream connection, Dictionary<string, object> message)
        {
            switch (message.Get("fileop", ""))
            {
                case "GetFileHandle":
                    {
                        var filePath = (string)message["filepath"];
                        var readWrite = (bool)message["readwrite"];
                        using var hFile = Kernel32.CreateFile(filePath, Kernel32.FileAccess.GENERIC_READ | (readWrite ? Kernel32.FileAccess.GENERIC_WRITE : 0), FileShare.ReadWrite, null, FileMode.Open, FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL);
                        if (hFile.IsInvalid)
                        {
                            await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", false } }, message.Get("RequestID", (string)null));
                            return;
                        }
                        var processId = (int)(long)message["processid"];
                        using var uwpProces = System.Diagnostics.Process.GetProcessById(processId);
                        if (!Kernel32.DuplicateHandle(Kernel32.GetCurrentProcess(), hFile.DangerousGetHandle(), uwpProces.Handle, out var targetHandle, 0, false, Kernel32.DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS))
                        {
                            await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", false } }, message.Get("RequestID", (string)null));
                            return;
                        }
                        await Win32API.SendMessageAsync(connection, new ValueSet() {
                            { "Success", true },
                            { "Handle", targetHandle.ToInt64() }
                        }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "Clipboard":
                    await Win32API.StartSTATask(() =>
                    {
                        System.Windows.Forms.Clipboard.Clear();
                        var fileToCopy = (string)message["filepath"];
                        var operation = (DataPackageOperation)(long)message["operation"];
                        var fileList = new System.Collections.Specialized.StringCollection();
                        fileList.AddRange(fileToCopy.Split('|'));
                        MemoryStream dropEffect = new MemoryStream(operation == DataPackageOperation.Copy ?
                            new byte[] { 5, 0, 0, 0 } : new byte[] { 2, 0, 0, 0 });
                        var data = new System.Windows.Forms.DataObject();
                        data.SetFileDropList(fileList);
                        data.SetData("Preferred DropEffect", dropEffect);
                        System.Windows.Forms.Clipboard.SetDataObject(data, true);
                        return true;
                    });
                    break;

                case "DragDrop":
                    var dropPath = (string)message["droppath"];
                    var result = await Win32API.StartSTATask(() =>
                    {
                        var rdo = new RemoteDataObject(System.Windows.Forms.Clipboard.GetDataObject());

                        foreach (RemoteDataObject.DataPackage package in rdo.GetRemoteData())
                        {
                            try
                            {
                                if (package.ItemType == RemoteDataObject.StorageType.File)
                                {
                                    string directoryPath = Path.GetDirectoryName(dropPath);
                                    if (!Directory.Exists(directoryPath))
                                    {
                                        Directory.CreateDirectory(directoryPath);
                                    }

                                    string uniqueName = Win32API.GenerateUniquePath(Path.Combine(dropPath, package.Name));
                                    using FileStream stream = new FileStream(uniqueName, FileMode.CreateNew);
                                    package.ContentStream.CopyTo(stream);
                                }
                                else
                                {
                                    string directoryPath = Path.Combine(dropPath, package.Name);
                                    if (!Directory.Exists(directoryPath))
                                    {
                                        Directory.CreateDirectory(directoryPath);
                                    }
                                }
                            }
                            finally
                            {
                                package.Dispose();
                            }
                        }
                        return true;
                    });
                    await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", result } }, message.Get("RequestID", (string)null));
                    break;

                case "CreateFile":
                case "CreateFolder":
                    {
                        var filePath = (string)message["filepath"];
                        var template = message.Get("template", (string)null);
                        var dataStr = message.Get("data", (string)null);
                        var (success, shellOperationResult) = await Win32API.StartSTATask(async () =>
                        {
                            using var op = new ShellFileOperations();

                            op.Options = ShellFileOperations.OperationFlags.Silent
                                        | ShellFileOperations.OperationFlags.NoConfirmMkDir
                                        | ShellFileOperations.OperationFlags.RenameOnCollision
                                        | ShellFileOperations.OperationFlags.NoErrorUI;

                            var shellOperationResult = new ShellOperationResult();

                            if (!Extensions.IgnoreExceptions(() =>
                            {
                                using var shd = new ShellFolder(Path.GetDirectoryName(filePath));
                                op.QueueNewItemOperation(shd, Path.GetFileName(filePath),
                                    (string)message["fileop"] == "CreateFolder" ? FileAttributes.Directory : FileAttributes.Normal, template);
                            }))
                            {
                                shellOperationResult.Items.Add(new ShellOperationItemResult()
                                {
                                    Succeeded = false,
                                    Destination = filePath,
                                    HResult = (int)-1
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

                            if (dataStr != null && (shellOperationResult.Items.SingleOrDefault()?.Succeeded ?? false))
                            {
                                Extensions.IgnoreExceptions(() =>
                                {
                                    var dataBytes = Convert.FromBase64String(dataStr);
                                    using var fs = new FileStream(shellOperationResult.Items.Single().Destination, FileMode.Open);
                                    fs.Write(dataBytes, 0, dataBytes.Length);
                                    fs.Flush();
                                }, Program.Logger);
                            }

                            return (await createTcs.Task, shellOperationResult);
                        });
                        await Win32API.SendMessageAsync(connection, new ValueSet() {
                            { "Success", success },
                            { "Result", JsonConvert.SerializeObject(shellOperationResult) }
                        }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "TestRecycle":
                    {
                        var fileToDeletePath = ((string)message["filepath"]).Split('|');
                        var (success, shellOperationResult) = await Win32API.StartSTATask(async () =>
                        {
                            using var op = new ShellFileOperations();

                            op.Options = ShellFileOperations.OperationFlags.Silent
                                        | ShellFileOperations.OperationFlags.NoConfirmation
                                        | ShellFileOperations.OperationFlags.NoErrorUI;
                            op.Options |= ShellFileOperations.OperationFlags.RecycleOnDelete;

                            var shellOperationResult = new ShellOperationResult();

                            for (var i = 0; i < fileToDeletePath.Length; i++)
                            {
                                if (!Extensions.IgnoreExceptions(() =>
                                {
                                    using var shi = new ShellItem(fileToDeletePath[i]);
                                    var file = Extensions.IgnoreExceptions(() => GetFirstFile(shi)) ?? shi;
                                    op.QueueDeleteOperation(file);
                                }))
                                {
                                    shellOperationResult.Items.Add(new ShellOperationItemResult()
                                    {
                                        Succeeded = false,
                                        Source = fileToDeletePath[i],
                                        HResult = (int)-1
                                    });
                                }
                            }

                            var deleteTcs = new TaskCompletionSource<bool>();
                            op.PreDeleteItem += (s, e) =>
                            {
                                if (!e.Flags.HasFlag(ShellFileOperations.TransferFlags.DeleteRecycleIfPossible))
                                {
                                    shellOperationResult.Items.Add(new ShellOperationItemResult()
                                    {
                                        Succeeded = false,
                                        Source = e.SourceItem.GetParsingPath(),
                                        HResult = (int)HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND
                                    });
                                    throw new Win32Exception(HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND); // E_FAIL, stops operation
                                }
                                else
                                {
                                    shellOperationResult.Items.Add(new ShellOperationItemResult()
                                    {
                                        Succeeded = true,
                                        Source = e.SourceItem.GetParsingPath(),
                                        HResult = (int)HRESULT.COPYENGINE_E_USER_CANCELLED
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
                        await Win32API.SendMessageAsync(connection, new ValueSet() {
                            { "Success", success },
                            { "Result", JsonConvert.SerializeObject(shellOperationResult) }
                        }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "DeleteItem":
                    {
                        var fileToDeletePath = ((string)message["filepath"]).Split('|');
                        var permanently = (bool)message["permanently"];
                        var operationID = (string)message["operationID"];
                        var ownerHwnd = (long)message["HWND"];
                        var (success, shellOperationResult) = await Win32API.StartSTATask(async () =>
                        {
                            using var op = new ShellFileOperations();
                            op.Options = ShellFileOperations.OperationFlags.Silent
                                        | ShellFileOperations.OperationFlags.NoConfirmation
                                        | ShellFileOperations.OperationFlags.NoErrorUI;
                            op.OwnerWindow = Win32API.Win32Window.FromLong(ownerHwnd);
                            if (!permanently)
                            {
                                op.Options |= ShellFileOperations.OperationFlags.RecycleOnDelete
                                            | ShellFileOperations.OperationFlags.WantNukeWarning;
                            }

                            var shellOperationResult = new ShellOperationResult();

                            for (var i = 0; i < fileToDeletePath.Length; i++)
                            {
                                if (!Extensions.IgnoreExceptions(() =>
                                {
                                    using var shi = new ShellItem(fileToDeletePath[i]);
                                    op.QueueDeleteOperation(shi);
                                }))
                                {
                                    shellOperationResult.Items.Add(new ShellOperationItemResult()
                                    {
                                        Succeeded = false,
                                        Source = fileToDeletePath[i],
                                        HResult = (int)-1
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
                        await Win32API.SendMessageAsync(connection, new ValueSet() {
                            { "Success", success },
                            { "Result", JsonConvert.SerializeObject(shellOperationResult) }
                        }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "RenameItem":
                    {
                        var fileToRenamePath = (string)message["filepath"];
                        var newName = (string)message["newName"];
                        var operationID = (string)message["operationID"];
                        var overwriteOnRename = (bool)message["overwrite"];
                        var (success, shellOperationResult) = await Win32API.StartSTATask(async () =>
                        {
                            using var op = new ShellFileOperations();
                            var shellOperationResult = new ShellOperationResult();

                            op.Options = ShellFileOperations.OperationFlags.Silent
                                      | ShellFileOperations.OperationFlags.NoErrorUI;
                            op.Options |= !overwriteOnRename ? ShellFileOperations.OperationFlags.RenameOnCollision : 0;

                            if (!Extensions.IgnoreExceptions(() =>
                            {
                                using var shi = new ShellItem(fileToRenamePath);
                                op.QueueRenameOperation(shi, newName);
                            }))
                            {
                                shellOperationResult.Items.Add(new ShellOperationItemResult()
                                {
                                    Succeeded = false,
                                    Source = fileToRenamePath,
                                    HResult = (int)-1
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
                        await Win32API.SendMessageAsync(connection, new ValueSet() {
                            { "Success", success },
                            { "Result", JsonConvert.SerializeObject(shellOperationResult) },
                        }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "MoveItem":
                    {
                        var fileToMovePath = ((string)message["filepath"]).Split('|');
                        var moveDestination = ((string)message["destpath"]).Split('|');
                        var operationID = (string)message["operationID"];
                        var overwriteOnMove = (bool)message["overwrite"];
                        var ownerHwnd = (long)message["HWND"];
                        var (success, shellOperationResult) = await Win32API.StartSTATask(async () =>
                        {
                            using var op = new ShellFileOperations();
                            var shellOperationResult = new ShellOperationResult();

                            op.Options = ShellFileOperations.OperationFlags.NoConfirmMkDir
                                        | ShellFileOperations.OperationFlags.Silent
                                        | ShellFileOperations.OperationFlags.NoErrorUI;
                            op.OwnerWindow = Win32API.Win32Window.FromLong(ownerHwnd);
                            op.Options |= !overwriteOnMove ? ShellFileOperations.OperationFlags.PreserveFileExtensions | ShellFileOperations.OperationFlags.RenameOnCollision
                                : ShellFileOperations.OperationFlags.NoConfirmation;

                            for (var i = 0; i < fileToMovePath.Length; i++)
                            {
                                if (!Extensions.IgnoreExceptions(() =>
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
                                        HResult = (int)-1
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
                                    Destination = e.DestFolder.GetParsingPath() != null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(e.DestFolder.GetParsingPath(), e.Name) : null,
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
                        await Win32API.SendMessageAsync(connection, new ValueSet() {
                            { "Success", success },
                            { "Result", JsonConvert.SerializeObject(shellOperationResult) }
                        }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "CopyItem":
                    {
                        var fileToCopyPath = ((string)message["filepath"]).Split('|');
                        var copyDestination = ((string)message["destpath"]).Split('|');
                        var operationID = (string)message["operationID"];
                        var overwriteOnCopy = (bool)message["overwrite"];
                        var ownerHwnd = (long)message["HWND"];
                        var (success, shellOperationResult) = await Win32API.StartSTATask(async () =>
                        {
                            using var op = new ShellFileOperations();

                            var shellOperationResult = new ShellOperationResult();

                            op.Options = ShellFileOperations.OperationFlags.NoConfirmMkDir
                                        | ShellFileOperations.OperationFlags.Silent
                                        | ShellFileOperations.OperationFlags.NoErrorUI;
                            op.OwnerWindow = Win32API.Win32Window.FromLong(ownerHwnd);
                            op.Options |= !overwriteOnCopy ? ShellFileOperations.OperationFlags.PreserveFileExtensions | ShellFileOperations.OperationFlags.RenameOnCollision
                                : ShellFileOperations.OperationFlags.NoConfirmation;

                            for (var i = 0; i < fileToCopyPath.Length; i++)
                            {
                                if (!Extensions.IgnoreExceptions(() =>
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
                                        HResult = (int)-1
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
                                    Destination = e.DestFolder.GetParsingPath() != null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(e.DestFolder.GetParsingPath(), e.Name) : null,
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
                        await Win32API.SendMessageAsync(connection, new ValueSet() {
                            { "Success", success },
                            { "Result", JsonConvert.SerializeObject(shellOperationResult) }
                        }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "CancelOperation":
                    {
                        var operationID = (string)message["operationID"];
                        progressHandler.TryCancel(operationID);
                    }
                    break;

                case "ParseLink":
                    try
                    {
                        var linkPath = (string)message["filepath"];
                        if (linkPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                        {
                            using var link = new ShellLink(linkPath, LinkResolution.NoUIWithMsgPump, null, TimeSpan.FromMilliseconds(100));
                            await Win32API.SendMessageAsync(connection, new ValueSet()
                            {
                                { "ShortcutInfo", JsonConvert.SerializeObject(ShellFolderExtensions.GetShellLinkItem(link)) }
                            }, message.Get("RequestID", (string)null));
                        }
                        else if (linkPath.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
                        {
                            var linkUrl = await Win32API.StartSTATask(() =>
                            {
                                var ipf = new Url.IUniformResourceLocator();
                                (ipf as System.Runtime.InteropServices.ComTypes.IPersistFile).Load(linkPath, 0);
                                ipf.GetUrl(out var retVal);
                                return retVal;
                            });
                            await Win32API.SendMessageAsync(connection, new ValueSet()
                            {
                                { "ShortcutInfo", JsonConvert.SerializeObject(new ShellLinkItem() { TargetPath = linkUrl }) }
                            }, message.Get("RequestID", (string)null));
                        }
                    }
                    catch (Exception ex)
                    {
                        // Could not parse shortcut
                        Program.Logger.Warn(ex, ex.Message);
                        await Win32API.SendMessageAsync(connection, new ValueSet()
                        {
                            { "ShortcutInfo", JsonConvert.SerializeObject(new ShellLinkItem()) }
                        }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "CreateLink":
                case "UpdateLink":
                    try
                    {
                        var linkSavePath = (string)message["filepath"];
                        var targetPath = (string)message["targetpath"];

                        bool success = false;
                        if (linkSavePath.EndsWith(".lnk", StringComparison.Ordinal))
                        {
                            var arguments = (string)message["arguments"];
                            var workingDirectory = (string)message["workingdir"];
                            var runAsAdmin = (bool)message["runasadmin"];
                            using var newLink = new ShellLink(targetPath, arguments, workingDirectory);
                            newLink.RunAsAdministrator = runAsAdmin;
                            newLink.SaveAs(linkSavePath); // Overwrite if exists
                            success = true;
                        }
                        else if (linkSavePath.EndsWith(".url", StringComparison.Ordinal))
                        {
                            success = await Win32API.StartSTATask(() =>
                            {
                                var ipf = new Url.IUniformResourceLocator();
                                ipf.SetUrl(targetPath, Url.IURL_SETURL_FLAGS.IURL_SETURL_FL_GUESS_PROTOCOL);
                                (ipf as System.Runtime.InteropServices.ComTypes.IPersistFile).Save(linkSavePath, false); // Overwrite if exists
                                return true;
                            });
                        }
                        await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", success } }, message.Get("RequestID", (string)null));
                    }
                    catch (Exception ex)
                    {
                        // Could not create shortcut
                        Program.Logger.Warn(ex, ex.Message);
                        await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", false } }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "SetLinkIcon":
                    try
                    {
                        var linkPath = (string)message["filepath"];
                        using var link = new ShellLink(linkPath, LinkResolution.NoUIWithMsgPump, null, TimeSpan.FromMilliseconds(100));
                        link.IconLocation = new IconLocation((string)message["iconFile"], (int)message.Get("iconIndex", 0L));
                        link.SaveAs(linkPath); // Overwrite if exists
                        await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", true } }, message.Get("RequestID", (string)null));
                    }
                    catch (Exception ex)
                    {
                        // Could not create shortcut
                        Program.Logger.Warn(ex, ex.Message);
                        await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", false } }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "GetFilePermissions":
                    {
                        var filePathForPerm = (string)message["filepath"];
                        var isFolder = (bool)message["isfolder"];
                        var filePermissions = FilePermissions.FromFilePath(filePathForPerm, isFolder);
                        await Win32API.SendMessageAsync(connection, new ValueSet()
                        {
                            { "FilePermissions", JsonConvert.SerializeObject(filePermissions) }
                        }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "SetFilePermissions":
                    {
                        var filePermissionsString = (string)message["permissions"];
                        var filePermissionsToSet = JsonConvert.DeserializeObject<FilePermissions>(filePermissionsString);
                        await Win32API.SendMessageAsync(connection, new ValueSet()
                        {
                            { "Success", filePermissionsToSet.SetPermissions() }
                        }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "SetFileOwner":
                    {
                        var filePathForPerm = (string)message["filepath"];
                        var isFolder = (bool)message["isfolder"];
                        var ownerSid = (string)message["ownersid"];
                        var fp = FilePermissions.FromFilePath(filePathForPerm, isFolder);
                        await Win32API.SendMessageAsync(connection, new ValueSet()
                        {
                            { "Success", fp.SetOwner(ownerSid) }
                        }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "SetAccessRuleProtection":
                    {
                        var filePathForPerm = (string)message["filepath"];
                        var isFolder = (bool)message["isfolder"];
                        var isProtected = (bool)message["isprotected"];
                        var preserveInheritance = (bool)message["preserveinheritance"];
                        var fp = FilePermissions.FromFilePath(filePathForPerm, isFolder);
                        await Win32API.SendMessageAsync(connection, new ValueSet()
                        {
                            { "Success", fp.SetAccessRuleProtection(isProtected, preserveInheritance) }
                        }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "OpenObjectPicker":
                    var hwnd = (long)message["HWND"];
                    var pickedObject = await FilePermissions.OpenObjectPicker(hwnd);
                    await Win32API.SendMessageAsync(connection, new ValueSet()
                    {
                        { "PickedObject", pickedObject }
                    }, message.Get("RequestID", (string)null));
                    break;

                case "ReadCompatOptions":
                    {
                        var filePath = (string)message["filepath"];
                        var compatOptions = Extensions.IgnoreExceptions(() =>
                        {
                            using var compatKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers");
                            if (compatKey == null)
                            {
                                return null;
                            }
                            return (string)compatKey.GetValue(filePath, null);
                        }, Program.Logger);
                        await Win32API.SendMessageAsync(connection, new ValueSet()
                        {
                            { "CompatOptions", compatOptions }
                        }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "SetCompatOptions":
                    {
                        var filePath = (string)message["filepath"];
                        var compatOptions = (string)message["options"];
                        bool success = false;
                        if (string.IsNullOrEmpty(compatOptions) || compatOptions == "~")
                        {
                            success = Win32API.RunPowershellCommand(@$"Remove-ItemProperty -Path 'HKCU:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers' -Name '{filePath}' | Out-Null", false);
                        }
                        else
                        {
                            success = Win32API.RunPowershellCommand(@$"New-ItemProperty -Path 'HKCU:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers' -Name '{filePath}' -Value '{compatOptions}' -PropertyType String -Force | Out-Null", false);
                        }
                        await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", success } }, message.Get("RequestID", (string)null));
                    }
                    break;
            }
        }

        private ShellItem GetFirstFile(ShellItem shi)
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

        public void WaitForCompletion()
        {
            progressHandler.WaitForCompletion();
        }

        private void UpdateFileTagsDb(ShellFileOperations.ShellFileOpEventArgs e, string operationType)
        {
            if (e.Result.Succeeded)
            {
                var sourcePath = e.SourceItem.GetParsingPath();
                var destPath = e.DestFolder.GetParsingPath();
                var destination = operationType switch
                {
                    "delete" => e.DestItem.GetParsingPath(),
                    "rename" => !string.IsNullOrEmpty(e.Name) ? Path.Combine(Path.GetDirectoryName(sourcePath), e.Name) : null,
                    "copy" => destPath != null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(destPath, e.Name) : null,
                    _ => destPath != null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(destPath, e.Name) : null
                };
                if (destination == null)
                {
                    dbInstance.SetTag(sourcePath, null, null); // remove tag from deleted files
                }
                else
                {
                    Extensions.IgnoreExceptions(() =>
                    {
                        if (operationType == "copy")
                        {
                            var tag = dbInstance.GetTag(sourcePath);
                            dbInstance.SetTag(destination, FileTagsHandler.GetFileFRN(destination), tag); // copy tag to new files
                            using var si = new ShellItem(destination);
                            if (si.IsFolder) // File tag is not copied automatically for folders
                            {
                                FileTagsHandler.WriteFileTag(destination, tag);
                            }
                        }
                        else
                        {
                            dbInstance.UpdateTag(sourcePath, FileTagsHandler.GetFileFRN(destination), destination); // move tag to new files
                        }
                    }, Program.Logger);
                }
                if (e.Result == HRESULT.COPYENGINE_S_DONT_PROCESS_CHILDREN) // child items not processed, update manually
                {
                    var tags = dbInstance.GetAllUnderPath(sourcePath).ToList();
                    if (destination == null) // remove tag for items contained in the folder
                    {
                        tags.ForEach(t => dbInstance.SetTag(t.FilePath, null, null));
                    }
                    else
                    {
                        if (operationType == "copy") // copy tag for items contained in the folder
                        {
                            tags.ForEach(t =>
                            {
                                Extensions.IgnoreExceptions(() =>
                                {
                                    var subPath = t.FilePath.Replace(sourcePath, destination, StringComparison.Ordinal);
                                    dbInstance.SetTag(subPath, FileTagsHandler.GetFileFRN(subPath), t.Tag);
                                }, Program.Logger);
                            });
                        }
                        else // move tag to new files
                        {
                            tags.ForEach(t =>
                            {
                                Extensions.IgnoreExceptions(() =>
                                {
                                    var subPath = t.FilePath.Replace(sourcePath, destination, StringComparison.Ordinal);
                                    dbInstance.UpdateTag(t.FilePath, FileTagsHandler.GetFileFRN(subPath), subPath);
                                }, Program.Logger);
                            });
                        }
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                progressHandler?.Dispose();
                dbInstance?.Dispose();
            }
        }

        private class ProgressHandler : IDisposable
        {
            private readonly ManualResetEvent operationsCompletedEvent;
            private readonly PipeStream connection;

            private class OperationWithProgress
            {
                public int Progress { get; set; }
                public bool Canceled { get; set; }
            }

            private readonly Shell32.ITaskbarList4 taskbar;
            private readonly ConcurrentDictionary<string, OperationWithProgress> operations;

            public System.Windows.Forms.IWin32Window OwnerWindow { get; set; }

            public ProgressHandler(PipeStream conn)
            {
                taskbar = Win32API.CreateTaskbarObject();
                operations = new ConcurrentDictionary<string, OperationWithProgress>();
                operationsCompletedEvent = new ManualResetEvent(true);
                connection = conn;
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

            public async void UpdateOperation(string uid, int progress)
            {
                if (operations.TryGetValue(uid, out var op))
                {
                    op.Progress = progress;
                    await Win32API.SendMessageAsync(connection, new ValueSet() {
                        { "Progress", progress },
                        { "OperationID", uid }
                    });
                    UpdateTaskbarProgress();
                }
            }

            public bool CheckCanceled(string uid)
            {
                if (operations.TryGetValue(uid, out var op))
                {
                    return op.Canceled;
                }
                return true;
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
                if (OwnerWindow == null || taskbar == null)
                {
                    return;
                }
                if (operations.Any())
                {
                    taskbar.SetProgressValue(OwnerWindow.Handle, (ulong)Progress, 100);
                }
                else
                {
                    taskbar.SetProgressState(OwnerWindow.Handle, Shell32.TBPFLAG.TBPF_NOPROGRESS);
                }
            }

            public void WaitForCompletion()
            {
                operationsCompletedEvent.WaitOne();
            }

            public void Dispose()
            {
                operationsCompletedEvent?.Dispose();
            }
        }
    }
}
