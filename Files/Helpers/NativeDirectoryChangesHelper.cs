using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Helpers
{
    public class NativeDirectoryChangesHelper
    {
        [DllImport("api-ms-win-core-io-l1-1-1.dll")]
        public static extern bool CancelIo(IntPtr hFile);

        [DllImport("api-ms-win-core-synch-l1-2-0.dll")]
        public static extern uint WaitForMultipleObjectsEx(uint nCount, IntPtr[] lpHandles, bool bWaitAll, uint dwMilliseconds, bool bAlertable);

        [DllImport("api-ms-win-core-synch-l1-2-0.dll", SetLastError = true)]
        public static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

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

        [StructLayout(LayoutKind.Explicit, Size = 20)]
        public struct OVERLAPPED
        {
            [FieldOffset(0)]
            public uint Internal;

            [FieldOffset(4)]
            public uint InternalHigh;

            [FieldOffset(8)]
            public uint Offset;

            [FieldOffset(12)]
            public uint OffsetHigh;

            [FieldOffset(8)]
            public IntPtr Pointer;

            [FieldOffset(16)]
            public IntPtr hEvent;
        }

        public const int FILE_NOTIFY_CHANGE_FILE_NAME = 1;
        public const int FILE_NOTIFY_CHANGE_DIR_NAME = 2;

        public struct FILE_NOTIFY_INFORMATION
        {
            public uint NextEntryOffset;
            public uint Action;
            public uint FileNameLength;
            public IntPtr FileName;
        }

        [DllImport("api-ms-win-core-file-l2-1-0.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool ReadDirectoryChangesW(IntPtr hDirectory, ref byte lpBuffer,
            int nBufferLength, bool bWatchSubtree, int dwNotifyFilter, out int
            lpBytesReturned, ref OVERLAPPED lpOverlapped,
            LpoverlappedCompletionRoutine lpCompletionRoutine);
    }
}
