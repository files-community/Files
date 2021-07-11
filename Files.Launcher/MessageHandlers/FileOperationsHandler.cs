using Files.Common;
using FilesFullTrust.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;

namespace FilesFullTrust.MessageHandlers
{
    public class FileOperationsHandler : IMessageHandler
    {
        private DisposableDictionary handleTable;

        public FileOperationsHandler()
        {
            // Create handle table to store context menu references
            handleTable = new DisposableDictionary();
        }

        public void Initialize(NamedPipeServerStream connection)
        {
        }

        public async Task ParseArgumentsAsync(NamedPipeServerStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "FileOperation":
                    await ParseFileOperationAsync(connection, message);
                    break;
            }
        }

        private async Task ParseFileOperationAsync(NamedPipeServerStream connection, Dictionary<string, object> message)
        {
            switch (message.Get("fileop", ""))
            {
                case "Clipboard":
                    await Win32API.StartSTATask(() =>
                    {
                        System.Windows.Forms.Clipboard.Clear();
                        var fileToCopy = (string)message["filepath"];
                        var operation = (DataPackageOperation)(long)message["operation"];
                        var fileList = new System.Collections.Specialized.StringCollection();
                        fileList.AddRange(fileToCopy.Split('|'));
                        if (operation == DataPackageOperation.Copy)
                        {
                            System.Windows.Forms.Clipboard.SetFileDropList(fileList);
                        }
                        else if (operation == DataPackageOperation.Move)
                        {
                            byte[] moveEffect = new byte[] { 2, 0, 0, 0 };
                            MemoryStream dropEffect = new MemoryStream();
                            dropEffect.Write(moveEffect, 0, moveEffect.Length);
                            var data = new System.Windows.Forms.DataObject();
                            data.SetFileDropList(fileList);
                            data.SetData("Preferred DropEffect", dropEffect);
                            System.Windows.Forms.Clipboard.SetDataObject(data, true);
                        }
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
                                    using (FileStream stream = new FileStream(uniqueName, FileMode.CreateNew))
                                    {
                                        package.ContentStream.CopyTo(stream);
                                    }
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

                case "DeleteItem":
                    {
                        var fileToDeletePath = ((string)message["filepath"]).Split('|');
                        var permanently = (bool)message["permanently"];
                        var operationID = (string)message["operationID"];
                        var (succcess, deletedItems, recycledItems) = await Win32API.StartSTATask(async () =>
                        {
                            using (var op = new ShellFileOperations())
                            {
                                op.Options = ShellFileOperations.OperationFlags.Silent
                                            | ShellFileOperations.OperationFlags.NoConfirmation
                                            | ShellFileOperations.OperationFlags.NoErrorUI
                                            | ShellFileOperations.OperationFlags.EarlyFailure;
                                if (!permanently)
                                {
                                    op.Options |= ShellFileOperations.OperationFlags.RecycleOnDelete
                                                | ShellFileOperations.OperationFlags.WantNukeWarning;
                                }
                                List<string> deletedItems = new List<string>();
                                List<string> recycledItems = new List<string>();

                                for (var i = 0; i < fileToDeletePath.Length; i++)
                                {
                                    using var shi = new ShellItem(fileToDeletePath[i]);
                                    op.QueueDeleteOperation(shi);
                                }

                                handleTable.SetValue(operationID, false);
                                var deleteTcs = new TaskCompletionSource<bool>();
                                op.PostDeleteItem += (s, e) =>
                                {
                                    if (e.Result.Succeeded)
                                    {
                                        if (!fileToDeletePath.Any(x => x == e.SourceItem.FileSystemPath))
                                        {
                                            return;
                                        }
                                        deletedItems.Add(e.SourceItem.FileSystemPath);
                                        if (e.DestItem != null)
                                        {
                                            recycledItems.Add(e.DestItem.FileSystemPath);
                                        }
                                    }
                                };
                                op.FinishOperations += (s, e) => deleteTcs.TrySetResult(e.Result.Succeeded);
                                op.UpdateProgress += async (s, e) => await Win32API.SendMessageAsync(connection, new ValueSet() {
                                    { "Progress", e.ProgressPercentage },
                                    { "OperationID", operationID }
                                });
                                op.UpdateProgress += (s, e) =>
                                {
                                    if (handleTable.GetValue<bool>(operationID))
                                    {
                                        throw new Win32Exception(unchecked((int)0x80004005)); // E_FAIL, stops operation
                                    }
                                };

                                try
                                {
                                    op.PerformOperations();
                                }
                                catch
                                {
                                    deleteTcs.TrySetResult(false);
                                }

                                handleTable.RemoveValue(operationID);

                                return (await deleteTcs.Task && deletedItems.Count == fileToDeletePath.Length, deletedItems, recycledItems);
                            }
                        });
                        await Win32API.SendMessageAsync(connection, new ValueSet() {
                            { "Success", succcess },
                            { "DeletedItems", JsonConvert.SerializeObject(deletedItems) },
                            { "RecycledItems", JsonConvert.SerializeObject(recycledItems) }
                        }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "RenameItem":
                    {
                        var fileToRenamePath = (string)message["filepath"];
                        var newName = (string)message["newName"];
                        var operationID = (string)message["operationID"];
                        var overwriteOnRename = (bool)message["overwrite"];
                        var (succcess, renamedItems) = await Win32API.StartSTATask(async () =>
                        {
                            using (var op = new ShellFileOperations())
                            {
                                List<string> renamedItems = new List<string>();

                                op.Options = ShellFileOperations.OperationFlags.Silent
                                          | ShellFileOperations.OperationFlags.NoErrorUI
                                          | ShellFileOperations.OperationFlags.EarlyFailure;
                                op.Options |= !overwriteOnRename ? ShellFileOperations.OperationFlags.RenameOnCollision : 0;

                                using var shi = new ShellItem(fileToRenamePath);
                                op.QueueRenameOperation(shi, newName);

                                handleTable.SetValue(operationID, false);
                                var renameTcs = new TaskCompletionSource<bool>();
                                op.PostRenameItem += (s, e) =>
                                {
                                    if (e.Result.Succeeded)
                                    {
                                        renamedItems.Add($"{Path.Combine(Path.GetDirectoryName(e.SourceItem.FileSystemPath), e.Name)}");
                                    }
                                };
                                op.FinishOperations += (s, e) => renameTcs.TrySetResult(e.Result.Succeeded);

                                try
                                {
                                    op.PerformOperations();
                                }
                                catch
                                {
                                    renameTcs.TrySetResult(false);
                                }

                                handleTable.RemoveValue(operationID);

                                return (await renameTcs.Task && renamedItems.Count == 1, renamedItems);
                            }
                        });
                        await Win32API.SendMessageAsync(connection, new ValueSet() {
                            { "Success", succcess },
                            { "RenamedItems", JsonConvert.SerializeObject(renamedItems) },
                        }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "MoveItem":
                    {
                        var fileToMovePath = ((string)message["filepath"]).Split('|');
                        var moveDestination = ((string)message["destpath"]).Split('|');
                        var operationID = (string)message["operationID"];
                        var overwriteOnMove = (bool)message["overwrite"];
                        var (succcess, movedItems, movedSources) = await Win32API.StartSTATask(async () =>
                        {
                            using (var op = new ShellFileOperations())
                            {
                                List<string> movedItems = new List<string>();
                                List<string> movedSources = new List<string>();

                                op.Options = ShellFileOperations.OperationFlags.NoConfirmMkDir
                                            | ShellFileOperations.OperationFlags.Silent
                                            | ShellFileOperations.OperationFlags.NoErrorUI
                                            | ShellFileOperations.OperationFlags.EarlyFailure;

                                op.Options |= !overwriteOnMove ? ShellFileOperations.OperationFlags.PreserveFileExtensions | ShellFileOperations.OperationFlags.RenameOnCollision
                                    : ShellFileOperations.OperationFlags.NoConfirmation;

                                for (var i = 0; i < fileToMovePath.Length; i++)
                                {
                                    using (ShellItem shi = new ShellItem(fileToMovePath[i]))
                                    using (ShellFolder shd = new ShellFolder(Path.GetDirectoryName(moveDestination[i])))
                                    {
                                        op.QueueMoveOperation(shi, shd, Path.GetFileName(moveDestination[i]));
                                    }
                                }

                                handleTable.SetValue(operationID, false);
                                var moveTcs = new TaskCompletionSource<bool>();
                                op.PostMoveItem += (s, e) =>
                                {
                                    if (e.Result.Succeeded)
                                    {
                                        if (!fileToMovePath.Any(x => x == e.SourceItem.FileSystemPath))
                                        {
                                            return;
                                        }
                                        if (e.DestFolder != null && !string.IsNullOrEmpty(e.Name))
                                        {
                                            movedItems.Add($"{Path.Combine(e.DestFolder.FileSystemPath, e.Name)}");
                                            movedSources.Add(e.SourceItem.FileSystemPath);
                                        }
                                    }
                                };
                                op.FinishOperations += (s, e) => moveTcs.TrySetResult(e.Result.Succeeded);
                                op.UpdateProgress += async (s, e) => await Win32API.SendMessageAsync(connection, new ValueSet() {
                                    { "Progress", e.ProgressPercentage },
                                    { "OperationID", operationID }
                                });
                                op.UpdateProgress += (s, e) =>
                                {
                                    if (handleTable.GetValue<bool>(operationID))
                                    {
                                        throw new Win32Exception(unchecked((int)0x80004005)); // E_FAIL, stops operation
                                    }
                                };

                                try
                                {
                                    op.PerformOperations();
                                }
                                catch
                                {
                                    moveTcs.TrySetResult(false);
                                }

                                handleTable.RemoveValue(operationID);

                                return (await moveTcs.Task && movedItems.Count == fileToMovePath.Length, movedItems, movedSources);
                            }
                        });
                        await Win32API.SendMessageAsync(connection, new ValueSet() {
                            { "Success", succcess },
                            { "MovedItems", JsonConvert.SerializeObject(movedItems) },
                            { "MovedSources", JsonConvert.SerializeObject(movedSources) },
                        }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "CopyItem":
                    {
                        var fileToCopyPath = ((string)message["filepath"]).Split('|');
                        var copyDestination = ((string)message["destpath"]).Split('|');
                        var operationID = (string)message["operationID"];
                        var overwriteOnCopy = (bool)message["overwrite"];
                        var (succcess, copiedItems, copiedSources) = await Win32API.StartSTATask(async () =>
                        {
                            using (var op = new ShellFileOperations())
                            {
                                List<string> copiedItems = new List<string>();
                                List<string> copiedSources = new List<string>();

                                op.Options = ShellFileOperations.OperationFlags.NoConfirmMkDir
                                            | ShellFileOperations.OperationFlags.Silent
                                            | ShellFileOperations.OperationFlags.NoErrorUI
                                            | ShellFileOperations.OperationFlags.EarlyFailure;

                                op.Options |= !overwriteOnCopy ? ShellFileOperations.OperationFlags.PreserveFileExtensions | ShellFileOperations.OperationFlags.RenameOnCollision
                                    : ShellFileOperations.OperationFlags.NoConfirmation;

                                for (var i = 0; i < fileToCopyPath.Length; i++)
                                {
                                    using (ShellItem shi = new ShellItem(fileToCopyPath[i]))
                                    using (ShellFolder shd = new ShellFolder(Path.GetDirectoryName(copyDestination[i])))
                                    {
                                        op.QueueCopyOperation(shi, shd, Path.GetFileName(copyDestination[i]));
                                    }
                                }

                                handleTable.SetValue(operationID, false);
                                var copyTcs = new TaskCompletionSource<bool>();
                                op.PostCopyItem += (s, e) =>
                                {
                                    if (e.Result.Succeeded)
                                    {
                                        if (!fileToCopyPath.Any(x => x == e.SourceItem.FileSystemPath))
                                        {
                                            return;
                                        }
                                        if (e.DestFolder != null && !string.IsNullOrEmpty(e.Name))
                                        {
                                            copiedItems.Add($"{Path.Combine(e.DestFolder.FileSystemPath, e.Name)}");
                                            copiedSources.Add(e.SourceItem.FileSystemPath);
                                        }
                                    }
                                };
                                op.FinishOperations += (s, e) => copyTcs.TrySetResult(e.Result.Succeeded);
                                op.UpdateProgress += async (s, e) => await Win32API.SendMessageAsync(connection, new ValueSet() {
                                    { "Progress", e.ProgressPercentage },
                                    { "OperationID", operationID }
                                });
                                op.UpdateProgress += (s, e) =>
                                {
                                    if (handleTable.GetValue<bool>(operationID))
                                    {
                                        throw new Win32Exception(unchecked((int)0x80004005)); // E_FAIL, stops operation
                                    }
                                };

                                try
                                {
                                    op.PerformOperations();
                                }
                                catch
                                {
                                    copyTcs.TrySetResult(false);
                                }

                                handleTable.RemoveValue(operationID);

                                return (await copyTcs.Task && copiedItems.Count == fileToCopyPath.Length, copiedItems, copiedSources);
                            }
                        });
                        await Win32API.SendMessageAsync(connection, new ValueSet() {
                            { "Success", succcess },
                            { "CopiedItems", JsonConvert.SerializeObject(copiedItems) },
                            { "CopiedSources", JsonConvert.SerializeObject(copiedSources) },
                        }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "CancelOperation":
                    {
                        var operationID = (string)message["operationID"];
                        handleTable.SetValue(operationID, true);
                    }
                    break;

                case "ParseLink":
                    var linkPath = (string)message["filepath"];
                    try
                    {
                        if (linkPath.EndsWith(".lnk"))
                        {
                            using var link = new ShellLink(linkPath, LinkResolution.NoUIWithMsgPump, null, TimeSpan.FromMilliseconds(100));
                            await Win32API.SendMessageAsync(connection, new ValueSet()
                            {
                                { "TargetPath", link.TargetPath },
                                { "Arguments", link.Arguments },
                                { "WorkingDirectory", link.WorkingDirectory },
                                { "RunAsAdmin", link.RunAsAdministrator },
                                { "IsFolder", !string.IsNullOrEmpty(link.TargetPath) && link.Target.IsFolder }
                            }, message.Get("RequestID", (string)null));
                        }
                        else if (linkPath.EndsWith(".url"))
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
                                { "TargetPath", linkUrl },
                                { "Arguments", null },
                                { "WorkingDirectory", null },
                                { "RunAsAdmin", false },
                                { "IsFolder", false }
                            }, message.Get("RequestID", (string)null));
                        }
                    }
                    catch (Exception ex)
                    {
                        // Could not parse shortcut
                        Program.Logger.Warn(ex, ex.Message);
                        await Win32API.SendMessageAsync(connection, new ValueSet()
                            {
                                { "TargetPath", null },
                                { "Arguments", null },
                                { "WorkingDirectory", null },
                                { "RunAsAdmin", false },
                                { "IsFolder", false }
                            }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "CreateLink":
                case "UpdateLink":
                    var linkSavePath = (string)message["filepath"];
                    var targetPath = (string)message["targetpath"];
                    if (linkSavePath.EndsWith(".lnk"))
                    {
                        var arguments = (string)message["arguments"];
                        var workingDirectory = (string)message["workingdir"];
                        var runAsAdmin = (bool)message["runasadmin"];
                        using var newLink = new ShellLink(targetPath, arguments, workingDirectory);
                        newLink.RunAsAdministrator = runAsAdmin;
                        newLink.SaveAs(linkSavePath); // Overwrite if exists
                    }
                    else if (linkSavePath.EndsWith(".url"))
                    {
                        await Win32API.StartSTATask(() =>
                        {
                            var ipf = new Url.IUniformResourceLocator();
                            ipf.SetUrl(targetPath, Url.IURL_SETURL_FLAGS.IURL_SETURL_FL_GUESS_PROTOCOL);
                            (ipf as System.Runtime.InteropServices.ComTypes.IPersistFile).Save(linkSavePath, false); // Overwrite if exists
                            return true;
                        });
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
            }
        }

        public void Dispose()
        {
            handleTable?.Dispose();
        }
    }
}
