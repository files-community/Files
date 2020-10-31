using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.System;

namespace FilesFullTrust
{
    internal partial class Win32API
    {
        public static Task<T> StartSTATask<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            Thread thread = new Thread(() =>
            {
                try
                {
                    tcs.SetResult(func());
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        public static async Task<string> GetFileAssociation(string filename)
        {
            // Find UWP apps
            var uwp_apps = await Launcher.FindFileHandlersAsync(Path.GetExtension(filename));
            if (uwp_apps.Any())
            {
                return uwp_apps.First().PackageFamilyName;
            }

            // Find desktop apps
            var lpResult = new StringBuilder(2048);
            var hResult = Shell32.FindExecutable(filename, null, lpResult);
            if (hResult.ToInt64() > 32)
            {
                return lpResult.ToString();
            }

            return null;
        }

        public static string ExtractStringFromDLL(string file, int number)
        {
            var lib = Kernel32.LoadLibrary(file);
            StringBuilder result = new StringBuilder(2048);
            User32.LoadString(lib, number, result, result.Capacity);
            Kernel32.FreeLibrary(lib);
            return result.ToString();
        }

        public static string[] CommandLineToArgs(string commandLine)
        {
            if (string.IsNullOrEmpty(commandLine))
            {
                return Array.Empty<string>();
            }

            var argv = Shell32.CommandLineToArgvW(commandLine, out int argc);
            if (argv == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            try
            {
                var args = new string[argc];
                for (var i = 0; i < args.Length; i++)
                {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }

                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }

        public static void UnlockBitlockerDrive(string drive, string password)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.Verb = "runas";
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.Arguments = $"-command \"$SecureString = ConvertTo-SecureString '{password}' -AsPlainText -Force; Unlock-BitLocker -MountPoint '{drive}' -Password $SecureString\"";
                process.Start();
                process.WaitForExit(30 * 1000);
            }
            catch (Win32Exception)
            {
                // If user cancels UAC
            }
        }

        public static (string icon, bool isCustom) GetFileOverlayIcon(string path)
        {
            var shfi = new Shell32.SHFILEINFO();
            var ret = Shell32.SHGetFileInfo(path, 0, ref shfi, Shell32.SHFILEINFO.Size, Shell32.SHGFI.SHGFI_OVERLAYINDEX | Shell32.SHGFI.SHGFI_ICON | Shell32.SHGFI.SHGFI_SYSICONINDEX | Shell32.SHGFI.SHGFI_ICONLOCATION);
            if (ret == IntPtr.Zero) return (null, false);
            bool isCustom = !shfi.szDisplayName.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
            User32.DestroyIcon(shfi.hIcon);
            Shell32.SHGetImageList(Shell32.SHIL.SHIL_LARGE, typeof(ComCtl32.IImageList).GUID, out var tmp);
            using var imageList = ComCtl32.SafeHIMAGELIST.FromIImageList(tmp);
            if (imageList.IsNull || imageList.IsInvalid) return (null, isCustom);
            var overlay_idx = shfi.iIcon >> 24;
            //var icon_idx = shfi.iIcon & 0xFFFFFF;
            if (overlay_idx == 0) return (null, isCustom);
            var overlay_image = imageList.Interface.GetOverlayImage(overlay_idx);
            using var hIcon = imageList.Interface.GetIcon(overlay_image, ComCtl32.IMAGELISTDRAWFLAGS.ILD_TRANSPARENT);
            if (hIcon.IsNull || hIcon.IsInvalid) return (null, isCustom);
            using var image = hIcon.ToIcon().ToBitmap();
            byte[] bitmapData = (byte[])new ImageConverter().ConvertTo(image, typeof(byte[]));
            return (Convert.ToBase64String(bitmapData, 0, bitmapData.Length), isCustom);
        }

        // There is usually no need to define Win32 COM interfaces/P-Invoke methods here.
        // The Vanara library contains the definitions for all members of Shell32.dll, User32.dll and more
        // The ones below are due to bugs in the current version of the library and can be removed once fixed
        // Structure used by SHQueryRecycleBin.
        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        // Get information from recycle bin.
        [DllImport(Lib.Shell32, SetLastError = false, CharSet = CharSet.Auto)]
        public static extern int SHQueryRecycleBin(string pszRootPath,
            ref SHQUERYRBINFO pSHQueryRBInfo);
    }
}