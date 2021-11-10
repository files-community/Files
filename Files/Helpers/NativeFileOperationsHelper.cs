using Files.Common;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Collections;

namespace Files.Helpers
{
    public class NativeFileOperationsHelper
    {
        public enum File_Attributes : uint
        {
            Readonly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Directory = 0x00000010,
            Archive = 0x00000020,
            Device = 0x00000040,
            Normal = 0x00000080,
            Temporary = 0x00000100,
            SparseFile = 0x00000200,
            ReparsePoint = 0x00000400,
            Compressed = 0x00000800,
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            Encrypted = 0x00004000,
            Write_Through = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            FirstPipeInstance = 0x00080000
        }

        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint FILE_APPEND_DATA = 0x0004;

        public const uint FILE_SHARE_READ = 0x00000001;
        public const uint FILE_SHARE_WRITE = 0x00000002;
        public const uint FILE_SHARE_DELETE = 0x00000004;

        public const uint FILE_BEGIN = 0;
        public const uint FILE_END = 2;

        public const uint CREATE_ALWAYS = 2;
        public const uint CREATE_NEW = 1;
        public const uint OPEN_ALWAYS = 4;
        public const uint OPEN_EXISTING = 3;
        public const uint TRUNCATE_EXISTING = 5;

        [DllImport("api-ms-win-core-handle-l1-1-0.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.StdCall,
        SetLastError = true)]
        public static extern IntPtr CreateFileFromApp(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr SecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile
        );

        public static SafeFileHandle CreateFileForWrite(string filePath, bool overwrite = true)
        {
            return new SafeFileHandle(CreateFileFromApp(filePath,
                GENERIC_WRITE, 0, IntPtr.Zero, overwrite ? CREATE_ALWAYS : OPEN_ALWAYS, (uint)File_Attributes.BackupSemantics, IntPtr.Zero), true);
        }

