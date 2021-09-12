using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;

namespace CustomOpenDialog
{
    [ComVisible(true)]  // This is mandatory.
    [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
    [SuppressUnmanagedCodeSecurity]
    [ClassInterface(ClassInterfaceType.None)]
    public class FilesOpenDialog : IFileOpenDialog, IFileDialogCustomize, IFileDialog, IFileDialog2, IModalWindow, ICustomQueryInterface
    {
        private FILEOPENDIALOGOPTIONS _fos = FILEOPENDIALOGOPTIONS.FOS_FILEMUSTEXIST | FILEOPENDIALOGOPTIONS.FOS_PATHMUSTEXIST;
        private string[] _selectedItems = null;

        private string debugPath = null;
        private string initFolder = null;
        private string outputPath = null;

        public FilesOpenDialog()
        {
            debugPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "open_dialog.txt");
            outputPath = Path.GetTempFileName();
            initFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            File.WriteAllText(debugPath, "Create\n");
        }

        public HRESULT Show([Optional] HWND parent)
        {
            File.AppendAllText(debugPath, $"Show, parent:{parent}\n");
            using var fproc = Process.Start("files.exe", $"-directory {initFolder} -outputpath {outputPath}");
            fproc.WaitForExit();
            if (parent != null)
            {
                User32.SetForegroundWindow(parent);
            }
            _selectedItems = File.ReadAllLines(outputPath);
            File.Delete(outputPath);
            return _selectedItems.Any() ? HRESULT.S_OK : HRESULT.HRESULT_FROM_WIN32(Win32Error.ERROR_CANCELLED);
        }

        public void SetFileTypes(uint cFileTypes, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] COMDLG_FILTERSPEC[] rgFilterSpec)
        {
            File.AppendAllText(debugPath, $"SetFileTypes, cFileTypes:{cFileTypes}\n");
            foreach (var type in rgFilterSpec)
            {
                File.AppendAllText(debugPath, $"SetFileTypes, filter:{type.pszName}|{type.pszSpec}\n");
            }
        }

        public void SetFileTypeIndex(uint iFileType)
        {
            File.AppendAllText(debugPath, $"SetFileTypeIndex, cFileTypes:{iFileType}\n");
        }

        public uint GetFileTypeIndex()
        {
            File.AppendAllText(debugPath, $"GetFileTypeIndex\n");
            return 0;
        }

        public uint Advise(IFileDialogEvents pfde)
        {
            File.AppendAllText(debugPath, $"Advise\n");
            return 0;
        }

        public void Unadvise(uint dwCookie)
        {
            File.AppendAllText(debugPath, $"Unadvise, dwCookie:{dwCookie}\n");
        }

        public void SetOptions(FILEOPENDIALOGOPTIONS fos)
        {
            File.AppendAllText(debugPath, $"SetOptions, fos:{fos}\n");
            _fos = fos;
        }

        public FILEOPENDIALOGOPTIONS GetOptions()
        {
            File.AppendAllText(debugPath, $"GetOptions, fos:{_fos}\n");
            return _fos;
        }

        public void SetDefaultFolder(IShellItem psi)
        {
            File.AppendAllText(debugPath, $"SetDefaultFolder, psi:{psi?.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY)}\n");
            if (psi?.GetAttributes(SFGAO.SFGAO_FILESYSTEM) == SFGAO.SFGAO_FILESYSTEM)
            {
                initFolder = psi?.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
            }
        }

        public void SetFolder(IShellItem psi)
        {
            File.AppendAllText(debugPath, $"SetFolder, psi:{psi?.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY)}\n");
            if (psi?.GetAttributes(SFGAO.SFGAO_FILESYSTEM) == SFGAO.SFGAO_FILESYSTEM)
            {
                initFolder = psi?.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
            }
        }

        public IShellItem GetFolder()
        {
            File.AppendAllText(debugPath, $"GetFolder\n");
            return Shell32.SHCreateItemFromParsingName<IShellItem>(Path.GetDirectoryName(debugPath));
        }

        public IShellItem GetCurrentSelection()
        {
            File.AppendAllText(debugPath, $"GetCurrentSelection\n");
            return null;
        }

