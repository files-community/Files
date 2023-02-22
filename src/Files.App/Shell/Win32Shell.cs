using Files.Core;
using Files.Core.Extensions;
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
		private static ShellFolder controlPanel;

		private static ShellFolder controlPanelCategoryView;

		static Win32Shell()
		{
			controlPanel = new ShellFolder(Shell32.KNOWNFOLDERID.FOLDERID_ControlPanelFolder);

			controlPanelCategoryView = new ShellFolder("::{26EE0668-A00A-44D7-9371-BEB064C98683}");
		}

		public static async Task<(ShellFileItem Folder, List<ShellFileItem> Enumerate)> GetShellFolderAsync(string path, string action, int from, int count, params string[] properties)
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

					if ((controlPanel.PIDL.IsParentOf(shellFolder.PIDL, false) ||
						controlPanelCategoryView.PIDL.IsParentOf(shellFolder.PIDL, false)) &&
						(shellFolder is null || !shellFolder.Any()))
					{
						// Return null to force open unsupported items in explorer
						// only if inside control panel and folder appears empty
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

								foreach (var prop in properties)
									shellFileItem.Properties[prop] = SafetyExtensions.IgnoreExceptions(() => folderItem.Properties[prop]);

								flc.Add(shellFileItem);
							}
							catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
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
	}
}
