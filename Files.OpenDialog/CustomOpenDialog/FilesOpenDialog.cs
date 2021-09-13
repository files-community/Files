//#define SYSTEMDIALOG

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
    public class FileDialogEvents : IFileDialogEvents
    {
        private IFileDialogEvents pfde;
        private string debugPath;

        public FileDialogEvents(IFileDialogEvents pfde, string debugPath)
        {
            this.pfde = pfde;
            this.debugPath = debugPath;
        }

        public int OnFileOk(IFileDialog pfd)
        {
            File.AppendAllText(debugPath, $"OnFileOk\n");
            return pfde.OnFileOk(pfd);
        }

        public int OnFolderChanging(IFileDialog pfd, IShellItem psiFolder)
        {
            File.AppendAllText(debugPath, $"OnFolderChanging, psiFolder:{psiFolder?.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY)}\n");
            return pfde.OnFolderChanging(pfd, psiFolder);
        }

        public int OnFolderChange(IFileDialog pfd)
        {
            File.AppendAllText(debugPath, $"OnFolderChange\n");
            return pfde.OnFolderChange(pfd);
        }

        public int OnSelectionChange(IFileDialog pfd)
        {
            File.AppendAllText(debugPath, $"OnSelectionChange\n");
            return pfde.OnSelectionChange(pfd);
        }

        public int OnShareViolation(IFileDialog pfd, IShellItem psi, out FDE_SHAREVIOLATION_RESPONSE pResponse)
        {
            File.AppendAllText(debugPath, $"OnShareViolation, psi:{psi?.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY)}\n");
            var res = pfde.OnShareViolation(pfd, psi, out var pResponseT);
            pResponse = pResponseT;
            return res;
        }

        public int OnTypeChange(IFileDialog pfd)
        {
            File.AppendAllText(debugPath, $"OnTypeChange\n");
            return pfde.OnTypeChange(pfd);
        }

        public int OnOverwrite(IFileDialog pfd, IShellItem psi, out FDE_SHAREVIOLATION_RESPONSE pResponse)
        {
            File.AppendAllText(debugPath, $"OnOverwrite, psi:{psi?.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY)}\n");
            var res = pfde.OnOverwrite(pfd, psi, out var pResponseT);
            pResponse = pResponseT;
            return res;
        }
    }

    [ComVisible(true)]
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
#if SYSTEMDIALOG
        private IFileOpenDialog systemDialog = null;
#endif
        private FileDialogEvents dialogEvents = null;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate uint DllGetClassObjectDelegate(
            [MarshalAs(UnmanagedType.LPStruct)]
            Guid rclsid,
            [MarshalAs(UnmanagedType.LPStruct)]
            Guid riid,
            [MarshalAs(UnmanagedType.IUnknown, IidParameterIndex=1)]
            out object ppv
        );

        private IFileOpenDialog GetSystemDialog()
        {
            var lib = Ole32.CoLoadLibrary(@"C:\Windows\System32\comdlg32.dll");
            var mDllGetClassObject = Kernel32.GetProcAddress(lib, "DllGetClassObject");
            var dllGetClassObject = (DllGetClassObjectDelegate)Marshal.GetDelegateForFunctionPointer(mDllGetClassObject, typeof(DllGetClassObjectDelegate));
            dllGetClassObject(typeof(FilesOpenDialog).GUID, typeof(Ole32.IClassFactory).GUID, out var ppv);
            ((Ole32.IClassFactory)ppv).CreateInstance(null, typeof(IFileOpenDialog).GUID, out var ppvObject);
            Ole32.CoFreeLibrary(lib);
            return (IFileOpenDialog)ppvObject;
        }

        public FilesOpenDialog()
        {
            debugPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "open_dialog.txt");
            outputPath = Path.GetTempFileName();
            initFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            File.WriteAllText(debugPath, "Create\n");
#if SYSTEMDIALOG
            systemDialog = GetSystemDialog();
