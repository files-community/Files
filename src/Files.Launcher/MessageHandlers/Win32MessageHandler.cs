using Files.Common;
using FilesFullTrust.Helpers;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using Vanara.Windows.Shell;
using Windows.ApplicationModel;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace FilesFullTrust.MessageHandlers
{
    public class Win32MessageHandler : IMessageHandler
    {
        public void Initialize(PipeStream connection)
        {
            DetectIsSetAsDefaultFileManager();
            DetectIsSetAsOpenFileDialog();
            ApplicationData.Current.LocalSettings.Values["TEMP"] = Environment.GetEnvironmentVariable("TEMP");
        }

        private void DetectIsSetAsDefaultFileManager()
        {
            using var subkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\Directory\shell");
            ApplicationData.Current.LocalSettings.Values["IsSetAsDefaultFileManager"] = subkey?.GetValue(string.Empty) as string == "openinfiles";
        }

        private void DetectIsSetAsOpenFileDialog()
        {
            using var subkeyOpen = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\CLSID\{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}");
            using var subkeySave = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\CLSID\{C0B4E2F3-BA21-4773-8DBA-335EC946EB8B}");
            var isSetAsOpenDialog = subkeyOpen?.GetValue(string.Empty) as string == "FilesOpenDialog class";
            var isSetAsSaveDialog = subkeySave?.GetValue(string.Empty) as string == "FilesSaveDialog class";
            ApplicationData.Current.LocalSettings.Values["IsSetAsOpenFileDialog"] = isSetAsOpenDialog || isSetAsSaveDialog;
        }

        public async Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "Bitlocker":
                    var bitlockerAction = (string)message["action"];
                    if (bitlockerAction == "Unlock")
                    {
                        var drive = (string)message["drive"];
                        var password = (string)message["password"];
                        Win32API.UnlockBitlockerDrive(drive, password);
                        await Win32API.SendMessageAsync(connection, new ValueSet() { { "Bitlocker", "Unlock" } }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "SetVolumeLabel":
                    var driveName = (string)message["drivename"];
                    var newLabel = (string)message["newlabel"];
                    Win32API.SetVolumeLabel(driveName, newLabel);
                    await Win32API.SendMessageAsync(connection, new ValueSet() { { "SetVolumeLabel", driveName } }, message.Get("RequestID", (string)null));
                    break;

                case "GetIconOverlay":
                    var fileIconPath = (string)message["filePath"];
                    var thumbnailSize = (int)(long)message["thumbnailSize"];
                    var isOverlayOnly = (bool)message["isOverlayOnly"];
                    var iconOverlay = await Win32API.StartSTATask(() => Win32API.GetFileIconAndOverlay(fileIconPath, thumbnailSize, true, isOverlayOnly));
                    await Win32API.SendMessageAsync(connection, new ValueSet()
                    {
                        { "Icon", iconOverlay.icon },
                        { "Overlay", iconOverlay.overlay }
                    }, message.Get("RequestID", (string)null));
                    break;

                case "GetIconWithoutOverlay":
                    var fileIconPath2 = (string)message["filePath"];
                    var thumbnailSize2 = (int)(long)message["thumbnailSize"];
                    var icon2 = await Win32API.StartSTATask(() => Win32API.GetFileIconAndOverlay(fileIconPath2, thumbnailSize2, false));
                    await Win32API.SendMessageAsync(connection, new ValueSet()
                    {
                        { "Icon", icon2.icon },
                    }, message.Get("RequestID", (string)null));
                    break;

                case "ShellFolder":
                    // Enumerate shell folder contents and send response to UWP
                    var folderPath = (string)message["folder"];
                    var responseEnum = new ValueSet();
                    var folderContentsList = await Win32API.StartSTATask(() =>
                    {
                        var flc = new List<ShellFileItem>();
                        try
                        {
                            using (var shellFolder = new ShellFolder(folderPath))
                            {
                                foreach (var folderItem in shellFolder)
                                {
                                    try
                                    {
                                        var shellFileItem = ShellFolderExtensions.GetShellFileItem(folderItem);
                                        flc.Add(shellFileItem);
                                    }
                                    catch (FileNotFoundException)
                                    {
                                        // Happens if files are being deleted
                                    }
                                    finally
                                    {
                                        folderItem.Dispose();
                                    }
                                }
                            }
                        }
                        catch
                        {
                        }
                        return flc;
                    });
                    responseEnum.Add("Enumerate", JsonConvert.SerializeObject(folderContentsList));
                    await Win32API.SendMessageAsync(connection, responseEnum, message.Get("RequestID", (string)null));
                    break;

                case "GetFolderIconsFromDLL":
                    var iconInfos = Win32API.ExtractIconsFromDLL((string)message["iconFile"]);
                    await Win32API.SendMessageAsync(connection, new ValueSet()
                    {
                        { "IconInfos", JsonConvert.SerializeObject(iconInfos) },
                    }, message.Get("RequestID", (string)null));
                    break;

                case "SetCustomFolderIcon":
                    await Win32API.SendMessageAsync(connection, new ValueSet()
                    {
                        { "Success", Win32API.SetCustomDirectoryIcon((string)message["folder"], (string)message["iconFile"], (int)message.Get("iconIndex", 0L)) },
                    }, message.Get("RequestID", (string)null));
                    break;

                case "GetSelectedIconsFromDLL":
                    var selectedIconInfos = Win32API.ExtractSelectedIconsFromDLL((string)message["iconFile"], JsonConvert.DeserializeObject<List<int>>((string)message["iconIndexes"]), Convert.ToInt32(message["requestedIconSize"]));
                    await Win32API.SendMessageAsync(connection, new ValueSet()
                    {
                        { "IconInfos", JsonConvert.SerializeObject(selectedIconInfos) },
                    }, message.Get("RequestID", (string)null));
                    break;

                case "SetAsDefaultExplorer":
                    {
                        var enable = (bool)message["Value"];
                        var destFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "FilesOpenDialog");
                        Directory.CreateDirectory(destFolder);
                        foreach (var file in Directory.GetFiles(Path.Combine(Package.Current.InstalledLocation.Path, "Files.Launcher", "Assets", "FilesOpenDialog")))
                        {
                            if (!Extensions.IgnoreExceptions(() => File.Copy(file, Path.Combine(destFolder, Path.GetFileName(file)), true), Program.Logger))
                            {
                                // Error copying files
                                DetectIsSetAsDefaultFileManager();
                                await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", false } }, message.Get("RequestID", (string)null));
                                return;
                            }
                        }

                        try
                        {
                            using var regProcess = Process.Start(new ProcessStartInfo("regedit.exe", @$"/s ""{Path.Combine(destFolder, enable ? "SetFilesAsDefault.reg" : "UnsetFilesAsDefault.reg")}""") { UseShellExecute = true, Verb = "runas" });
                            regProcess.WaitForExit();
                            DetectIsSetAsDefaultFileManager();
                            await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", true } }, message.Get("RequestID", (string)null));
                        }
                        catch
                        {
                            // Canceled UAC
                            DetectIsSetAsDefaultFileManager();
                            await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", false } }, message.Get("RequestID", (string)null));
                        }
                    }
                    break;

                case "SetAsOpenFileDialog":
                    {
                        var enable = (bool)message["Value"];
                        var destFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "FilesOpenDialog");
                        Directory.CreateDirectory(destFolder);
                        foreach (var file in Directory.GetFiles(Path.Combine(Package.Current.InstalledLocation.Path, "Files.Launcher", "Assets", "FilesOpenDialog")))
                        {
                            if (!Extensions.IgnoreExceptions(() => File.Copy(file, Path.Combine(destFolder, Path.GetFileName(file)), true), Program.Logger))
                            {
                                // Error copying files
                                DetectIsSetAsOpenFileDialog();
                                await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", false } }, message.Get("RequestID", (string)null));
                                return;
                            }
                        }

                        try
                        {
                            using (var regProc = Process.Start("regsvr32.exe", @$"/s /n {(!enable ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "CustomOpenDialog32.dll")}"""))
                                regProc.WaitForExit();
                            using (var regProc = Process.Start("regsvr32.exe", @$"/s /n {(!enable ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "CustomOpenDialog64.dll")}"""))
                                regProc.WaitForExit();
                            using (var regProc = Process.Start("regsvr32.exe", @$"/s /n {(!enable ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "CustomOpenDialogARM64.dll")}"""))
                                regProc.WaitForExit();
                            using (var regProc = Process.Start("regsvr32.exe", @$"/s /n {(!enable ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "CustomSaveDialog32.dll")}"""))
                                regProc.WaitForExit();
                            using (var regProc = Process.Start("regsvr32.exe", @$"/s /n {(!enable ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "CustomSaveDialog64.dll")}"""))
                                regProc.WaitForExit();
                            using (var regProc = Process.Start("regsvr32.exe", @$"/s /n {(!enable ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "CustomSaveDialogARM64.dll")}"""))
                                regProc.WaitForExit();

                            DetectIsSetAsOpenFileDialog();
                            await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", true } }, message.Get("RequestID", (string)null));
                        }
                        catch
                        {
                            DetectIsSetAsOpenFileDialog();
                            await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", false } }, message.Get("RequestID", (string)null));
                        }
                    }
                    break;

                case "GetFileAssociation":
                    {
                        var filePath = (string)message["filepath"];
                        await Win32API.SendMessageAsync(connection, new ValueSet() { { "FileAssociation", await Win32API.GetFileAssociationAsync(filePath, true) } }, message.Get("RequestID", (string)null));
                    }
                    break;
            }
        }

        public void Dispose()
        {
        }
    }
}
