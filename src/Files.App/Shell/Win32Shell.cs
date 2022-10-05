#nullable enable

using Files.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace Files.App.Shell
{
    public class Win32Shell
    {
        private static ShellFolder controlPanel, controlPanelCategoryView;

        static Win32Shell()
        {
            controlPanel = new ShellFolder(Shell32.KNOWNFOLDERID.FOLDERID_ControlPanelFolder);
            controlPanelCategoryView = new ShellFolder("::{26EE0668-A00A-44D7-9371-BEB064C98683}");
        }

        public static async Task<(ShellFileItem Folder, List<ShellFileItem> Enumerate)> GetShellFolderAsync(string path, string action, int from, int count)
        {
            if (path.StartsWith("::{", StringComparison.Ordinal))
            {
                path = $"shell:{path}";
            }

            return await Win32API.StartSTATask(() =>
            {
                var flc = new List<ShellFileItem>();
                var folder = (ShellFileItem)null;
                try
                {
                    using var shellFolder = ShellFolderExtensions.GetShellItemFromPathOrPidl(path) as ShellFolder;
                    folder = ShellFolderExtensions.GetShellFileItem(shellFolder);
                    if ((controlPanel.PIDL.IsParentOf(shellFolder.PIDL, false) || controlPanelCategoryView.PIDL.IsParentOf(shellFolder.PIDL, false))
                        && !shellFolder.Any())
                    {
                        // Return null to force open unsupported items in explorer
                        // Only if inside control panel and folder appears empty
                        return (null, flc);
                    }
                    if (action == "Enumerate")
                    {
                        foreach (var folderItem in shellFolder.Skip(from).Take(count))
                        {
                            try
                            {
                                var shellFileItem = folderItem is ShellLink link ?
                                    ShellFolderExtensions.GetShellLinkItem(link) :
                                    ShellFolderExtensions.GetShellFileItem(folderItem);
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
                return (folder, flc);
            });
        }

        public static (bool HasRecycleBin, long NumItems, long BinSize) QueryRecycleBin(string drive = "")
        {
            Win32API.SHQUERYRBINFO queryBinInfo = new Win32API.SHQUERYRBINFO();
            queryBinInfo.cbSize = Marshal.SizeOf(queryBinInfo);
            var res = Win32API.SHQueryRecycleBin(drive, ref queryBinInfo);
            if (res == HRESULT.S_OK)
            {
                var numItems = queryBinInfo.i64NumItems;
                var binSize = queryBinInfo.i64Size;
                return (true, numItems, binSize);
            }
            else
            {
                return (false, 0, 0);
            }
        }

        public static async Task<ShellLinkItem?> ParseLink(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            string targetPath = string.Empty;

            try
            {
                if (filePath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    using var link = new ShellLink(filePath, LinkResolution.NoUIWithMsgPump, null, TimeSpan.FromMilliseconds(100));
                    targetPath = link.TargetPath;
                    return ShellFolderExtensions.GetShellLinkItem(link);
                }

                if (filePath.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
                {
                    targetPath = await Win32API.StartSTATask(() =>
                    {
                        var ipf = new Url.IUniformResourceLocator();
                        (ipf as System.Runtime.InteropServices.ComTypes.IPersistFile)?.Load(filePath, 0);
                        ipf.GetUrl(out var retVal);
                        return retVal;
                    });

                    return string.IsNullOrEmpty(targetPath) ? null : new ShellLinkItem { TargetPath = targetPath };
                }
            }
            catch (FileNotFoundException ex) // Could not parse shortcut
            {
                App.Logger?.Warn(ex, ex.Message);
                // Return a item containing the invalid target path
                return new ShellLinkItem
                {
                    TargetPath = string.IsNullOrEmpty(targetPath) ? string.Empty : targetPath,
                    InvalidTarget = true
                };
            }
            catch (Exception ex)
            {
                App.Logger?.Warn(ex, ex.Message);
            }
            return null;
        }
    }
}