        public static SafeFileHandle OpenFileForRead(string filePath, bool readWrite = false)
        {
            return new SafeFileHandle(CreateFileFromApp(filePath,
                GENERIC_READ | (readWrite ? GENERIC_WRITE : 0), FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_ALWAYS, (uint)File_Attributes.BackupSemantics, IntPtr.Zero), true);
        }

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.StdCall,
        SetLastError = true)]
        public static extern IntPtr CreateFile2FromApp(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            uint dwCreationDisposition,
            IntPtr pCreateExParams
        );

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.StdCall,
        SetLastError = true)]
        public static extern bool CreateDirectoryFromApp(
            string lpPathName,
            IntPtr SecurityAttributes
        );

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.StdCall,
        SetLastError = true)]
        public static extern bool MoveFileFromApp(
            string lpExistingFileName,
            string lpNewFileName
        );

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.StdCall,
        SetLastError = true)]
        public static extern bool CopyFileFromApp(
            string lpExistingFileName,
            string lpNewFileName,
            bool bFailIfExists
        );

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.StdCall,
        SetLastError = true)]
        public static extern bool DeleteFileFromApp(
            string lpFileName
        );

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.StdCall,
        SetLastError = true)]
        public static extern bool RemoveDirectoryFromApp(
            string lpPathName
        );

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetFileAttributesExFromApp(
            string lpFileName,
            GET_FILEEX_INFO_LEVELS fInfoLevelId,
            out WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetFileAttributesFromApp(
            string lpFileName,
            FileAttributes dwFileAttributes);

        [DllImport("api-ms-win-core-file-l1-2-1.dll", ExactSpelling = true,
        CallingConvention = CallingConvention.StdCall,
        SetLastError = true)]
        public static extern uint SetFilePointer(
            IntPtr hFile,
            long lDistanceToMove,
            IntPtr lpDistanceToMoveHigh,
            uint dwMoveMethod
        );

        [DllImport("api-ms-win-core-file-l1-2-1.dll", CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.StdCall,
        SetLastError = true)]
        public unsafe static extern bool ReadFile(
            IntPtr hFile,
            byte* lpBuffer,
            int nBufferLength,
            int* lpBytesReturned,
            IntPtr lpOverlapped
        );

        [DllImport("api-ms-win-core-file-l1-2-1.dll", CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.StdCall,
        SetLastError = true)]
        public unsafe static extern bool WriteFile(
            IntPtr hFile,
            byte* lpBuffer,
            int nBufferLength,
            int* lpBytesWritten,
            IntPtr lpOverlapped
        );

        [DllImport("api-ms-win-core-file-l1-2-1.dll", CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall,
            SetLastError = true)]
        public static extern bool WriteFileEx(
            IntPtr hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToWrite,
            [In] ref NativeOverlapped lpOverlapped,
            LPOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine);

        public delegate void LPOVERLAPPED_COMPLETION_ROUTINE(uint dwErrorCode, uint dwNumberOfBytesTransfered, ref NativeOverlapped lpOverlapped);

        public enum GET_FILEEX_INFO_LEVELS
        {
            GetFileExInfoStandard,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WIN32_FILE_ATTRIBUTE_DATA
        {
            public FileAttributes dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
        }

        public static bool HasFileAttribute(string lpFileName, FileAttributes dwAttrs)
        {
            if (GetFileAttributesExFromApp(
                lpFileName, GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out var lpFileInfo))
            {
                return (lpFileInfo.dwFileAttributes & dwAttrs) == dwAttrs;
            }
            return false;
        }

        public static bool SetFileAttribute(string lpFileName, FileAttributes dwAttrs)
        {
            if (!GetFileAttributesExFromApp(
                lpFileName, GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out var lpFileInfo))
            {
                return false;
            }
            return SetFileAttributesFromApp(lpFileName, lpFileInfo.dwFileAttributes | dwAttrs);
        }

        public static bool UnsetFileAttribute(string lpFileName, FileAttributes dwAttrs)
        {
            if (!GetFileAttributesExFromApp(
                lpFileName, GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out var lpFileInfo))
            {
                return false;
            }
            return SetFileAttributesFromApp(lpFileName, lpFileInfo.dwFileAttributes & ~dwAttrs);
        }

        public static string ReadStringFromFile(string filePath)
        {
            IntPtr hFile = CreateFileFromApp(filePath,
                GENERIC_READ,
                FILE_SHARE_READ,
                IntPtr.Zero,
                OPEN_EXISTING,
                (uint)File_Attributes.BackupSemantics,
                IntPtr.Zero);

            if (hFile.ToInt64() == -1)
            {
                return null;
            }

            const int BUFFER_LENGTH = 4096;

            byte[] buffer = new byte[BUFFER_LENGTH];
            int dwBytesRead;
            string szRead = string.Empty;

            unsafe
            {
                bool bRead = false;

                using (MemoryStream msBuffer = new MemoryStream(buffer))
                {
                    using (StreamReader reader = new StreamReader(msBuffer, true))
                    {
                        do
                        {
                            fixed (byte* pBuffer = buffer)
                            {
                                Array.Clear(buffer, 0, buffer.Length);
                                msBuffer.Position = 0;

                                if (bRead = ReadFile(hFile, pBuffer, BUFFER_LENGTH - 1, &dwBytesRead, IntPtr.Zero) && dwBytesRead > 0)
                                {
                                    szRead += reader.ReadToEnd().Substring(0, dwBytesRead);
                                }
                                else
                                {
                                    break;
                                }
                            }

                        } while (bRead);
                    }
                }
            }

            CloseHandle(hFile);

            return szRead;
        }

        public static bool WriteStringToFile(string filePath, string str, File_Attributes flags = 0)
        {
            IntPtr hStream = CreateFileFromApp(filePath,
                GENERIC_WRITE, 0, IntPtr.Zero, CREATE_ALWAYS, (uint)(File_Attributes.BackupSemantics | flags), IntPtr.Zero);
            if (hStream.ToInt64() == -1)
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

        public static bool WriteBufferToFileWithProgress(string filePath, byte[] buffer, LPOVERLAPPED_COMPLETION_ROUTINE callback)
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
                System.Diagnostics.Debug.WriteLine(Marshal.GetLastWin32Error());
            }

            return result;
        }

        public static async Task<SafeFileHandle> OpenProtectedFileForRead(string filePath, bool readWrite = false)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "FileOperation" },
                    { "fileop", "GetFileHandle" },
                    { "filepath", filePath },
                    { "readwrite", readWrite },
                    { "processid", System.Diagnostics.Process.GetCurrentProcess().Id },
                });
                if (status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success && response.Get("Success", false))
                {
                    return new SafeFileHandle(new IntPtr((long)response["Handle"]), true);
                }
            }
            return new SafeFileHandle(new IntPtr(-1), true);
        }
    }
}
