using Files.Common;
using FilesFullTrust.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Foundation.Collections;

namespace FilesFullTrust.MessageHandlers
{
    [SupportedOSPlatform("Windows10.0.10240")]
    public class ContextMenuHandler : IMessageHandler
    {
        private readonly DisposableDictionary handleTable;

        public ContextMenuHandler()
        {
            // Create handle table to store context menu references
            handleTable = new DisposableDictionary();
        }

        public void Initialize(PipeStream connection)
        {
        }

        public async Task ParseArgumentsAsync(PipeStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "LoadContextMenu":
                    var contextMenuResponse = new ValueSet();
                    var loadThreadWithMessageQueue = new ThreadWithMessageQueue<Dictionary<string, object>>(HandleMenuMessage);
                    var cMenuLoad = await loadThreadWithMessageQueue.PostMessageAsync<ContextMenu>(message);
                    contextMenuResponse.Add("Handle", handleTable.AddValue(loadThreadWithMessageQueue));
                    contextMenuResponse.Add("ContextMenu", JsonConvert.SerializeObject(cMenuLoad));
                    await Win32API.SendMessageAsync(connection, contextMenuResponse, message.Get("RequestID", (string)null));
                    break;

                case "ExecAndCloseContextMenu":
                    var menuKey = (string)message["Handle"];
                    var execThreadWithMessageQueue = handleTable.GetValue<ThreadWithMessageQueue<Dictionary<string, object>>>(menuKey);
                    if (execThreadWithMessageQueue != null)
                    {
                        await execThreadWithMessageQueue.PostMessage(message);
                    }
                    // The following line is needed to cleanup resources when menu is closed.
                    // Unfortunately if you uncomment it some menu items will randomly stop working.
                    // Resource cleanup is currently done on app closing,
                    // if we find a solution for the issue above, we should cleanup as soon as a menu is closed.
                    //handleTable.RemoveValue(menuKey);
                    break;

                case "InvokeVerb":
                    var filePath = (string)message["FilePath"];
                    var split = filePath.Split('|').Where(x => !string.IsNullOrWhiteSpace(x));
                    var verb = (string)message["Verb"];
                    using (var cMenu = ContextMenu.GetContextMenuForFiles(split.ToArray(), Shell32.CMF.CMF_DEFAULTONLY))
                    {
                        var result = cMenu?.InvokeVerb(verb);
                        await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", result } }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "GetNewContextMenuEntries":
                    var entries = await Extensions.IgnoreExceptions(() => ShellNewMenuHelper.GetNewContextMenuEntries(), Program.Logger);
                    await Win32API.SendMessageAsync(connection, new ValueSet() { { "Entries", JsonConvert.SerializeObject(entries) } }, message.Get("RequestID", (string)null));
                    break;

                case "GetNewContextMenuEntryForType":
                    var fileExtension = (string)message["extension"];
                    var entry = await Extensions.IgnoreExceptions(() => ShellNewMenuHelper.GetNewContextMenuEntryForType(fileExtension), Program.Logger);
                    await Win32API.SendMessageAsync(connection, new ValueSet() { { "Entry", JsonConvert.SerializeObject(entry) } }, message.Get("RequestID", (string)null));
                    break;
            }
        }

