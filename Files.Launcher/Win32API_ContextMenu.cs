using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using Vanara.InteropServices;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static Vanara.PInvoke.Gdi32;

namespace FilesFullTrust
{
    internal partial class Win32API
    {
        // Shows context menu items, only for debug purpose
        public class ContextMenuDebuggerForm : System.Windows.Forms.Form
        {
            private ContextMenu menu;

            public ContextMenuDebuggerForm(ContextMenu menu)
            {
                this.menu = menu;
                this.Load += ContextMenuDebuggerForm_Load;
            }

            private void ContextMenuDebuggerForm_Load(object sender, EventArgs e)
            {
                AutoScroll = true;
                Text = this.GetType().Name;
                int offset = 0;
                RenderContextMenu(menu.Items, ref offset);
            }

            private void RenderContextMenu(List<ContextMenuItem> items, ref int offset, int level = 0)
            {
                foreach (var mi in items)
                {
                    if (mi.Type != User32.MenuItemType.MFT_STRING)
                        continue;
                    var label = new System.Windows.Forms.Label();
                    label.Text = mi.Label;
                    label.Location = new Point(level * 30 + 30, offset * 30);
                    label.Width = Width;
                    var image = new System.Windows.Forms.PictureBox();
                    image.Image = mi.Icon;
                    image.Location = new Point(level * 30, offset * 30);
                    image.Size = new Size(mi.Icon?.Width ?? 0, mi.Icon?.Height ?? 0);
                    this.Controls.Add(image);
                    this.Controls.Add(label);
                    offset = offset + 1;
                    RenderContextMenu(mi.SubItems, ref offset, level + 1);
                }
            }
        }

        public class ContextMenu : IDisposable
        {
            private IContextMenu cMenu;
            public List<ContextMenuItem> Items { get; set; }

            public ContextMenu(IContextMenu cMenu)
            {
                this.cMenu = cMenu;
                this.Items = new List<ContextMenuItem>();
            }

            public void InvokeVerb(string verb)
            {
                var pici = new CMINVOKECOMMANDINFOEX();
                pici.lpVerb = new SafeResourceId(verb, CharSet.Ansi);
                pici.nShow = ShowWindowCommand.SW_SHOWNORMAL;
                pici.cbSize = (uint)Marshal.SizeOf(pici);
                cMenu.InvokeCommand(pici);
            }

            #region FactoryMethods
            public static ContextMenu GetContextMenuForFiles(string filePath, Shell32.CMF flags)
            {
                return GetContextMenuForFiles(filePath.Split(';').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray(), flags);
            }

            public static ContextMenu GetContextMenuForFiles(string[] filePathList, Shell32.CMF flags)
            {
                List<ShellItem> shellItems = new List<ShellItem>();
                try
                {
                    foreach (var fp in filePathList)
                        shellItems.Add(new ShellItem(fp));
                    return GetContextMenuForFiles(shellItems.ToArray(), flags);
                }
                finally
                {
                    foreach (var si in shellItems)
                        si.Dispose();
                }
            }

            public static ContextMenu GetContextMenuForFiles(ShellItem[] shellItems, Shell32.CMF flags)
            {
                if (shellItems == null || !shellItems.Any())
                    return null;
                using var sf = shellItems.First().Parent; // TODO: what if the items aren't all in the same folder?
                IContextMenu menu = sf.GetChildrenUIObjects<IContextMenu>(null, shellItems);
                var contextMenu = new ContextMenu(menu);
                var hMenu = User32.CreatePopupMenu();                
                menu.QueryContextMenu(hMenu, 0, 1, 0x7FFF, flags);
                //User32.TrackPopupMenuEx(hMenu, User32.TrackPopupMenuFlags.TPM_RETURNCMD, 0, 0, wv.Handle);
                ContextMenu.EnumMenuItems(menu, hMenu, contextMenu.Items);
                User32.DestroyMenu(hMenu);
                return contextMenu;
            }
            #endregion

