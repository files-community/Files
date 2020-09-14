using Files.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Vanara.InteropServices;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static Vanara.PInvoke.Gdi32;

namespace FilesFullTrust
{
    internal partial class Win32API
    {
        public class ThreadWithMessageQueue<T> : IDisposable
        {
            private BlockingCollection<Internal> messageQueue;
            private Thread thread;
            private DisposableDictionary state;

            public void Dispose()
            {
                messageQueue.CompleteAdding();
                thread.Join();
                state.Dispose();
            }

            public async Task<V> PostMessage<V>(T payload)
            {
                var message = new Internal(payload);
                messageQueue.TryAdd(message);
                return (V)await message.tcs.Task;
            }

            public Task PostMessage(T payload)
            {
                var message = new Internal(payload);
                messageQueue.TryAdd(message);
                return message.tcs.Task;
            }

            public ThreadWithMessageQueue(Func<T, DisposableDictionary, object> handleMessage)
            {
                messageQueue = new BlockingCollection<Internal>(new ConcurrentQueue<Internal>());
                state = new DisposableDictionary();
                thread = new Thread(new ThreadStart(() =>
                {
                    foreach (var message in messageQueue.GetConsumingEnumerable())
                    {
                        var res = handleMessage(message.payload, state);
                        message.tcs.SetResult(res);
                    }
                }));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }

            private class Internal
            {
                public T payload;
                public TaskCompletionSource<object> tcs;

                public Internal(T payload)
                {
                    this.payload = payload;
                    this.tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                }
            }
        }

        public class DisposableDictionary : IDisposable
        {
            private ConcurrentDictionary<string, object> _dict;

            public DisposableDictionary()
            {
                _dict = new ConcurrentDictionary<string, object>();
            }

            public string AddValue(object obj)
            {
                string key = Guid.NewGuid().ToString();
                if (!_dict.TryAdd(key, obj))
                    throw new ArgumentException("Could not create handle: key exists");
                return key;
            }

            public void SetValue(string key, object obj)
            {
                RemoveValue(key);
                if (!_dict.TryAdd(key, obj))
                    throw new ArgumentException("Could not create handle: key exists");
            }

            public object GetValue(string key)
            {
                _dict.TryGetValue(key, out var elem);
                return elem;
            }

            public T GetValue<T>(string key)
            {
                _dict.TryGetValue(key, out var elem);
                return (T)elem;
            }

            public void RemoveValue(string key)
            {
                _dict.TryRemove(key, out var elem);
                (elem as IDisposable)?.Dispose();
            }

            public void Dispose()
            {
                foreach (var elem in _dict)
                {
                    _dict.TryRemove(elem.Key, out _);
                    (elem.Value as IDisposable)?.Dispose();
                }
            }
        }

        public class ContextMenu : Win32ContextMenu, IDisposable
        {
            private Shell32.IContextMenu cMenu;

            public ContextMenu(Shell32.IContextMenu cMenu)
            {
                this.cMenu = cMenu;
                this.Items = new List<Win32ContextMenuItem>();
            }

            public void InvokeVerb(string verb)
            {
                if (string.IsNullOrEmpty(verb)) return;
                try
                {
                    var pici = new Shell32.CMINVOKECOMMANDINFOEX();
                    pici.lpVerb = new SafeResourceId(verb, CharSet.Ansi);
                    pici.nShow = ShowWindowCommand.SW_SHOWNORMAL;
                    pici.cbSize = (uint)Marshal.SizeOf(pici);
                    cMenu.InvokeCommand(pici);
                }
                catch (Exception ex) when (
                    ex is COMException
                    || ex is UnauthorizedAccessException)
                {
                    Debug.WriteLine(ex);
                }
            }

            public void InvokeItem(int itemID)
            {
                if (itemID < 0) return;
                try
                {
                    var pici = new Shell32.CMINVOKECOMMANDINFOEX();
                    pici.lpVerb = Macros.MAKEINTRESOURCE(itemID);
                    pici.nShow = ShowWindowCommand.SW_SHOWNORMAL;
                    pici.cbSize = (uint)Marshal.SizeOf(pici);
                    cMenu.InvokeCommand(pici);
                }
                catch (Exception ex) when (
                    ex is COMException
                    || ex is UnauthorizedAccessException)
                {
                    Debug.WriteLine(ex);
                }
            }

