using System;
using System.Runtime.InteropServices;

namespace Files.Helpers
{
    public class NativeDirectoryChangesHelper
    {
        [DllImport("api-ms-win-core-handle-l1-1-0.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("api-ms-win-core-io-l1-1-1.dll")]
        public static extern bool GetOverlappedResult(IntPtr hFile, OVERLAPPED lpOverlapped, out int lpNumberOfBytesTransferred, bool bWait);

        [DllImport("api-ms-win-core-io-l1-1-1.dll")]
        public static extern bool CancelIo(IntPtr hFile);

        [DllImport("api-ms-win-core-io-l1-1-1.dll")]
        public static extern bool CancelIoEx(IntPtr hFile, IntPtr lpOverlapped);

        [DllImport("api-ms-win-core-synch-l1-2-0.dll")]
        public static extern uint WaitForMultipleObjectsEx(uint nCount, IntPtr[] lpHandles, bool bWaitAll, uint dwMilliseconds, bool bAlertable);

        [DllImport("api-ms-win-core-synch-l1-2-0.dll", SetLastError = true)]
        public static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        [DllImport("api-ms-win-core-synch-l1-2-0.dll", SetLastError = true)]
        public static extern bool ResetEvent(IntPtr hEvent);

        [DllImport("api-ms-win-core-synch-l1-2-0.dll", SetLastError = true)]
        public static extern UInt32 WaitForSingleObjectEx(IntPtr hHandle, UInt32 dwMilliseconds, bool bAlertable);

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

        public delegate void LpoverlappedCompletionRoutine(uint dwErrorCode,
            uint dwNumberOfBytesTransfered,
            OVERLAPPED lpOverlapped
        );

        public unsafe struct OVERLAPPED
        {
            public IntPtr Internal;
            public IntPtr InternalHigh;
            public Union PointerAndOffset;
            public IntPtr hEvent;

            [StructLayout(LayoutKind.Explicit)]
            public struct Union
            {
                [FieldOffset(0)] public void* IntPtr;
                [FieldOffset(0)] public OffsetPair Offset;

                public struct OffsetPair { public uint Offset; public uint OffsetHigh; }
            }
        }

        public const int FILE_NOTIFY_CHANGE_FILE_NAME = 1;
        public const int FILE_NOTIFY_CHANGE_DIR_NAME = 2;
        public const int FILE_NOTIFY_CHANGE_ATTRIBUTES = 4;

        public unsafe struct FILE_NOTIFY_INFORMATION
        {
            public uint NextEntryOffset;
            public uint Action;
            public uint FileNameLength;
            public fixed char FileName[1];
        }

        [DllImport("api-ms-win-core-file-l2-1-0.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public unsafe static extern bool ReadDirectoryChangesW(IntPtr hDirectory, byte* lpBuffer,
            int nBufferLength, bool bWatchSubtree, int dwNotifyFilter, int*
            lpBytesReturned, ref OVERLAPPED lpOverlapped,
            LpoverlappedCompletionRoutine lpCompletionRoutine);

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
    }
}