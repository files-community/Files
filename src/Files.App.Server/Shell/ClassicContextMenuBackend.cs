// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Storage.Interop;
using System.Diagnostics;
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
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Server
{
	internal sealed partial class ClassicContextMenuBackend : IDisposable
	{
		private const uint CmicMaskUnicode = 0x00004000;
		private static readonly ImageConverter IconConverter = new();

		private readonly IContextMenu contextMenu;
		private readonly HMENU menu;
		private readonly string? backgroundDirectory;
		private readonly Func<string?, bool>? itemFilter;
		private readonly Dictionary<List<Win32ContextMenuItem>, Action> loadSubMenuActions = [];
		private readonly HashSet<List<Win32ContextMenuItem>> submenus = [];

		public List<Win32ContextMenuItem> Items { get; } = [];

		internal ClassicContextMenuBackend(IShellItemArray selection, uint flags, Func<string?, bool>? itemFilter, ShellItem? backgroundFolder = null)
		{
			this.itemFilter = itemFilter;
			backgroundDirectory = backgroundFolder?.FileSystemPath;

			using var timeout = new ComCallTimeout(ContextMenu.ModernItemsTimeout);
			contextMenu = BindContextMenu(selection, backgroundFolder);
			menu = PInvoke.CreatePopupMenu();
			try
			{
				contextMenu.QueryContextMenu(menu, 0, 1, 0x7FFF, flags).ThrowOnFailure();
				EnumMenuItems(menu, Items);
				submenus.UnionWith(loadSubMenuActions.Keys);
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public bool HasSubmenu(List<Win32ContextMenuItem> items) => submenus.Contains(items);

		private static IContextMenu BindContextMenu(IShellItemArray selection, ShellItem? backgroundFolder)
		{
			// Uniquely owned, so Dispose can release it on this STA
			object? handler;
			if (backgroundFolder is not null)
			{
				((IUniqueShellItem)backgroundFolder.IShellItem).BindToHandler(null, PInvoke.BHID_SFViewObject, typeof(IContextMenu).GUID, out handler).ThrowOnFailure();
			}
			else
			{
				((IUniqueShellItemArray)selection).BindToHandler(null, PInvoke.BHID_SFUIObject, typeof(IContextMenu).GUID, out handler).ThrowOnFailure();
			}
			return handler as IContextMenu ?? throw new InvalidOperationException("The shell did not return a context menu.");
		}

		public unsafe HRESULT InvokeVerb(string verb)
		{
			byte[] verbBytes = Encoding.ASCII.GetBytes(verb + '\0');
			fixed (byte* verbPointer = verbBytes)
			fixed (char* verbPointerW = verb)
			fixed (char* directory = backgroundDirectory)
			{
				CMINVOKECOMMANDINFOEX commandInfo = default;
				commandInfo.cbSize = (uint)sizeof(CMINVOKECOMMANDINFOEX);
				commandInfo.fMask = CmicMaskUnicode;
				commandInfo.lpVerb = (PCSTR)verbPointer;
				commandInfo.lpVerbW = verbPointerW;
				commandInfo.lpDirectoryW = directory;
				commandInfo.nShow = (int)SHOW_WINDOW_CMD.SW_SHOWNORMAL;
				return contextMenu.InvokeCommand(in Unsafe.As<CMINVOKECOMMANDINFOEX, CMINVOKECOMMANDINFO>(ref commandInfo));
			}
		}

		public unsafe HRESULT InvokeItem(int itemId, string? workingDirectory)
		{
			fixed (char* directory = workingDirectory ?? backgroundDirectory)
			{
				CMINVOKECOMMANDINFOEX commandInfo = default;
				commandInfo.cbSize = (uint)sizeof(CMINVOKECOMMANDINFOEX);
				commandInfo.fMask = CmicMaskUnicode;
				commandInfo.lpVerb = (PCSTR)(byte*)(nuint)(uint)itemId;
				commandInfo.lpVerbW = (PCWSTR)(char*)(nuint)(uint)itemId;
				commandInfo.lpDirectoryW = directory;
				commandInfo.nShow = (int)SHOW_WINDOW_CMD.SW_SHOWNORMAL;
				return contextMenu.InvokeCommand(in Unsafe.As<CMINVOKECOMMANDINFOEX, CMINVOKECOMMANDINFO>(ref commandInfo));
			}
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
						menuItem.CommandString = GetCommandString(contextMenu, info.wID - 1);
						if (itemFilter is not null && (itemFilter(menuItem.CommandString) || itemFilter(menuItem.Label)))
							continue;

						if (!info.hbmpItem.IsNull && !Enum.IsDefined((HBITMAP_HMENU)((IntPtr)info.hbmpItem).ToInt64()))
						{
							using Bitmap? bitmap = ShellImage.GetBitmapFromHBitmap(info.hbmpItem);
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
							var subMenuIndex = index;
							menuItem.SubItems = subItems;
							if (loadSubmenus)
								LoadSubMenu();
							else
								loadSubMenuActions.Add(subItems, LoadSubMenu);

							void LoadSubMenu()
							{
								try
								{
									if (contextMenu is IContextMenu2 contextMenu2)
										contextMenu2.HandleMenuMsg(PInvoke.WM_INITMENUPOPUP, (WPARAM)(nuint)subMenu.Value, (LPARAM)(nint)subMenuIndex);
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

		public bool LoadSubMenu(List<Win32ContextMenuItem> subItems)
		{
			if (!loadSubMenuActions.Remove(subItems, out Action? loadSubMenu))
				return false;

			using var timeout = new ComCallTimeout(ContextMenu.ModernItemsTimeout);
			loadSubMenu();
			return true;
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

		public void Dispose()
		{
			loadSubMenuActions.Clear();
			if (!menu.IsNull)
				PInvoke.DestroyMenu(menu);
			if ((object)contextMenu is ComObject comObject)
				comObject.FinalRelease();
		}
	}
}