            #region FactoryMethods

            public static ContextMenu GetContextMenuForFiles(string[] filePathList, Shell32.CMF flags, Func<string, bool> itemFilter = null)
            {
                List<ShellItem> shellItems = new List<ShellItem>();
                try
                {
                    foreach (var fp in filePathList.Where(x => !string.IsNullOrEmpty(x)))
                        shellItems.Add(new ShellItem(fp));
                    return GetContextMenuForFiles(shellItems.ToArray(), flags, itemFilter);
                }
                catch (ArgumentException)
                {
                    // Return empty context menu
                    return null;
                }
                finally
                {
                    foreach (var si in shellItems)
                        si.Dispose();
                }
            }

            public static ContextMenu GetContextMenuForFiles(ShellItem[] shellItems, Shell32.CMF flags, Func<string, bool> itemFilter = null)
            {
                if (shellItems == null || !shellItems.Any())
                    return null;
                using var sf = shellItems.First().Parent; // HP: the items are all in the same folder
                Shell32.IContextMenu menu = sf.GetChildrenUIObjects<Shell32.IContextMenu>(null, shellItems);
                var contextMenu = new ContextMenu(menu);
                var hMenu = User32.CreatePopupMenu();
                menu.QueryContextMenu(hMenu, 0, 1, 0x7FFF, flags);
                ContextMenu.EnumMenuItems(menu, hMenu, contextMenu.Items, itemFilter);
                User32.DestroyMenu(hMenu);
                return contextMenu;
            }

            #endregion FactoryMethods

