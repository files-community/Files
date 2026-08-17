// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Memory;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Provides a helper for Win32 context menus.
	/// </summary>
	public sealed partial class ContextMenu : Win32ContextMenu, IDisposable
	{
		private const uint CmicMaskUnicode = 0x00004000;
		private static readonly ImageConverter IconConverter = new();

		private IContextMenu? contextMenu;
		private HMENU menu;
		private readonly ContextMenuWorkerPool.Worker worker;
		private readonly Func<string?, bool>? itemFilter;
		private readonly Dictionary<List<Win32ContextMenuItem>, Action> loadSubMenuActions = [];
		private bool disposedValue;

		private ThreadWithMessageQueue OwningThread => worker.Thread;

		private IContextMenu Menu => contextMenu ?? throw new ObjectDisposedException(nameof(ContextMenu));

		public List<string> ItemsPath { get; }

		private ContextMenu(IContextMenu contextMenu, HMENU menu, IEnumerable<string> itemsPath, ContextMenuWorkerPool.Worker worker, Func<string?, bool>? itemFilter)
		{
			this.contextMenu = contextMenu;
			this.menu = menu;
			this.worker = worker;
			this.itemFilter = itemFilter;
			ItemsPath = itemsPath.ToList();
			Items = [];
		}

		public static async Task<bool> InvokeVerb(string verb, params string?[] filePaths)
		{
			using var contextMenu = await GetContextMenuForFiles(filePaths, PInvoke.CMF_DEFAULTONLY);
			return contextMenu is not null && await contextMenu.InvokeVerb(verb);
		}

		public async Task<bool> InvokeVerb(string? verb)
		{
			if (string.IsNullOrEmpty(verb))
				return false;

			var items = Items ?? throw new InvalidOperationException("The shell context menu has not been initialized.");
			var item = items.FirstOrDefault(x => x.CommandString == verb);
			if (item is not null && item.ID >= 0)
				return await InvokeItem(item.ID);

			try
			{
				var currentWindows = Win32Helper.GetDesktopWindows();
				HRESULT result = await OwningThread.PostMethod(() => InvokeVerbCore(verb));
				if (result.Failed)
					return false;
				Win32Helper.BringToForeground(currentWindows);
				return true;
			}
			catch (Exception ex) when (ex is COMException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);
				return false;
			}
		}

		public async Task<bool> InvokeItem(int itemId, string? workingDirectory = null)
		{
			if (itemId < 0)
				return false;

			try
			{
				var currentWindows = Win32Helper.GetDesktopWindows();
				HRESULT result = await OwningThread.PostMethod(() => InvokeItemCore(itemId, workingDirectory));
				if (result.Failed)
					return false;
				Win32Helper.BringToForeground(currentWindows);
				return true;
			}
			catch (Exception ex) when (ex is COMException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);
				return false;
			}
		}

		private unsafe HRESULT InvokeVerbCore(string verb)
		{
			byte[] verbBytes = Encoding.ASCII.GetBytes(verb + '\0');
			fixed (byte* verbPointer = verbBytes)
			{
				CMINVOKECOMMANDINFOEX commandInfo = default;
				commandInfo.cbSize = (uint)sizeof(CMINVOKECOMMANDINFOEX);
				commandInfo.lpVerb = (PCSTR)verbPointer;
				commandInfo.nShow = (int)SHOW_WINDOW_CMD.SW_SHOWNORMAL;
				return Menu.InvokeCommand((CMINVOKECOMMANDINFO*)&commandInfo);
			}
		}

		private unsafe HRESULT InvokeItemCore(int itemId, string? workingDirectory)
		{
			fixed (char* directory = workingDirectory)
			{
				CMINVOKECOMMANDINFOEX commandInfo = default;
				commandInfo.cbSize = (uint)sizeof(CMINVOKECOMMANDINFOEX);
				commandInfo.fMask = CmicMaskUnicode;
				commandInfo.lpVerb = (PCSTR)(byte*)(nuint)(uint)itemId;
				commandInfo.lpVerbW = (PCWSTR)(char*)(nuint)(uint)itemId;
				commandInfo.lpDirectoryW = directory;
				commandInfo.nShow = (int)SHOW_WINDOW_CMD.SW_SHOWNORMAL;
				return Menu.InvokeCommand((CMINVOKECOMMANDINFO*)&commandInfo);
			}
		}

		public static async Task<ContextMenu?> GetContextMenuForFiles(string?[] filePathList, uint flags, Func<string?, bool>? itemFilter = null)
		{
			var worker = ContextMenuWorkerPool.Rent();
			var contextMenu = await worker.Thread.PostMethod<ContextMenu?>(() =>
			{
				var shellItems = new List<ShellItem>();
				try
				{
					foreach (string path in filePathList.WhereNotNull().Where(path => !string.IsNullOrEmpty(path)))
						shellItems.Add(ShellFolderExtensions.GetShellItemFromPathOrPIDL(path));
					return Create([.. shellItems], flags, worker, itemFilter);
				}
				catch
				{
					return null;
				}
				finally
				{
					foreach (ShellItem item in shellItems)
						item.Dispose();
				}
			});

			if (contextMenu is null)
				ContextMenuWorkerPool.Return(worker);
			return contextMenu;
		}

		public static async Task<ContextMenu?> GetContextMenuForFiles(ShellItem[] shellItems, uint flags, Func<string?, bool>? itemFilter = null)
		{
			var worker = ContextMenuWorkerPool.Rent();
			var contextMenu = await worker.Thread.PostMethod<ContextMenu?>(() => Create(shellItems, flags, worker, itemFilter));
			if (contextMenu is null)
				ContextMenuWorkerPool.Return(worker);
			return contextMenu;
		}

		private static unsafe ContextMenu? Create(ShellItem[] shellItems, uint flags, ContextMenuWorkerPool.Worker worker, Func<string?, bool>? itemFilter)
		{
			if (shellItems.Length is 0)
				return null;

			ITEMIDLIST** pidls = null;
			HMENU menu = default;
			try
			{
				pidls = (ITEMIDLIST**)NativeMemory.AllocZeroed((nuint)shellItems.Length, (nuint)sizeof(ITEMIDLIST*));
				for (int index = 0; index < shellItems.Length; index++)
					PInvoke.SHGetIDListFromObject(shellItems[index].IShellItem, out pidls[index]).ThrowOnFailure();

				PInvoke.SHCreateShellItemArrayFromIDLists((uint)shellItems.Length, pidls, out IShellItemArray itemArray).ThrowOnFailure();
				IContextMenu shellContextMenu = BindContextMenu(itemArray);

				menu = PInvoke.CreatePopupMenu();
				shellContextMenu.QueryContextMenu(menu, 0, 1, 0x7FFF, flags).ThrowOnFailure();
				var contextMenu = new ContextMenu(shellContextMenu, menu, shellItems.Select(item => item.ParsingName).WhereNotNull(), worker, itemFilter);
				menu = default;
				contextMenu.EnumMenuItems(contextMenu.menu, contextMenu.Items!);
				return contextMenu;
			}
			catch (COMException)
			{
				return null;
			}
			finally
			{
				if (!menu.IsNull)
					PInvoke.DestroyMenu(menu);
				if (pidls is not null)
				{
					for (int index = 0; index < shellItems.Length; index++)
						PInvoke.CoTaskMemFree(pidls[index]);
					NativeMemory.Free(pidls);
				}
			}
		}

		private static unsafe IContextMenu BindContextMenu(IShellItemArray itemArray)
		{
			void* itemArrayPointer = ComInterfaceMarshaller<IShellItemArray>.ConvertToUnmanaged(itemArray);
			void* contextMenuPointer = null;
			try
			{
				// Bind through the native vtable so the result can use a uniquely owned generated COM wrapper.
				void** vtable = *(void***)itemArrayPointer;
				var bindToHandler =
					(delegate* unmanaged[MemberFunction]<void*, void*, Guid*, Guid*, void**, int>)vtable[3];
				Guid handlerId = PInvoke.BHID_SFUIObject;
				Guid interfaceId = typeof(IContextMenu).GUID;
				HRESULT result = new(bindToHandler(itemArrayPointer, null, &handlerId, &interfaceId, &contextMenuPointer));
				result.ThrowOnFailure();
				return UniqueComInterfaceMarshaller<IContextMenu>.ConvertToManaged(contextMenuPointer)
					?? throw new InvalidOperationException("The shell did not return a context menu.");
			}
			finally
			{
				UniqueComInterfaceMarshaller<IContextMenu>.Free(contextMenuPointer);
				ComInterfaceMarshaller<IShellItemArray>.Free(itemArrayPointer);
			}
		}

		public static async Task WarmUpQueryContextMenuAsync()
		{
			using var contextMenu = await GetContextMenuForFiles([$@"{Constants.UserEnvironmentPaths.SystemDrivePath}\"], PInvoke.CMF_NORMAL);
		}

		private unsafe void EnumMenuItems(HMENU targetMenu, List<Win32ContextMenuItem> result, bool loadSubmenus = false)
		{
			uint itemCount = unchecked((uint)PInvoke.GetMenuItemCount(targetMenu));
			if (itemCount is unchecked((uint)-1))
				return;

			for (uint index = 0; index < itemCount; index++)
			{
				const uint bufferLength = 512;
				MENUITEMINFOW info = default;
				info.cbSize = (uint)sizeof(MENUITEMINFOW);
				info.fMask = MENU_ITEM_MASK.MIIM_BITMAP | MENU_ITEM_MASK.MIIM_FTYPE | MENU_ITEM_MASK.MIIM_STRING | MENU_ITEM_MASK.MIIM_ID | MENU_ITEM_MASK.MIIM_SUBMENU;
				info.dwTypeData = (char*)NativeMemory.AllocZeroed(bufferLength, sizeof(char));
				info.cch = bufferLength - 1;

				try
				{
					if (!PInvoke.GetMenuItemInfo(targetMenu, index, true, &info))
						continue;

					var menuItem = new ContextMenuItem { Type = (MENU_ITEM_TYPE)info.fType, ID = (int)(info.wID - 1) };
					if (menuItem.Type == MENU_ITEM_TYPE.MFT_STRING)
					{
						menuItem.Label = info.dwTypeData.ToString();
						menuItem.CommandString = GetCommandString(Menu, info.wID - 1);
						if (itemFilter is not null && (itemFilter(menuItem.CommandString) || itemFilter(menuItem.Label)))
							continue;

						if (!info.hbmpItem.IsNull && !Enum.IsDefined((HBITMAP_HMENU)((IntPtr)info.hbmpItem).ToInt64()))
						{
							using Bitmap? bitmap = Win32Helper.GetBitmapFromHBitmap(info.hbmpItem);
							if (bitmap is not null)
							{
								bitmap.MakeTransparent();
								if (IconConverter.ConvertTo(bitmap, typeof(byte[])) is byte[] icon)
									menuItem.Icon = icon;
							}
						}

						if (!info.hSubMenu.IsNull)
						{
							var subItems = new List<Win32ContextMenuItem>();
							HMENU subMenu = info.hSubMenu;
							menuItem.SubItems = subItems;
							if (loadSubmenus)
								LoadSubMenu();
							else
								loadSubMenuActions.Add(subItems, LoadSubMenu);

							void LoadSubMenu()
							{
								try
								{
									if (Menu is IContextMenu2 contextMenu2)
										contextMenu2.HandleMenuMsg(PInvoke.WM_INITMENUPOPUP, (WPARAM)(nuint)subMenu.Value, (LPARAM)(nint)index);
								}
								catch (Exception ex) when (ex is InvalidCastException or ArgumentException or COMException or NotImplementedException)
								{
									Debug.WriteLine(ex);
								}
								EnumMenuItems(subMenu, subItems, true);
							}
						}
					}

					result.Add(menuItem);
				}
				finally
				{
					NativeMemory.Free(info.dwTypeData);
				}
			}
		}

		public Task<bool> LoadSubMenu(List<Win32ContextMenuItem> subItems)
		{
			if (!loadSubMenuActions.Remove(subItems, out Action? loadSubMenu))
				return Task.FromResult(false);

			return OwningThread.PostMethod(() =>
			{
				try
				{
					loadSubMenu();
					return true;
				}
				catch
				{
					return false;
				}
			});
		}

		private static unsafe string? GetCommandString(IContextMenu contextMenu, uint offset)
		{
			// Avoid an AccessViolationException from handlers that return abnormally large command offsets,
			// notably the "Run with graphics processor" menu item from NVIDIA.
			if (offset > 5000)
				return null;

			const int capacity = 512;
			char* buffer = (char*)NativeMemory.AllocZeroed((nuint)capacity, sizeof(char));
			try
			{
				return contextMenu.GetCommandString(offset, PInvoke.GCS_VERBW, (PSTR)(byte*)buffer, capacity).Succeeded ? new string(buffer) : null;
			}
			catch (Exception ex) when (ex is InvalidCastException or ArgumentException or COMException or NotImplementedException)
			{
				Debug.WriteLine(ex);
				return null;
			}
			finally
			{
				NativeMemory.Free(buffer);
			}
		}

		private void Dispose(bool disposing)
		{
			if (disposedValue)
				return;

			if (disposing && Items is not null)
			{
				foreach (Win32ContextMenuItem item in Items)
					(item as IDisposable)?.Dispose();
			}

			// Release the native menu on the worker's own STA thread. The message queue is FIFO,
			// so cleanup completes before work posted by the next renter.
			HMENU menuToDestroy = menu;
			IContextMenu? contextMenuToRelease = contextMenu;
			menu = default;
			contextMenu = null;
			OwningThread.PostMethod(() =>
			{
				if (!menuToDestroy.IsNull)
					PInvoke.DestroyMenu(menuToDestroy);
				if ((object?)contextMenuToRelease is ComObject comObject)
					comObject.FinalRelease();
			});
			ContextMenuWorkerPool.Return(worker);
			disposedValue = true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~ContextMenu()
		{
			Dispose(false);
		}
	}
}
