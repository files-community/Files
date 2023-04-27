using Files.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Vanara.InteropServices;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace Files.App.Shell
{
	[SupportedOSPlatform("Windows10.0.10240")]
	public class ContextMenu : Win32ContextMenu, IDisposable
	{
		private Shell32.IContextMenu cMenu;
		
		private User32.SafeHMENU hMenu;
		
		private ThreadWithMessageQueue owningThread;

		Func<string, bool>? itemFilter;

		private Dictionary<List<Win32ContextMenuItem>, Action> loadSubMenuActions;

		public List<string> ItemsPath { get; }

		private ContextMenu(Shell32.IContextMenu cMenu, User32.SafeHMENU hMenu, IEnumerable<string> itemsPath, ThreadWithMessageQueue owningThread, Func<string, bool>? itemFilter)
		{
			this.cMenu = cMenu;
			this.hMenu = hMenu;
			ItemsPath = itemsPath.ToList();
			Items = new List<Win32ContextMenuItem>();
			this.owningThread = owningThread;
			this.itemFilter = itemFilter;
			loadSubMenuActions = new();
		}

		public async static Task<bool> InvokeVerb(string verb, params string[] filePaths)
		{
			using var cMenu = await GetContextMenuForFiles(filePaths, Shell32.CMF.CMF_DEFAULTONLY);

			return cMenu is not null && await cMenu.InvokeVerb(verb);
		}

		public async Task<bool> InvokeVerb(string? verb)
		{
			if (string.IsNullOrEmpty(verb))
				return false;

			try
			{
				var currentWindows = Win32API.GetDesktopWindows();
				var pici = new Shell32.CMINVOKECOMMANDINFOEX
				{
					lpVerb = new SafeResourceId(verb, CharSet.Ansi),
					nShow = ShowWindowCommand.SW_SHOWNORMAL,
				};

				pici.cbSize = (uint)Marshal.SizeOf(pici);

				await owningThread.PostMethod(() => cMenu.InvokeCommand(pici));
				Win32API.BringToForeground(currentWindows);

				return true;
			}
			catch (Exception ex) when (ex is COMException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);
			}

			return false;
		}

		public async Task InvokeItem(int itemID)
		{
			if (itemID < 0)
				return;

			try
			{
				var currentWindows = Win32API.GetDesktopWindows();
				var pici = new Shell32.CMINVOKECOMMANDINFOEX
				{
					lpVerb = Macros.MAKEINTRESOURCE(itemID),
					nShow = ShowWindowCommand.SW_SHOWNORMAL,
				};

				pici.cbSize = (uint)Marshal.SizeOf(pici);

				await owningThread.PostMethod(() => cMenu.InvokeCommand(pici));

				Win32API.BringToForeground(currentWindows);
			}
			catch (Exception ex) when (ex is COMException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);
			}
		}

		#region FactoryMethods

		public async static Task<ContextMenu?> GetContextMenuForFiles(string[] filePathList, Shell32.CMF flags, Func<string, bool>? itemFilter = null)
		{
			var owningThread = new ThreadWithMessageQueue();
			return await owningThread.PostMethod<ContextMenu>(() =>
			{
				var shellItems = new List<ShellItem>();
				try
				{
					foreach (var fp in filePathList.Where(x => !string.IsNullOrEmpty(x)))
						shellItems.Add(ShellFolderExtensions.GetShellItemFromPathOrPidl(fp));
					return GetContextMenuForFiles(shellItems.ToArray(), flags, owningThread, itemFilter);
				}
				catch (Exception ex) when (ex is ArgumentException or FileNotFoundException)
				{
					// Return empty context menu
					return null;
				}
				finally
				{
					foreach (var si in shellItems)
					{
						si.Dispose();
					}
				}
			});
		}

		public async static Task<ContextMenu?> GetContextMenuForFiles(ShellItem[] shellItems, Shell32.CMF flags, Func<string, bool>? itemFilter = null)
		{
			var owningThread = new ThreadWithMessageQueue();
			return await owningThread.PostMethod<ContextMenu>(()
				=> GetContextMenuForFiles(shellItems, flags, owningThread, itemFilter));
		}

		private static ContextMenu? GetContextMenuForFiles(ShellItem[] shellItems, Shell32.CMF flags, ThreadWithMessageQueue owningThread, Func<string, bool>? itemFilter = null)
		{
			if (!shellItems.Any())
				return null;

			try
			{
				// HP: The items are all in the same folder
				using var sf = shellItems[0].Parent;

				Shell32.IContextMenu menu = sf.GetChildrenUIObjects<Shell32.IContextMenu>(default, shellItems);
				var hMenu = User32.CreatePopupMenu();
				menu.QueryContextMenu(hMenu, 0, 1, 0x7FFF, flags);
				var contextMenu = new ContextMenu(menu, hMenu, shellItems.Select(x => x.ParsingName), owningThread, itemFilter);
				contextMenu.EnumMenuItems(hMenu, contextMenu.Items);

				return contextMenu;
			}
			catch (COMException)
			{
				// Return empty context menu
				return null;
			}
		}

		public static async Task WarmUpQueryContextMenuAsync()
		{
			using var cMenu = await GetContextMenuForFiles(new string[] { "C:\\" }, Shell32.CMF.CMF_NORMAL);
		}

		#endregion FactoryMethods

		private void EnumMenuItems(
			HMENU hMenu,
			List<Win32ContextMenuItem> menuItemsResult,
			bool loadSubenus = false)
		{
			var itemCount = User32.GetMenuItemCount(hMenu);

			var mii = new User32.MENUITEMINFO()
			{
				fMask = User32.MenuItemInfoMask.MIIM_BITMAP |
					User32.MenuItemInfoMask.MIIM_FTYPE |
					User32.MenuItemInfoMask.MIIM_STRING |
					User32.MenuItemInfoMask.MIIM_ID |
					User32.MenuItemInfoMask.MIIM_SUBMENU,
			};

			mii.cbSize = (uint)Marshal.SizeOf(mii);

			for (uint ii = 0; ii < itemCount; ii++)
			{
				var menuItem = new ContextMenuItem();
				var container = new SafeCoTaskMemString(512);
				var cMenu2 = cMenu as Shell32.IContextMenu2;

				mii.dwTypeData = (IntPtr)container;

				// https://devblogs.microsoft.com/oldnewthing/20040928-00/?p=37723
				mii.cch = (uint)container.Capacity - 1;

				var retval = User32.GetMenuItemInfo(hMenu, ii, true, ref mii);
				if (!retval)
				{
					container.Dispose();
					continue;
				}

				menuItem.Type = (MenuItemType)mii.fType;

				// wID - idCmdFirst
				menuItem.ID = (int)(mii.wID - 1);

				if (menuItem.Type == MenuItemType.MFT_STRING)
				{
					Debug.WriteLine("Item {0} ({1}): {2}", ii, mii.wID, mii.dwTypeData);

					// Hackish workaround to avoid an AccessViolationException on some items,
					// notably the "Run with graphic processor" menu item of NVidia cards
					if (mii.wID - 1 > 5000)
					{
						container.Dispose();

						continue;
					}

					menuItem.Label = mii.dwTypeData;
					menuItem.CommandString = GetCommandString(cMenu, mii.wID - 1);

					if (itemFilter is not null && (itemFilter(menuItem.CommandString) || itemFilter(menuItem.Label)))
					{
						// Skip items implemented in UWP
						container.Dispose();

						continue;
					}

					if (mii.hbmpItem != HBITMAP.NULL && !Enum.IsDefined(typeof(HBITMAP_HMENU), ((IntPtr)mii.hbmpItem).ToInt64()))
					{
						using var bitmap = Win32API.GetBitmapFromHBitmap(mii.hbmpItem);

						if (bitmap is not null)
						{
							byte[] bitmapData = (byte[])new ImageConverter().ConvertTo(bitmap, typeof(byte[]));
							menuItem.Icon = bitmapData;
						}
					}

					if (mii.hSubMenu != HMENU.NULL)
					{
						Debug.WriteLine("Item {0}: has submenu", ii);
						var subItems = new List<Win32ContextMenuItem>();
						var hSubMenu = mii.hSubMenu;

						if (loadSubenus)
						{
							LoadSubMenu(hSubMenu);
						}
						else
						{
							loadSubMenuActions.Add(subItems, () => LoadSubMenu(hSubMenu));
						}

						menuItem.SubItems = subItems;

						Debug.WriteLine("Item {0}: done submenu", ii);

						void LoadSubMenu(HMENU hSubMenu)
						{
							try
							{
								cMenu2?.HandleMenuMsg((uint)User32.WindowMessage.WM_INITMENUPOPUP, (IntPtr)hSubMenu, new IntPtr(ii));
							}
							catch (Exception ex) when (ex is COMException or NotImplementedException)
							{
								// Only for dynamic/owner drawn? (open with, etc)
							}

							EnumMenuItems(hSubMenu, subItems, true);
						}
					}
				}
				else
				{
					Debug.WriteLine("Item {0}: {1}", ii, mii.fType.ToString());
				}

				container.Dispose();
				menuItemsResult.Add(menuItem);
			}
		}

		public async Task<bool> LoadSubMenu(List<Win32ContextMenuItem> subItems)
		{
			return await owningThread.PostMethod<bool>(() =>
			{
				var result = loadSubMenuActions.Remove(subItems, out var loadSubMenuAction);

				if (result)
				{
					try
					{
						loadSubMenuAction!();
					}
					catch (COMException)
					{
						result = false;
					}
				}

				return result;
			});
		}

		private static string? GetCommandString(Shell32.IContextMenu cMenu, uint offset, Shell32.GCS flags = Shell32.GCS.GCS_VERBW)
		{
			SafeCoTaskMemString? commandString = null;

			try
			{
				commandString = new SafeCoTaskMemString(512);
				cMenu.GetCommandString(new IntPtr(offset), flags, IntPtr.Zero, commandString, (uint)commandString.Capacity - 1);
				Debug.WriteLine("Verb {0}: {1}", offset, commandString);

				return commandString.ToString();
			}
			catch (Exception ex) when (ex is InvalidCastException or ArgumentException)
			{
				// TODO: Investigate this...
				Debug.WriteLine(ex);

				return null;
			}

			catch (Exception ex) when (ex is COMException or NotImplementedException)
			{
				// Not every item has an associated verb
				return null;
			}
			finally
			{
				commandString?.Dispose();
			}
		}

		#region IDisposable Support

		// To detect redundant calls
		private bool disposedValue = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: Dispose managed state (managed objects)
					if (Items is not null)
					{
						foreach (var si in Items)
						{
							(si as IDisposable)?.Dispose();
						}

						Items = null;
					}
				}

				// TODO: Free unmanaged resources (unmanaged objects) and override a finalizer below
				if (hMenu is not null)
				{
					User32.DestroyMenu(hMenu);
					hMenu = null;
				}
				if (cMenu is not null)
				{
					Marshal.ReleaseComObject(cMenu);
					cMenu = null;
				}

				owningThread.Dispose();

				disposedValue = true;
			}
		}

		~ContextMenu()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion IDisposable Support

		public enum HBITMAP_HMENU : long
		{
			HBMMENU_CALLBACK = -1,
			HBMMENU_MBAR_CLOSE = 5,
			HBMMENU_MBAR_CLOSE_D = 6,
			HBMMENU_MBAR_MINIMIZE = 3,
			HBMMENU_MBAR_MINIMIZE_D = 7,
			HBMMENU_MBAR_RESTORE = 2,
			HBMMENU_POPUP_CLOSE = 8,
			HBMMENU_POPUP_MAXIMIZE = 10,
			HBMMENU_POPUP_MINIMIZE = 11,
			HBMMENU_POPUP_RESTORE = 9,
			HBMMENU_SYSTEM = 1
		}
	}
}