        public void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName)
        {
            File.AppendAllText(debugPath, $"SetFileName: {pszName}\n");
        }

        [return: MarshalAs(UnmanagedType.LPWStr)]
        public string GetFileName()
        {
            File.AppendAllText(debugPath, $"GetFileName\n");
            return "";
        }

        public void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle)
        {
            File.AppendAllText(debugPath, $"SetTitle: {pszTitle}\n");
        }

        public void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText)
        {
            File.AppendAllText(debugPath, $"SetOkButtonLabel: {pszText}\n");
        }

        public void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel)
        {
            File.AppendAllText(debugPath, $"SetFileNameLabel: {pszLabel}\n");
        }

        public IShellItem GetResult()
        {
            File.AppendAllText(debugPath, $"GetResult\n");
            if (_selectedItems.Any())
            {
                return Shell32.SHCreateItemFromParsingName<IShellItem>(_selectedItems[0]);
            }
            return null;
        }

        public void AddPlace(IShellItem psi, FDAP fdap)
        {
            File.AppendAllText(debugPath, $"AddPlace, psi:{psi?.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY)}, fdap:{fdap}\n");
        }

        public void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension)
        {
            File.AppendAllText(debugPath, $"SetDefaultExtension, pszDefaultExtension:{pszDefaultExtension}\n");
        }

        public void Close([MarshalAs(UnmanagedType.Error)] HRESULT hr)
        {
            File.AppendAllText(debugPath, $"Close, hr:{hr}\n");
        }

        public void SetClientGuid(in Guid guid)
        {
            File.AppendAllText(debugPath, $"SetClientGuid, guid:{guid}\n");
        }

        public void ClearClientData()
        {
            File.AppendAllText(debugPath, $"ClearClientData\n");
        }

        public void SetFilter([MarshalAs(UnmanagedType.Interface)] object pFilter)
        {
            File.AppendAllText(debugPath, $"SetFilter, pFilter:{pFilter}\n");
        }

        public IShellItemArray GetResults()
        {
            File.AppendAllText(debugPath, $"GetResults\n");
            if (_selectedItems == null || !_selectedItems.Any())
            {
                Shell32.SHCreateShellItemArray(null, null, 0, new IntPtr[0], out var eshia);
                return eshia;
            }
            var selectedShellItem = _selectedItems.Select(x => GetItemPIDL(Shell32.SHCreateItemFromParsingName<IShellItem>(x)).DangerousGetHandle()).ToArray();
            Shell32.SHCreateShellItemArrayFromIDLists((uint)selectedShellItem.Length, selectedShellItem, out var shia);
            return shia;
        }

        private PIDL GetItemPIDL(IShellItem item)
        {
            if (Shell32.SH­Get­ID­List­From­Object(item, out var ppidl).Succeeded)
            {
                return ppidl;
            }
            return null;
        }

        public IShellItemArray GetSelectedItems()
        {
            File.AppendAllText(debugPath, $"GetSelectedItems\n");
            Shell32.SHCreateShellItemArray(null, null, 0, new IntPtr[0], out var shia);
            return shia;
        }

        public void SetCancelButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel)
        {
            File.AppendAllText(debugPath, $"SetCancelButtonLabel\n");
        }

        public void SetNavigationRoot([MarshalAs(UnmanagedType.Interface)] IShellItem psi)
        {
            File.AppendAllText(debugPath, $"SetNavigationRoot, psi:{psi?.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY)}\n");
        }

        public CustomQueryInterfaceResult GetInterface(ref Guid iid, out IntPtr ppv)
        {
            File.AppendAllText(debugPath, $"GetInterface, iid:{iid.ToString()}\n");
            ppv = IntPtr.Zero;
            return CustomQueryInterfaceResult.NotHandled;
        }

        public void EnableOpenDropDown(uint dwIDCtl)
        {
            File.AppendAllText(debugPath, $"EnableOpenDropDown, uid:{dwIDCtl}\n");
        }

        public void AddMenu(uint dwIDCtl, string pszLabel)
        {
            File.AppendAllText(debugPath, $"AddMenu, uid:{dwIDCtl}, label:{pszLabel}\n");
        }

        public void AddPushButton(uint dwIDCtl, string pszLabel)
        {
            File.AppendAllText(debugPath, $"AddPushButton, uid:{dwIDCtl}, label:{pszLabel}\n");
        }

        public void AddComboBox(uint dwIDCtl)
        {
            File.AppendAllText(debugPath, $"AddComboBox, uid:{dwIDCtl}\n");
        }

        public void AddRadioButtonList(uint dwIDCtl)
        {
            File.AppendAllText(debugPath, $"AddRadioButtonList, uid:{dwIDCtl}\n");
        }

        public void AddCheckButton(uint dwIDCtl, string pszLabel, bool bChecked)
        {
            File.AppendAllText(debugPath, $"AddCheckButton, uid:{dwIDCtl}, label:{pszLabel}, checked:{bChecked}\n");
        }

        public void AddEditBox(uint dwIDCtl, string pszText)
        {
            File.AppendAllText(debugPath, $"AddEditBox, uid:{dwIDCtl}, text:{pszText}\n");
        }

        public void AddSeparator(uint dwIDCtl)
        {
            File.AppendAllText(debugPath, $"AddSeparator, uid:{dwIDCtl}\n");
        }

        public void AddText(uint dwIDCtl, string pszText)
        {
            File.AppendAllText(debugPath, $"AddText, uid:{dwIDCtl}, text:{pszText}\n");
        }

        public void SetControlLabel(uint dwIDCtl, string pszLabel)
        {
            File.AppendAllText(debugPath, $"SetControlLabel, uid:{dwIDCtl}, text:{pszLabel}\n");
        }

        public CDCONTROLSTATEF GetControlState(uint dwIDCtl)
        {
            return CDCONTROLSTATEF.CDCS_ENABLEDVISIBLE;
        }

        public void SetControlState(uint dwIDCtl, CDCONTROLSTATEF dwState)
        {
            File.AppendAllText(debugPath, $"SetControlState, uid:{dwIDCtl}, state:{dwState}\n");
        }

        public void GetEditBoxText(uint dwIDCtl, out string ppszText)
        {
            File.AppendAllText(debugPath, $"GetEditBoxText, uid:{dwIDCtl}\n");
            ppszText = "";
        }

        public void SetEditBoxText(uint dwIDCtl, string pszText)
        {
            File.AppendAllText(debugPath, $"SetEditBoxText, uid:{dwIDCtl}, text:{pszText}\n");
        }

        public bool GetCheckButtonState(uint dwIDCtl)
        {
            File.AppendAllText(debugPath, $"GetCheckButtonState, uid:{dwIDCtl}\n");
            return false;
        }

        public void SetCheckButtonState(uint dwIDCtl, bool bChecked)
        {
            File.AppendAllText(debugPath, $"SetCheckButtonState, uid:{dwIDCtl}, checked:{bChecked}\n");
        }

        public void AddControlItem(uint dwIDCtl, int dwIDItem, string pszLabel)
        {
            File.AppendAllText(debugPath, $"AddControlItem, uid:{dwIDCtl}, item:{dwIDItem}, label:{pszLabel}\n");
        }

        public void RemoveControlItem(uint dwIDCtl, int dwIDItem)
        {
            File.AppendAllText(debugPath, $"RemoveControlItem, uid:{dwIDCtl}, item:{dwIDItem}\n");
        }

        public void RemoveAllControlItems(uint dwIDCtl)
        {
            File.AppendAllText(debugPath, $"RemoveAllControlItems, uid:{dwIDCtl}\n");
        }

        public CDCONTROLSTATEF GetControlItemState(uint dwIDCtl, int dwIDItem)
        {
            File.AppendAllText(debugPath, $"GetControlItemState, uid:{dwIDCtl}, item:{dwIDItem}\n");
            return CDCONTROLSTATEF.CDCS_ENABLEDVISIBLE;
        }

        public void SetControlItemState(uint dwIDCtl, int dwIDItem, CDCONTROLSTATEF dwState)
        {
            File.AppendAllText(debugPath, $"SetControlItemState, uid:{dwIDCtl}, item:{dwIDItem}, state:{dwState}\n");
        }

        public uint GetSelectedControlItem(uint dwIDCtl)
        {
            File.AppendAllText(debugPath, $"GetSelectedControlItem, uid:{dwIDCtl}\n");
            return 0;
        }

        public void SetSelectedControlItem(uint dwIDCtl, int dwIDItem)
        {
            File.AppendAllText(debugPath, $"SetSelectedControlItem, uid:{dwIDCtl}, item:{dwIDItem}\n");
        }

        public void StartVisualGroup(uint dwIDCtl, string pszLabel)
        {
            File.AppendAllText(debugPath, $"StartVisualGroup, uid:{dwIDCtl}, label:{pszLabel}\n");
        }

        public void EndVisualGroup()
        {
            File.AppendAllText(debugPath, $"EndVisualGroup\n");
        }

        public void MakeProminent(uint dwIDCtl)
        {
            File.AppendAllText(debugPath, $"MakeProminent, uid:{dwIDCtl}\n");
        }
    }
}
