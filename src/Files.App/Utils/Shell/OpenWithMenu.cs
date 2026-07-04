// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.Data.Items;
using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Com;
using Windows.Win32.System.Memory;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Utils.Shell
{
	internal sealed class OpenWithMenu : IDisposable
	{
		private static readonly ImageConverter IconConverter = new();

		private readonly ThreadWithMessageQueue owningThread;
		private unsafe IContextMenu* contextMenu;
		private HMENU menu;
		private bool disposedValue;

		private unsafe OpenWithMenu(IContextMenu* contextMenu, HMENU menu, ThreadWithMessageQueue owningThread)
		{
			this.contextMenu = contextMenu;
			this.menu = menu;
			this.owningThread = owningThread;
			Items = [];
		}

		public List<Win32ContextMenuItem> Items { get; }

		public static async Task<OpenWithMenu?> GetForFileAsync(string path)
		{
			var owningThread = new ThreadWithMessageQueue();
			var menu = await owningThread.PostMethod<OpenWithMenu?>(() =>
			{
				unsafe
				{
					return Create(path, owningThread);
				}
			});
			if (menu is null)
				owningThread.Dispose();

			return menu;
		}

		public async Task<bool> InvokeItem(int itemId)
		{
			unsafe
			{
				if (itemId < 0 || contextMenu is null)
					return false;
			}

			try
			{
				var currentWindows = Win32Helper.GetDesktopWindows();
				var hr = await owningThread.PostMethod<HRESULT>(() =>
				{
					unsafe
					{
						return InvokeItemCore(itemId);
					}
				});
				if (hr.Failed)
					return false;

				Win32Helper.BringToForeground(currentWindows);

				return true;
			}
			catch (Exception ex) when (ex is COMException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);
			}

			return false;
		}

		private unsafe HRESULT InvokeItemCore(int itemId)
		{
			if (contextMenu is null)
				return HRESULT.E_INVALIDARG;

			var commandInfo = new CMINVOKECOMMANDINFO
			{
				cbSize = (uint)sizeof(CMINVOKECOMMANDINFO),
				lpVerb = (PCSTR)(byte*)(nuint)(uint)itemId,
				nShow = (int)SHOW_WINDOW_CMD.SW_SHOWNORMAL,
			};

			return contextMenu->InvokeCommand(&commandInfo);
		}

		private static unsafe OpenWithMenu? Create(string path, ThreadWithMessageQueue owningThread)
		{
			IContextMenu* openWithContextMenu = default;
			HMENU hMenu = default;

			try
			{
				using ComPtr<IContextMenu2> openWithContextMenu2 = default;
				using ComPtr<IShellExtInit> shellExtInit = default;
				using ComPtr<IShellItem> shellItem = default;
				using ComPtr<IDataObject> dataObject = default;

				HRESULT hr = PInvoke.CoCreateInstance(CLSID.CLSID_OpenWithMenu, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IContextMenu, (void**)&openWithContextMenu);
				if (hr.ThrowIfFailedOnDebug().Failed)
					return null;

				hr = openWithContextMenu->QueryInterface(IID.IID_IContextMenu2, (void**)openWithContextMenu2.GetAddressOf());
				if (hr.ThrowIfFailedOnDebug().Failed)
					return null;

				hr = openWithContextMenu->QueryInterface(IID.IID_IShellExtInit, (void**)shellExtInit.GetAddressOf());
				if (hr.ThrowIfFailedOnDebug().Failed)
					return null;

				fixed (char* pathPtr = path)
				{
					hr = PInvoke.SHCreateItemFromParsingName(pathPtr, null, IID.IID_IShellItem, (void**)shellItem.GetAddressOf());
				}
				if (hr.ThrowIfFailedOnDebug().Failed)
					return null;

				hr = shellItem.Get()->BindToHandler(null, BHID.BHID_DataObject, IID.IID_IDataObject, (void**)dataObject.GetAddressOf());
				if (hr.ThrowIfFailedOnDebug().Failed)
					return null;

				hr = shellExtInit.Get()->Initialize(null, dataObject.Get(), default);
				if (hr.ThrowIfFailedOnDebug().Failed)
					return null;

				hMenu = PInvoke.CreatePopupMenu();
				hr = openWithContextMenu->QueryContextMenu(hMenu, 0, 1, 256, 0);
				if (hr.ThrowIfFailedOnDebug().Failed)
					return null;

				HMENU hSubMenu = PInvoke.GetSubMenu(hMenu, 0);
				if (hSubMenu.IsNull)
					return null;

				hr = openWithContextMenu2.Get()->HandleMenuMsg(PInvoke.WM_INITMENUPOPUP, (WPARAM)(nuint)hSubMenu.Value, 0);
				if (hr.ThrowIfFailedOnDebug().Failed)
					return null;

				var openWithMenu = new OpenWithMenu(openWithContextMenu, hMenu, owningThread);
				openWithContextMenu = default;
				hMenu = default;
				openWithMenu.EnumMenuItems(hSubMenu);

				return openWithMenu;
			}
			catch (Exception ex) when (ex is COMException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);
				return null;
			}
			finally
			{
				if (!hMenu.IsNull)
					PInvoke.DestroyMenu(hMenu);

				if (openWithContextMenu is not null)
					openWithContextMenu->Release();
			}
		}

		private unsafe void EnumMenuItems(HMENU hMenu)
		{
			uint count = unchecked((uint)PInvoke.GetMenuItemCount(hMenu));
			if (count is unchecked((uint)-1))
				return;

			for (uint index = 0; index < count; index++)
			{
				const uint bufferLength = 256;
				MENUITEMINFOW menuItemInfo = default;
				menuItemInfo.cbSize = (uint)sizeof(MENUITEMINFOW);
				menuItemInfo.fMask =
					MENU_ITEM_MASK.MIIM_BITMAP |
					MENU_ITEM_MASK.MIIM_FTYPE |
					MENU_ITEM_MASK.MIIM_ID |
					MENU_ITEM_MASK.MIIM_STATE |
					MENU_ITEM_MASK.MIIM_STRING;
				menuItemInfo.dwTypeData = (char*)NativeMemory.Alloc(bufferLength, sizeof(char));
				menuItemInfo.cch = bufferLength;

				try
				{
					if (!PInvoke.GetMenuItemInfo(hMenu, index, true, &menuItemInfo))
						continue;

					var menuItem = new ContextMenuItem
					{
						ID = (int)(menuItemInfo.wID - 1),
						Label = NormalizeLabel(menuItemInfo.dwTypeData.ToString()),
						Type = (MENU_ITEM_TYPE)menuItemInfo.fType,
					};

					if (!menuItemInfo.hbmpItem.IsNull && !Enum.IsDefined(typeof(HBITMAP_HMENU), ((IntPtr)menuItemInfo.hbmpItem).ToInt64()))
					{
						using Bitmap? bitmap = GetBitmapFromHBitmap(menuItemInfo.hbmpItem);
						if (bitmap is not null)
						{
							bitmap.MakeTransparent();
							menuItem.Icon = (byte[])IconConverter.ConvertTo(bitmap, typeof(byte[]));
						}
					}

					Items.Add(menuItem);
				}
				finally
				{
					NativeMemory.Free(menuItemInfo.dwTypeData);
				}
			}
		}

		private static Bitmap? GetBitmapFromHBitmap(HBITMAP hBitmap)
		{
			try
			{
				return Image.FromHbitmap((IntPtr)hBitmap);
			}
			catch
			{
				return null;
			}
		}

		private static string NormalizeLabel(string? rawLabel)
		{
			if (string.IsNullOrEmpty(rawLabel))
				return string.Empty;

			var labelBuilder = new System.Text.StringBuilder(rawLabel.Length);

			for (int i = 0; i < rawLabel.Length; i++)
			{
				char current = rawLabel[i];
				if (current != '&')
				{
					labelBuilder.Append(current);
					continue;
				}

				if (i + 1 >= rawLabel.Length)
				{
					labelBuilder.Append('&');
					continue;
				}

				char next = rawLabel[++i];
				if (next == '&')
					labelBuilder.Append('&');
				else
					labelBuilder.Append(next);
			}

			return labelBuilder.ToString();
		}

		public void Dispose()
		{
			if (disposedValue)
				return;

			if (!menu.IsNull)
			{
				PInvoke.DestroyMenu(menu);
				menu = default;
			}

			unsafe
			{
				if (contextMenu is not null)
				{
					contextMenu->Release();
					contextMenu = null;
				}
			}

			owningThread.Dispose();
			disposedValue = true;
		}
	}
}