        private object HandleMenuMessage(Dictionary<string, object> message, DisposableDictionary table)
        {
            switch (message.Get("Arguments", ""))
            {
                case "LoadContextMenu":
                    var contextMenuResponse = new ValueSet();
                    var filePath = (string)message["FilePath"];
                    var extendedMenu = (bool)message["ExtendedMenu"];
                    var showOpenMenu = (bool)message["ShowOpenMenu"];
                    var split = filePath.Split('|').Where(x => !string.IsNullOrWhiteSpace(x));
                    var cMenuLoad = ContextMenu.GetContextMenuForFiles(split.ToArray(),
                        (extendedMenu ? Shell32.CMF.CMF_EXTENDEDVERBS : Shell32.CMF.CMF_NORMAL) | Shell32.CMF.CMF_SYNCCASCADEMENU, FilterMenuItems(showOpenMenu));
                    table.SetValue("MENU", cMenuLoad);
                    return cMenuLoad;

                case "ExecAndCloseContextMenu":
                    var cMenuExec = table.GetValue<ContextMenu>("MENU");
                    if (message.TryGetValue("ItemID", out var menuId))
                    {
                        var isFont = new[] { ".fon", ".otf", ".ttc", ".ttf" }.Contains(Path.GetExtension(cMenuExec.ItemsPath[0]), StringComparer.OrdinalIgnoreCase);
                        var verb = message.Get("CommandString", (string)null);
                        switch (verb)
                        {
                            case string _ when verb == "install" && isFont:
                                {
                                    var userFontDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Fonts");
                                    var destName = Path.Combine(userFontDir, Path.GetFileName(cMenuExec.ItemsPath[0]));
                                    Win32API.RunPowershellCommand($"-command \"Copy-Item '{cMenuExec.ItemsPath[0]}' '{userFontDir}'; New-ItemProperty -Name '{Path.GetFileNameWithoutExtension(cMenuExec.ItemsPath[0])}' -Path 'HKCU:\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Fonts' -PropertyType string -Value '{destName}'\"", false);
                                }
                                break;

                            case string _ when verb == "installAllUsers" && isFont:
                                {
                                    var winFontDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
                                    Win32API.RunPowershellCommand($"-command \"Copy-Item '{cMenuExec.ItemsPath[0]}' '{winFontDir}'; New-ItemProperty -Name '{Path.GetFileNameWithoutExtension(cMenuExec.ItemsPath[0])}' -Path 'HKLM:\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Fonts' -PropertyType string -Value '{Path.GetFileName(cMenuExec.ItemsPath[0])}'\"", true);
                                }
                                break;

                            case string _ when verb == "mount":
                                var vhdPath = cMenuExec.ItemsPath[0];
                                Win32API.MountVhdDisk(vhdPath);
                                break;

                            case string _ when verb == "format":
                                var drivePath = cMenuExec.ItemsPath[0];
                                Win32API.OpenFormatDriveDialog(drivePath);
                                break;

                            default:
                                cMenuExec?.InvokeItem((int)(long)menuId);
                                break;
                        }
                    }
                    // The following line is needed to cleanup resources when menu is closed.
                    // Unfortunately if you uncomment it some menu items will randomly stop working.
                    // Resource cleanup is currently done on app closing,
                    // if we find a solution for the issue above, we should cleanup as soon as a menu is closed.
                    //table.RemoveValue("MENU");
                    return null;

                default:
                    return null;
            }
        }

        private Func<string, bool> FilterMenuItems(bool showOpenMenu)
        {
            var knownItems = new List<string>()
            {
                "opennew", "opencontaining", "opennewprocess",
                "runas", "runasuser", "pintohome", "PinToStartScreen",
                "cut", "copy", "paste", "delete", "properties", "link",
                "Windows.ModernShare", "Windows.Share", "setdesktopwallpaper",
                "eject", "rename", "explore", "openinfiles", "extract",
                "copyaspath", "undelete", "empty", "Open in Windows Terminal",
                Win32API.ExtractStringFromDLL("shell32.dll", 30312), // SendTo menu
                Win32API.ExtractStringFromDLL("shell32.dll", 34593), // Add to collection
            };

            bool filterMenuItemsImpl(string menuItem) => !string.IsNullOrEmpty(menuItem)
                && (knownItems.Contains(menuItem) || (!showOpenMenu && menuItem.Equals("open", StringComparison.OrdinalIgnoreCase)));

            return filterMenuItemsImpl;
        }

        public void Dispose()
        {
            handleTable?.Dispose();
        }
    }
}
