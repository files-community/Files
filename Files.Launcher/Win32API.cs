﻿using System;
using System.ComponentModel;
using System.Diagnostics;
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
            var lpResult = new StringBuilder();
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

        /// <summary>
        /// Gets file path from file FRN
        /// </summary>
        /// <param name="frn">File reference number</param>
        /// <param name="volumeHint">Drive containing the file (e.g. "C:\")</param>
        /// <returns>File path or null</returns>
        public static string PathFromFileId(ulong frn, string volumeHint)
        {
            string volumePath = Path.GetPathRoot(volumeHint);
            using var volumeHandle = Kernel32.CreateFile(volumePath, Kernel32.FileAccess.GENERIC_READ, FileShare.Read, null, FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS);
            if (volumeHandle.IsInvalid) return null;
            var fileId = new Kernel32.FILE_ID_DESCRIPTOR() { Type = 0, Id = new Kernel32.FILE_ID_DESCRIPTOR.DUMMYUNIONNAME() { FileId = (long)frn } };
            fileId.dwSize = (uint)Marshal.SizeOf(fileId);
            var tmp = Kernel32.OpenFileById(volumeHandle, fileId, Kernel32.FileAccess.GENERIC_READ, FileShare.Read, null, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS);
            using var hFile = new Kernel32.SafeHFILE(tmp);
            if (hFile.IsInvalid) return null;
            var sb = new StringBuilder(4096);
            var ret = Kernel32.GetFinalPathNameByHandle(hFile, sb, 4095, 0);
            return (ret != 0) ? sb.ToString() : null;
        }
    }

    // There is usually no need to define Win32 COM interfaces/P-Invoke methods here.
    // The Vanara library contains the definitions for all members of Shell32.dll, User32.dll and more
    // The ones below are due to bugs in the current version of the library and can be removed once fixed
}