#endif
        }

        public HRESULT Show([Optional] HWND parent)
        {
#if SYSTEMDIALOG
            return systemDialog.Show();
#else
            File.AppendAllText(debugPath, $"Show, parent:{parent}\n");
            using var fproc = Process.Start("files.exe", $"-directory {initFolder} -outputpath {outputPath}");
            fproc.WaitForExit();
            if (parent != null)
            {
                User32.SetForegroundWindow(parent);
            }
            _selectedItems = File.ReadAllLines(outputPath);
            File.Delete(outputPath);
            if (_selectedItems.Any())
            {
                dialogEvents?.OnFileOk(this);
            }
            return _selectedItems.Any() ? HRESULT.S_OK : HRESULT.HRESULT_FROM_WIN32(Win32Error.ERROR_CANCELLED);
#endif
        }

        public void SetFileTypes(uint cFileTypes, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] COMDLG_FILTERSPEC[] rgFilterSpec)
        {
            File.AppendAllText(debugPath, $"SetFileTypes, cFileTypes:{cFileTypes}\n");
            foreach (var type in rgFilterSpec)
            {
                File.AppendAllText(debugPath, $"SetFileTypes, filter:{type.pszName}|{type.pszSpec}\n");
            }
#if SYSTEMDIALOG
            systemDialog.SetFileTypes(cFileTypes, rgFilterSpec);
#endif
        }

        public void SetFileTypeIndex(uint iFileType)
        {
            File.AppendAllText(debugPath, $"SetFileTypeIndex, cFileTypes:{iFileType}\n");
#if SYSTEMDIALOG
            systemDialog.SetFileTypeIndex(iFileType);
#endif
        }

        public uint GetFileTypeIndex()
        {
            File.AppendAllText(debugPath, $"GetFileTypeIndex\n");
#if SYSTEMDIALOG
            return systemDialog.GetFileTypeIndex();
#else
            return 1;
#endif
        }

        public uint Advise(IFileDialogEvents pfde)
        {
            File.AppendAllText(debugPath, $"Advise\n");
            dialogEvents = new FileDialogEvents(pfde, debugPath);
#if SYSTEMDIALOG
            return systemDialog.Advise(dialogEvents);
#else
            return 0;
#endif
        }

        public void Unadvise(uint dwCookie)
        {
            File.AppendAllText(debugPath, $"Unadvise, dwCookie:{dwCookie}\n");
#if SYSTEMDIALOG
            systemDialog.Unadvise(dwCookie);
#endif
        }

        public void SetOptions(FILEOPENDIALOGOPTIONS fos)
        {
            File.AppendAllText(debugPath, $"SetOptions, fos:{fos}\n");
            _fos = fos;
#if SYSTEMDIALOG
            systemDialog.SetOptions(fos);
#endif
        }

        public FILEOPENDIALOGOPTIONS GetOptions()
        {
            File.AppendAllText(debugPath, $"GetOptions, fos:{_fos}\n");
#if SYSTEMDIALOG
            return systemDialog.GetOptions();
#else
            return _fos;
#endif
        }

        public void SetDefaultFolder(IShellItem psi)
        {
            File.AppendAllText(debugPath, $"SetDefaultFolder, psi:{psi?.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY)}\n");
            if (psi?.GetAttributes(SFGAO.SFGAO_FILESYSTEM) == SFGAO.SFGAO_FILESYSTEM)
            {
                initFolder = psi?.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
            }
#if SYSTEMDIALOG
            systemDialog.SetDefaultFolder(psi);
#endif
        }

        public void SetFolder(IShellItem psi)
        {
            File.AppendAllText(debugPath, $"SetFolder, psi:{psi?.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY)}\n");
            if (psi?.GetAttributes(SFGAO.SFGAO_FILESYSTEM) == SFGAO.SFGAO_FILESYSTEM)
            {
                initFolder = psi?.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
            }
#if SYSTEMDIALOG
            systemDialog.SetFolder(psi);
#endif
        }

        public IShellItem GetFolder()
        {
            File.AppendAllText(debugPath, $"GetFolder\n");
#if SYSTEMDIALOG
            return systemDialog.GetFolder();
#else
            var folderPath = _selectedItems.Any() ? Path.GetDirectoryName(_selectedItems[0]) : initFolder;
            return Shell32.SHCreateItemFromParsingName<IShellItem>(folderPath);
#endif
        }

        public IShellItem GetCurrentSelection()
        {
            File.AppendAllText(debugPath, $"GetCurrentSelection\n");
#if SYSTEMDIALOG
            return systemDialog.GetCurrentSelection();
#else
            return _selectedItems.Any() ? Shell32.SHCreateItemFromParsingName<IShellItem>(_selectedItems[0]) : null;
#endif
        }

        public void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName)
        {
            File.AppendAllText(debugPath, $"SetFileName: {pszName}\n");
#if SYSTEMDIALOG
            systemDialog.SetFileName(pszName);
#endif
        }

        [return: MarshalAs(UnmanagedType.LPWStr)]
        public string GetFileName()
        {
            File.AppendAllText(debugPath, $"GetFileName\n");
#if SYSTEMDIALOG
            return systemDialog.GetFileName();
#else
            return _selectedItems.Any()? Path.GetFileName(_selectedItems[0]) : "";
#endif
        }

        public void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle)
        {
            File.AppendAllText(debugPath, $"SetTitle: {pszTitle}\n");
#if SYSTEMDIALOG
            systemDialog.SetTitle(pszTitle);
#endif
        }

        public void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText)
        {
            File.AppendAllText(debugPath, $"SetOkButtonLabel: {pszText}\n");
#if SYSTEMDIALOG
            systemDialog.SetOkButtonLabel(pszText);
#endif
        }

        public void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel)
        {
            File.AppendAllText(debugPath, $"SetFileNameLabel: {pszLabel}\n");
#if SYSTEMDIALOG
            systemDialog.SetFileNameLabel(pszLabel);
#endif
        }

        public IShellItem GetResult()
        {
            File.AppendAllText(debugPath, $"GetResult\n");
#if SYSTEMDIALOG
            return systemDialog.GetResult();
#else
            if (_selectedItems.Any())
            {
                return Shell32.SHCreateItemFromParsingName<IShellItem>(_selectedItems[0]);
            }
            return null;
#endif
        }

        public void AddPlace(IShellItem psi, FDAP fdap)
        {
            File.AppendAllText(debugPath, $"AddPlace, psi:{psi?.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY)}, fdap:{fdap}\n");
#if SYSTEMDIALOG
            systemDialog.AddPlace(psi, fdap);
#endif
        }

        public void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension)
        {
            File.AppendAllText(debugPath, $"SetDefaultExtension, pszDefaultExtension:{pszDefaultExtension}\n");
#if SYSTEMDIALOG
            systemDialog.SetDefaultExtension(pszDefaultExtension);
#endif
        }

        public void Close([MarshalAs(UnmanagedType.Error)] HRESULT hr)
        {
            File.AppendAllText(debugPath, $"Close, hr:{hr}\n");
#if SYSTEMDIALOG
            systemDialog.Close(hr);
#endif
        }

        public void SetClientGuid(in Guid guid)
        {
            File.AppendAllText(debugPath, $"SetClientGuid, guid:{guid}\n");
#if SYSTEMDIALOG
            systemDialog.SetClientGuid(guid);
#endif
        }

        public void ClearClientData()
        {
            File.AppendAllText(debugPath, $"ClearClientData\n");
#if SYSTEMDIALOG
            systemDialog.ClearClientData();
#endif
        }

        public void SetFilter([MarshalAs(UnmanagedType.Interface)] object pFilter)
        {
            File.AppendAllText(debugPath, $"SetFilter, pFilter:{pFilter}\n");
#if SYSTEMDIALOG
            systemDialog.SetFilter(pFilter);
#endif
        }

        public IShellItemArray GetResults()
        {
            File.AppendAllText(debugPath, $"GetResults\n");
#if SYSTEMDIALOG
            return systemDialog.GetResults();
#else
            if (_selectedItems == null || !_selectedItems.Any())
            {
                Shell32.SHCreateShellItemArray(null, null, 0, new IntPtr[0], out var eshia);
                return eshia;
            }
            var selectedShellItem = _selectedItems.Select(x => GetItemPIDL(Shell32.SHCreateItemFromParsingName<IShellItem>(x)).DangerousGetHandle()).ToArray();
            Shell32.SHCreateShellItemArrayFromIDLists((uint)selectedShellItem.Length, selectedShellItem, out var shia);
            return shia;
#endif
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
#if SYSTEMDIALOG
            return systemDialog.GetSelectedItems();
#else
            Shell32.SHCreateShellItemArray(null, null, 0, new IntPtr[0], out var shia);
            return shia;
#endif
        }

        public void SetCancelButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel)
        {
            File.AppendAllText(debugPath, $"SetCancelButtonLabel\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialog2).SetCancelButtonLabel(pszLabel);
#endif
        }

        public void SetNavigationRoot([MarshalAs(UnmanagedType.Interface)] IShellItem psi)
        {
            File.AppendAllText(debugPath, $"SetNavigationRoot, psi:{psi?.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY)}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialog2).SetNavigationRoot(psi);
#endif
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
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).EnableOpenDropDown(dwIDCtl);
#endif
        }

        public void AddMenu(uint dwIDCtl, string pszLabel)
        {
            File.AppendAllText(debugPath, $"AddMenu, uid:{dwIDCtl}, label:{pszLabel}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).AddMenu(dwIDCtl, pszLabel);
#endif
        }

        public void AddPushButton(uint dwIDCtl, string pszLabel)
        {
            File.AppendAllText(debugPath, $"AddPushButton, uid:{dwIDCtl}, label:{pszLabel}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).AddPushButton(dwIDCtl, pszLabel);
#endif
        }

        public void AddComboBox(uint dwIDCtl)
        {
            File.AppendAllText(debugPath, $"AddComboBox, uid:{dwIDCtl}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).AddComboBox(dwIDCtl);
#endif
        }

        public void AddRadioButtonList(uint dwIDCtl)
        {
            File.AppendAllText(debugPath, $"AddRadioButtonList, uid:{dwIDCtl}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).AddRadioButtonList(dwIDCtl);
#endif
        }

        public void AddCheckButton(uint dwIDCtl, string pszLabel, bool bChecked)
        {
            File.AppendAllText(debugPath, $"AddCheckButton, uid:{dwIDCtl}, label:{pszLabel}, checked:{bChecked}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).AddCheckButton(dwIDCtl, pszLabel, bChecked);
#endif
        }

        public void AddEditBox(uint dwIDCtl, string pszText)
        {
            File.AppendAllText(debugPath, $"AddEditBox, uid:{dwIDCtl}, text:{pszText}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).AddEditBox(dwIDCtl, pszText);
#endif
        }

        public void AddSeparator(uint dwIDCtl)
        {
            File.AppendAllText(debugPath, $"AddSeparator, uid:{dwIDCtl}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).AddSeparator(dwIDCtl);
#endif
        }

        public void AddText(uint dwIDCtl, string pszText)
        {
            File.AppendAllText(debugPath, $"AddText, uid:{dwIDCtl}, text:{pszText}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).AddText(dwIDCtl, pszText);
#endif
        }

        public void SetControlLabel(uint dwIDCtl, string pszLabel)
        {
            File.AppendAllText(debugPath, $"SetControlLabel, uid:{dwIDCtl}, text:{pszLabel}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).SetControlLabel(dwIDCtl, pszLabel);
#endif
        }

        public CDCONTROLSTATEF GetControlState(uint dwIDCtl)
        {
#if SYSTEMDIALOG
            return (systemDialog as IFileDialogCustomize).GetControlState(dwIDCtl);
#else
            return CDCONTROLSTATEF.CDCS_ENABLEDVISIBLE;
#endif
        }

        public void SetControlState(uint dwIDCtl, CDCONTROLSTATEF dwState)
        {
            File.AppendAllText(debugPath, $"SetControlState, uid:{dwIDCtl}, state:{dwState}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).SetControlState(dwIDCtl, dwState);
#endif
        }

        public void GetEditBoxText(uint dwIDCtl, out string ppszText)
        {
            File.AppendAllText(debugPath, $"GetEditBoxText, uid:{dwIDCtl}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).GetEditBoxText(dwIDCtl, out var temp);
            ppszText = temp;
#else
            ppszText = "";
#endif
        }

        public void SetEditBoxText(uint dwIDCtl, string pszText)
        {
            File.AppendAllText(debugPath, $"SetEditBoxText, uid:{dwIDCtl}, text:{pszText}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).SetEditBoxText(dwIDCtl, pszText);
#endif
        }

        public bool GetCheckButtonState(uint dwIDCtl)
        {
            File.AppendAllText(debugPath, $"GetCheckButtonState, uid:{dwIDCtl}\n");
#if SYSTEMDIALOG
            return (systemDialog as IFileDialogCustomize).GetCheckButtonState(dwIDCtl);
#else
            return false;
#endif
        }

        public void SetCheckButtonState(uint dwIDCtl, bool bChecked)
        {
            File.AppendAllText(debugPath, $"SetCheckButtonState, uid:{dwIDCtl}, checked:{bChecked}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).SetCheckButtonState(dwIDCtl, bChecked);
#endif
        }

        public void AddControlItem(uint dwIDCtl, int dwIDItem, string pszLabel)
        {
            File.AppendAllText(debugPath, $"AddControlItem, uid:{dwIDCtl}, item:{dwIDItem}, label:{pszLabel}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).AddControlItem(dwIDCtl, dwIDItem, pszLabel);
#endif
        }

        public void RemoveControlItem(uint dwIDCtl, int dwIDItem)
        {
            File.AppendAllText(debugPath, $"RemoveControlItem, uid:{dwIDCtl}, item:{dwIDItem}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).RemoveControlItem(dwIDCtl, dwIDItem);
#endif
        }

        public void RemoveAllControlItems(uint dwIDCtl)
        {
            File.AppendAllText(debugPath, $"RemoveAllControlItems, uid:{dwIDCtl}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).RemoveAllControlItems(dwIDCtl);
#endif
        }

        public CDCONTROLSTATEF GetControlItemState(uint dwIDCtl, int dwIDItem)
        {
            File.AppendAllText(debugPath, $"GetControlItemState, uid:{dwIDCtl}, item:{dwIDItem}\n");
#if SYSTEMDIALOG
            return (systemDialog as IFileDialogCustomize).GetControlItemState(dwIDCtl, dwIDItem);
#else
            return CDCONTROLSTATEF.CDCS_ENABLEDVISIBLE;
#endif
        }

        public void SetControlItemState(uint dwIDCtl, int dwIDItem, CDCONTROLSTATEF dwState)
        {
            File.AppendAllText(debugPath, $"SetControlItemState, uid:{dwIDCtl}, item:{dwIDItem}, state:{dwState}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).SetControlItemState(dwIDCtl, dwIDItem, dwState);
#endif
        }

        public uint GetSelectedControlItem(uint dwIDCtl)
        {
            File.AppendAllText(debugPath, $"GetSelectedControlItem, uid:{dwIDCtl}\n");
#if SYSTEMDIALOG
            return (systemDialog as IFileDialogCustomize).GetSelectedControlItem(dwIDCtl);
#else
            return 0;
#endif
        }

        public void SetSelectedControlItem(uint dwIDCtl, int dwIDItem)
        {
            File.AppendAllText(debugPath, $"SetSelectedControlItem, uid:{dwIDCtl}, item:{dwIDItem}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).SetSelectedControlItem(dwIDCtl, dwIDItem);
#endif
        }

        public void StartVisualGroup(uint dwIDCtl, string pszLabel)
        {
            File.AppendAllText(debugPath, $"StartVisualGroup, uid:{dwIDCtl}, label:{pszLabel}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).StartVisualGroup(dwIDCtl, pszLabel);
#endif
        }

        public void EndVisualGroup()
        {
            File.AppendAllText(debugPath, $"EndVisualGroup\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).EndVisualGroup();
#endif
        }

        public void MakeProminent(uint dwIDCtl)
        {
            File.AppendAllText(debugPath, $"MakeProminent, uid:{dwIDCtl}\n");
#if SYSTEMDIALOG
            (systemDialog as IFileDialogCustomize).MakeProminent(dwIDCtl);
#endif
        }
    }
}