            private static void EnumMenuItems(
                Shell32.IContextMenu cMenu,
                HMENU hMenu,
                List<Win32ContextMenuItem> menuItemsResult,
                Func<string, bool> itemFilter = null)
            {
                var itemCount = User32.GetMenuItemCount(hMenu);
                var mii = new User32.MENUITEMINFO();
                mii.cbSize = (uint)Marshal.SizeOf(mii);
                mii.fMask = User32.MenuItemInfoMask.MIIM_BITMAP
                    | User32.MenuItemInfoMask.MIIM_FTYPE
                    | User32.MenuItemInfoMask.MIIM_STRING
                    | User32.MenuItemInfoMask.MIIM_ID
                    | User32.MenuItemInfoMask.MIIM_SUBMENU;
                for (uint ii = 0; ii < itemCount; ii++)
                {
                    var menuItem = new ContextMenuItem();
                    var container = new SafeCoTaskMemString(512);
                    mii.dwTypeData = (IntPtr)container;
                    mii.cch = (uint)container.Capacity - 1; // https://devblogs.microsoft.com/oldnewthing/20040928-00/?p=37723
                    var retval = User32.GetMenuItemInfo(hMenu, ii, true, ref mii);
                    if (!retval)
                    {
                        container.Dispose();
                        continue;
                    }
                    menuItem.Type = (MenuItemType)mii.fType;
                    menuItem.ID = (int)(mii.wID - 1); // wID - idCmdFirst
                    if (menuItem.Type == MenuItemType.MFT_STRING)
                    {
                        Debug.WriteLine("Item {0} ({1}): {2}", ii, mii.wID, mii.dwTypeData);
                        menuItem.Label = mii.dwTypeData;
                        menuItem.CommandString = GetCommandString(cMenu, mii.wID - 1);
                        if (itemFilter != null && (itemFilter(menuItem.CommandString) || itemFilter(menuItem.Label)))
                        {
                            // Skip items implemented in UWP
                            container.Dispose();
                            continue;
                        }
                        if (mii.hbmpItem != HBITMAP.NULL)
                        {
                            var bitmap = GetItemBitmap(mii.hbmpItem);
                            menuItem.Icon = bitmap;
                        }
                        if (mii.hSubMenu != HMENU.NULL)
                        {
                            Debug.WriteLine("Item {0}: has submenu", ii);
                            var subItems = new List<Win32ContextMenuItem>();
                            try
                            {
                                (cMenu as Shell32.IContextMenu2)?.HandleMenuMsg((uint)User32.WindowMessage.WM_INITMENUPOPUP, (IntPtr)mii.hSubMenu, new IntPtr(ii));
                                // Skip this items, clicking on them probably won't work
                                container.Dispose();
                                continue;
                            }
                            catch (NotImplementedException)
                            {
                                // Only for dynamic/owner drawn? (open with, etc)
                            }
                            EnumMenuItems(cMenu, mii.hSubMenu, subItems, itemFilter);
                            menuItem.SubItems = subItems;
                            Debug.WriteLine("Item {0}: done submenu", ii);
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

            private static string GetCommandString(Shell32.IContextMenu cMenu, uint offset, Shell32.GCS flags = Shell32.GCS.GCS_VERBW)
            {
                SafeCoTaskMemString commandString = null;
                try
                {
                    commandString = new SafeCoTaskMemString(512);
                    cMenu.GetCommandString(new IntPtr(offset), flags, IntPtr.Zero, commandString, (uint)commandString.Capacity - 1);
                    Debug.WriteLine("Verb {0}: {1}", offset, commandString);
                    return commandString.ToString();
                }
                catch (Exception ex) when (ex is InvalidCastException || ex is ArgumentException)
                {
                    // TODO: investigate this..
                    Debug.WriteLine(ex);
                    return null;
                }
                catch (Exception ex) when (ex is COMException || ex is NotImplementedException)
                {
                    // Not every item has an associated verb
                    return null;
                }
                finally
                {
                    commandString?.Dispose();
                }
            }

            private static Bitmap GetItemBitmap(HBITMAP hbitmap)
            {
                var bitmap = GetTransparentBitmap(hbitmap);
                bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
                return bitmap;
            }

            private static Bitmap GetTransparentBitmap(HBITMAP hbitmap)
            {
                try
                {
                    var dibsection = GetObject<BITMAP>(hbitmap);
                    var bitmap = new Bitmap(dibsection.bmWidth, dibsection.bmHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using var mstr = new NativeMemoryStream(dibsection.bmBits, dibsection.bmBitsPixel * dibsection.bmHeight * dibsection.bmWidth);
                    for (var x = 0; x < dibsection.bmWidth; x++)
                        for (var y = 0; y < dibsection.bmHeight; y++)
                        {
                            var rgbquad = mstr.Read<RGBQUAD>();
                            if (rgbquad.rgbReserved != 0)
                                bitmap.SetPixel(x, y, rgbquad.Color);
                        }
                    return bitmap;
                }
                catch { }
                return Image.FromHbitmap((IntPtr)hbitmap);
            }

            #region IDisposable Support

            private bool disposedValue = false; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: dispose managed state (managed objects).
                        if (Items != null)
                        {
                            foreach (var si in Items)
                                (si as IDisposable)?.Dispose();
                            Items = null;
                        }
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    if (cMenu != null)
                    {
                        Marshal.ReleaseComObject(cMenu);
                        cMenu = null;
                    }

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
        }

        public class ContextMenuItem : Win32ContextMenuItem, IDisposable
        {
            private Bitmap _Icon;

            [JsonIgnore]
            public Bitmap Icon
            {
                get
                {
                    return _Icon;
                }
                set
                {
                    _Icon = value;
                    byte[] bitmapData = (byte[])new ImageConverter().ConvertTo(value, typeof(byte[]));
                    IconBase64 = Convert.ToBase64String(bitmapData, 0, bitmapData.Length);
                }
            }

            public ContextMenuItem()
            {
                this.SubItems = new List<Win32ContextMenuItem>();
            }

            public void Dispose()
            {
                Icon?.Dispose();
                if (SubItems != null)
                {
                    foreach (var si in SubItems)
                        (si as IDisposable)?.Dispose();
                    SubItems = null;
                }
            }
        }
    }

    // There is usually no need to define Win32 COM interfaces/P-Invoke methods here.
    // The Vanara library contains the definitions for all members of Shell32.dll, User32.dll and more
    // The ones below are due to bugs in the current version of the library and can be removed once fixed
}