            private static void EnumMenuItems(IContextMenu cMenu, HMENU hMenu, List<ContextMenuItem> menuItemsResult)
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
                    var menuItem = new ContextMenuItem(cMenu);
                    var container = new SafeCoTaskMemString(512);
                    mii.dwTypeData = (IntPtr)container;
                    mii.cch = (uint)container.Capacity - 1; // https://devblogs.microsoft.com/oldnewthing/20040928-00/?p=37723
                    var retval = User32.GetMenuItemInfo(hMenu, ii, true, ref mii);
                    if (!retval)
                    {
                        container.Dispose();
                        continue;
                    }
                    menuItem.Type = mii.fType;
                    menuItem.ID = mii.wID - 1; // wID - idCmdFirst
                    if (mii.fType == User32.MenuItemType.MFT_STRING)
                    {
                        Debug.WriteLine("Item {0} ({1}): {2}", ii, mii.wID, mii.dwTypeData);
                        menuItem.Label = mii.dwTypeData;
                        menuItem.CommandString = GetCommandString(cMenu, mii.wID - 1);
                        if (mii.hbmpItem != HBITMAP.NULL)
                        {
                            var bitmap = GetTransparentBitmap(mii.hbmpItem);
                            menuItem.Icon = bitmap;
                        }
                        if (mii.hSubMenu != HMENU.NULL)
                        {
                            Debug.WriteLine("Item {0}: has submenu", ii);
                            var subItems = new List<ContextMenuItem>();
                            try
                            {
                                (cMenu as Shell32.IContextMenu2)?.HandleMenuMsg((uint)User32.WindowMessage.WM_INITMENUPOPUP, (IntPtr)mii.hSubMenu, new IntPtr(ii));
                            }
                            catch (NotImplementedException)
                            {
                                // Only for dynamic/owner drawn? (open with, etc)
                            }
                            EnumMenuItems(cMenu, mii.hSubMenu, subItems);
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

            private static string GetCommandString(IContextMenu cMenu, uint offset, Shell32.GCS flags = Shell32.GCS.GCS_VERBW)
            {
                SafeCoTaskMemString commandString = null;
                try
                {
                    commandString = new SafeCoTaskMemString(512);
                    cMenu.GetCommandString(new IntPtr(offset), flags, IntPtr.Zero, commandString, (uint)commandString.Capacity - 1);
                    Debug.WriteLine("Verb {0}: {1}", offset, commandString);
                    return commandString.ToString();
                }
                catch (InvalidCastException ex)
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

            private static Bitmap GetTransparentBitmap(HBITMAP hbitmap)
            {
                try
                {
                    var dibsection = GetObject<BITMAP>(hbitmap);
                    var bitmap = new Bitmap(dibsection.bmHeight, dibsection.bmWidth, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using var mstr = new NativeMemoryStream(dibsection.bmBits, dibsection.bmBitsPixel * dibsection.bmHeight * dibsection.bmWidth);
                    for (var x = 0; x < dibsection.bmWidth; x++)
                        for (var y = 0; y < dibsection.bmHeight; y++)
                        {
                            var rgbquad = mstr.Read<RGBQUAD>();
                            if (rgbquad.rgbReserved != 0)
                                bitmap.SetPixel(y, dibsection.bmWidth - 1 - x, rgbquad.Color);
                        }
                    return bitmap;
                }
                catch { }
                return Image.FromHbitmap((IntPtr)hbitmap);
            }

            #region IDisposable Support
            private bool disposedValue = false; // Per rilevare chiamate ridondanti

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: eliminare lo stato gestito (oggetti gestiti).
                        if (Items != null)
                        {
                            foreach (var si in Items)
                                si.Dispose();
                            Items = null;
                        }
                    }

                    // TODO: liberare risorse non gestite (oggetti non gestiti) ed eseguire sotto l'override di un finalizzatore.
                    if (cMenu != null)
                    {
                        Marshal.ReleaseComObject(cMenu);
                        cMenu = null;
                    }

                    disposedValue = true;
                }
            }

            // TODO: eseguire l'override di un finalizzatore solo se Dispose(bool disposing) include il codice per liberare risorse non gestite.
            ~ContextMenu()
            {
                // Non modificare questo codice. Inserire il codice di pulizia in Dispose(bool disposing) sopra.
                Dispose(false);
            }

            // Questo codice viene aggiunto per implementare in modo corretto il criterio Disposable.
            public void Dispose()
            {
                // Non modificare questo codice. Inserire il codice di pulizia in Dispose(bool disposing) sopra.
                Dispose(true);
                // TODO: rimuovere il commento dalla riga seguente se è stato eseguito l'override del finalizzatore.
                GC.SuppressFinalize(this);
            }
            #endregion
        }

        public class ContextMenuItem : IDisposable
        {
            public Bitmap Icon { get; set; }
            public uint ID { get; set; } // Valid only in current menu to invoke item
            public string Label { get; set; }
            public string CommandString { get; set; }
            public User32.MenuItemType Type { get; set; }
            public List<ContextMenuItem> SubItems { get; set; }

            private IContextMenu parentMenu;

            public ContextMenuItem(IContextMenu menu)
            {
                this.SubItems = new List<ContextMenuItem>();
                this.parentMenu = menu;
            }

            public void Invoke()
            {
                var pici = new CMINVOKECOMMANDINFOEX();
                pici.lpVerb = Macros.MAKEINTRESOURCE((int)ID);
                pici.nShow = ShowWindowCommand.SW_SHOWNORMAL;
                pici.cbSize = (uint)Marshal.SizeOf(pici);
                parentMenu?.InvokeCommand(pici);
            }

            public void Dispose()
            {
                Icon?.Dispose();
                if (SubItems != null)
                {
                    foreach (var si in SubItems)
                        si.Dispose();
                    SubItems = null;
                }
            }
        }
    }

    // There is usually no need to define Win32 COM interfaces/P-Invoke methods here.
    // The Vanara library contains the definitions for all members of Shell32.dll, User32.dll and more
    // The ones below are due to bugs in the current version of the library and can be removed once fixed
    #region WIN32_INTERFACES
    [PInvokeData("shobjidl_core.h", MSDNShortId = "c4c7f053-fdb1-4bba-9eb9-a514ce1d90f6")]
    [StructLayout(LayoutKind.Sequential)]
    public struct CMINVOKECOMMANDINFOEX
    {
        public uint cbSize;
        public Shell32.CMIC fMask;
        public HWND hwnd;
        //[MarshalAs(UnmanagedType.LPStr)]
        public ResourceId lpVerb;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpParameters;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpDirectory;
        public ShowWindowCommand nShow;
        public uint dwHotKey;
        public HICON hIcon;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpTitle;
        //[MarshalAs(UnmanagedType.LPWStr)]
        public ResourceId lpVerbW;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpParametersW;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpDirectoryW;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpTitleW;
        public Point ptInvoke;
    }

    [PInvokeData("Shobjidl.h", MSDNShortId = "bb776095")]
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214E4-0000-0000-c000-000000000046")]
    public interface IContextMenu
    {
        [PreserveSig]
        HRESULT QueryContextMenu(HMENU hmenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, Shell32.CMF uFlags);
        void InvokeCommand(in CMINVOKECOMMANDINFOEX pici);
        void GetCommandString(IntPtr idCmd, Shell32.GCS uType, IntPtr pReserved, IntPtr pszName, uint cchMax);
    }
    #endregion
}
