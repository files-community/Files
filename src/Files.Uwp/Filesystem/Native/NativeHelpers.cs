using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Shared;
using Files.Shared.Extensions;
using Files.Uwp.Helpers;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI.Core;
using static Files.Uwp.Filesystem.Native.NativeApi;
using static Files.Uwp.Filesystem.Native.NativeConstants;

namespace Files.Uwp.Filesystem.Native
{
    public static class NativeHelpers
    {
        private static readonly ILogger logger = Ioc.Default.GetService<ILogger>();

        public delegate void LpoverlappedCompletionRoutine(uint dwErrorCode, uint dwNumberOfBytesTransfered, OVERLAPPED lpOverlapped);
        public delegate void NativeLpoverlappedCompletionRoutine(uint dwErrorCode, uint dwNumberOfBytesTransfered, ref NativeOverlapped lpOverlapped);

        public static IntPtr CoreWindowHandle => ((ICoreWindowInterop)(object)CoreWindow.GetForCurrentThread()).WindowHandle;

        private static bool? isHasThreadAccessPropertyPresent = null;
        public static bool IsHasThreadAccessPropertyPresent
        {
            get
            {
                if (isHasThreadAccessPropertyPresent is null)
                {
                    isHasThreadAccessPropertyPresent = ApiInformation.IsPropertyPresent(typeof(DispatcherQueue).FullName, "HasThreadAccess");
                }
                return isHasThreadAccessPropertyPresent ?? false;
            }
        }

        // https://stackoverflow.com/questions/54456140/how-to-detect-were-running-under-the-arm64-version-of-windows-10-in-net
        // https://docs.microsoft.com/en-us/windows/win32/sysinfo/image-file-machine-constants
        private static bool? isRunningOnArm = null;
        public static bool IsRunningOnArm
        {
            get
            {
                if (isRunningOnArm is null)
                {
                    isRunningOnArm = IsArmProcessor();
                    logger?.Info("Running on ARM: {0}", isRunningOnArm);
                }
                return isRunningOnArm ?? false;
            }
        }

        public static SafeFileHandle CreateFileForWrite(string filePath, bool overwrite = true)
        {
            return new SafeFileHandle(CreateFileFromApp(
                filePath,
                GENERIC_WRITE,
                0,
                IntPtr.Zero,
                overwrite ? CREATE_ALWAYS : OPEN_ALWAYS,
                (uint)File_Attributes.BackupSemantics,
                IntPtr.Zero
            ), true);
        }

        public static SafeFileHandle OpenFileForRead(string filePath, bool readWrite = false, uint flags = 0)
        {
            return new SafeFileHandle(CreateFileFromApp(
                filePath,
                GENERIC_READ | (readWrite ? GENERIC_WRITE : 0),
                FILE_SHARE_READ | (readWrite ? 0 : FILE_SHARE_WRITE),
                IntPtr.Zero, OPEN_EXISTING,
                (uint)File_Attributes.BackupSemantics | flags, IntPtr.Zero),
                true
            );
        }

        public static bool GetFileDateModified(string filePath, out FILETIME dateModified)
        {
            using var hFile = new SafeFileHandle(CreateFileFromApp(
                filePath,
                GENERIC_READ,
                FILE_SHARE_READ,
                IntPtr.Zero,
                OPEN_EXISTING,
                (uint)File_Attributes.BackupSemantics,
                IntPtr.Zero
            ), true);
            return GetFileTime(hFile.DangerousGetHandle(), out _, out _, out dateModified);
        }

        public static bool SetFileDateModified(string filePath, FILETIME dateModified)
        {
            using var hFile = new SafeFileHandle(CreateFileFromApp(
                filePath,
                FILE_WRITE_ATTRIBUTES,
                0,
                IntPtr.Zero,
                OPEN_EXISTING,
                (uint)File_Attributes.BackupSemantics,
                IntPtr.Zero
            ), true);
            return SetFileTime(hFile.DangerousGetHandle(), new(), new(), dateModified);
        }

        public static async Task<string> GetFileAssociationAsync(string filePath)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection is not null)
            {
                var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet
                {
                    ["Arguments"] = "GetFileAssociation",
                    ["filepath"] = filePath,
                });

                if (status is AppServiceResponseStatus.Success && response.ContainsKey("FileAssociation"))
                {
                    return (string)response["FileAssociation"];
                }
            }
            return null;
        }

        public static bool HasFileAttribute(string lpFileName, FileAttributes dwAttrs)
            => GetFileAttributesExFromApp(lpFileName, GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out var lpFileInfo)
                && (lpFileInfo.dwFileAttributes & dwAttrs) == dwAttrs;

        public static bool SetFileAttribute(string lpFileName, FileAttributes dwAttrs)
            => GetFileAttributesExFromApp(lpFileName, GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out var lpFileInfo)
                && SetFileAttributesFromApp(lpFileName, lpFileInfo.dwFileAttributes | dwAttrs);

        public static bool UnsetFileAttribute(string lpFileName, FileAttributes dwAttrs)
            => GetFileAttributesExFromApp(lpFileName, GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out var lpFileInfo)
                && SetFileAttributesFromApp(lpFileName, lpFileInfo.dwFileAttributes & ~dwAttrs);

        public static string ReadStringFromFile(string filePath)
        {
            IntPtr hFile = CreateFileFromApp(filePath,
                GENERIC_READ,
                FILE_SHARE_READ,
                IntPtr.Zero,
                OPEN_EXISTING,
                (uint)File_Attributes.BackupSemantics,
                IntPtr.Zero
            );

            if (hFile.ToInt64() is not -1)
            {
                return null;
            }

            const int BUFFER_LENGTH = 4096;
            byte[] buffer = new byte[BUFFER_LENGTH];
            int dwBytesRead;
            string szRead = string.Empty;

            unsafe
            {
                using MemoryStream ms = new();
                using StreamReader reader = new(ms, true);
                while (true)
                {
                    fixed (byte* pBuffer = buffer)
                    {
                        if (ReadFile(hFile, pBuffer, BUFFER_LENGTH - 1, &dwBytesRead, IntPtr.Zero) && dwBytesRead > 0)
                        {
                            ms.Write(buffer, 0, dwBytesRead);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                ms.Position = 0;
                szRead = reader.ReadToEnd();
            }

            CloseHandle(hFile);
            return szRead;
        }

        public static bool WriteStringToFile(string filePath, string str, File_Attributes flags = 0)
        {
            IntPtr hStream = CreateFileFromApp(
                filePath,
                GENERIC_WRITE,
                0,
                IntPtr.Zero,
                CREATE_ALWAYS,
                (uint)(File_Attributes.BackupSemantics | flags),
                IntPtr.Zero
            );

            if (hStream.ToInt64() is not -1)
            {
                return false;
            }

            byte[] buff = Encoding.UTF8.GetBytes(str);
            int dwBytesWritten;
            unsafe
            {
                fixed (byte* pBuff = buff)
                {
                    WriteFile(hStream, pBuff, buff.Length, &dwBytesWritten, IntPtr.Zero);
                }
            }

            CloseHandle(hStream);
            return true;
        }

        public static bool WriteBufferToFileWithProgress(string filePath, byte[] buffer, NativeLpoverlappedCompletionRoutine callback)
        {
            using var hFile = CreateFileForWrite(filePath);

            if (hFile.IsInvalid)
            {
                return false;
            }

            NativeOverlapped nativeOverlapped = new NativeOverlapped();
            bool result = WriteFileEx(hFile.DangerousGetHandle(), buffer, (uint)buffer.LongLength, ref nativeOverlapped, callback);

            if (!result)
            {
                Debug.WriteLine(Marshal.GetLastWin32Error());
            }

            return result;
        }

        public static async Task<SafeFileHandle> OpenProtectedFileForRead(string filePath, bool readWrite = false)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection is not null)
            {
                var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet
                {
                    ["Arguments"] = "FileOperation",
                    ["fileop"] = "GetFileHandle",
                    [ "filepath"] = filePath,
                    ["readwrite"] = readWrite,
                    ["processid"] = Process.GetCurrentProcess().Id,
                });

                if (status is AppServiceResponseStatus.Success && response.Get("Success", false))
                {
                    return new SafeFileHandle(new IntPtr((long)response["Handle"]), true);
                }
            }
            return new SafeFileHandle(new IntPtr(-1), true);
        }

        // https://github.com/rad1oactive/BetterExplorer/blob/master/Windows%20API%20Code%20Pack%201.1/source/WindowsAPICodePack/Shell/ReparsePoint.cs
        public static string ParseSymLink(string path)
        {
            using var handle = OpenFileForRead(path, false, 0x00200000);
            if (!handle.IsInvalid)
            {
                REPARSE_DATA_BUFFER buffer = new();
                if (DeviceIoControl(handle.DangerousGetHandle(), FSCTL_GET_REPARSE_POINT,
                    IntPtr.Zero, 0, out buffer, MAXIMUM_REPARSE_DATA_BUFFER_SIZE, out _, IntPtr.Zero))
                {
                    string subsString = new(buffer.PathBuffer, 2 + buffer.SubsNameOffset / 2, buffer.SubsNameLength / 2);
                    string printString = new(buffer.PathBuffer, 2 + buffer.PrintNameOffset / 2, buffer.PrintNameLength / 2);
                    string normalisedTarget = printString ?? subsString;
                    if (string.IsNullOrEmpty(normalisedTarget))
                    {
                        normalisedTarget = subsString;
                        if (normalisedTarget.StartsWith(@"\??\", StringComparison.Ordinal))
                        {
                            normalisedTarget = normalisedTarget.Substring(4);
                        }
                    }
                    if (buffer.ReparseTag is IO_REPARSE_TAG_SYMLINK && (normalisedTarget.Length < 2 || normalisedTarget[1] is not ':'))
                    {
                        // Target is relative, get the absolute path
                        normalisedTarget = normalisedTarget.TrimStart(Path.DirectorySeparatorChar);
                        path = path.TrimEnd(Path.DirectorySeparatorChar);
                        normalisedTarget = Path.GetFullPath(Path.Combine(
                            path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar)), normalisedTarget));
                    }
                    return normalisedTarget;
                }
            }
            return null;
        }

        public static IEnumerable<(string Name, long Size)> GetAlternateStreams(string path)
        {
            using var handle = OpenFileForRead(path);
            if (!handle.IsInvalid)
            {
                var bufferSize = Marshal.SizeOf(typeof(FILE_STREAM_INFO)) * 10;
                var mem = Marshal.AllocHGlobal(bufferSize);
                if (GetFileInformationByHandleEx(handle.DangerousGetHandle(), FILE_INFO_BY_HANDLE_CLASS.FileStreamInfo, mem, (uint)bufferSize))
                {
                    uint offset = 0;
                    FILE_STREAM_INFO fileStruct;
                    do
                    {
                        fileStruct = Marshal.PtrToStructure<FILE_STREAM_INFO>(new IntPtr(mem.ToInt64() + offset));
                        var name = fileStruct.StreamName.Substring(0, (int)fileStruct.StreamNameLength / 2);
                        if (name.EndsWith(":$DATA") && name is not "::$DATA")
                        {
                            yield return (name, fileStruct.StreamSize);
                        }
                        offset += fileStruct.NextEntryOffset;
                    } while (fileStruct.NextEntryOffset is not 0);
                }
                Marshal.FreeHGlobal(mem);
            }
        }

        public static bool GetWin32FindDataForPath(string targetPath, out WIN32_FIND_DATA findData)
        {
            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

            IntPtr hFile = FindFirstFileExFromApp(
                targetPath,
                findInfoLevel,
                out findData,
                FINDEX_SEARCH_OPS.FindExSearchNameMatch,
                IntPtr.Zero,
                additionalFlags
            );
            if (hFile.ToInt64() is not -1)
            {
                FindClose(hFile);
                return true;
            }
            return false;
        }

        public static long GetSize(this WIN32_FIND_DATA findData)
        {
            const long MAXDWORD = 4294967295;

            long fDataFSize = findData.nFileSizeLow;

            return fDataFSize
                + (fDataFSize < 0 ? MAXDWORD + 1 : 0)
                + (findData.nFileSizeHigh > 0 ? findData.nFileSizeHigh * (MAXDWORD + 1) : 0)
            ;
        }

        // https://www.pinvoke.net/default.aspx/kernel32/GetFileInformationByHandleEx.html
        public static ulong? GetFolderFRN(string folderPath)
        {
            using var handle = OpenFileForRead(folderPath);
            if (!handle.IsInvalid)
            {
                var fileStruct = new FILE_ID_BOTH_DIR_INFO();
                if (GetFileInformationByHandleEx(
                    handle.DangerousGetHandle(),
                    FILE_INFO_BY_HANDLE_CLASS.FileIdBothDirectoryInfo,
                    out fileStruct,
                    (uint)Marshal.SizeOf(fileStruct)
                ))
                {
                    return (ulong)fileStruct.FileId;
                }
            }
            return null;
        }

        private static bool IsArmProcessor()
        {
            var handle = Process.GetCurrentProcess().Handle;
            return IsWow64Process2(handle, out _, out var nativeMachine) && nativeMachine is 0xaa64 or 0x01c0 or 0x01c2 or 0x01c4;
        }
    }
}
