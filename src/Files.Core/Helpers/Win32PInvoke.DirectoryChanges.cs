// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;

namespace Files.Core.Helpers
{
	public static partial class Win32PInvoke
	{
		public const int FILE_NOTIFY_CHANGE_FILE_NAME = 1;
		public const int FILE_NOTIFY_CHANGE_DIR_NAME = 2;
		public const int FILE_NOTIFY_CHANGE_ATTRIBUTES = 4;
		public const int FILE_NOTIFY_CHANGE_SIZE = 8;
		public const int FILE_NOTIFY_CHANGE_LAST_WRITE = 16;
		public const int FILE_NOTIFY_CHANGE_LAST_ACCESS = 32;
		public const int FILE_NOTIFY_CHANGE_CREATION = 64;
		public const int FILE_NOTIFY_CHANGE_SECURITY = 256;

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

		public unsafe struct FILE_NOTIFY_INFORMATION
		{
			public uint NextEntryOffset;
			public uint Action;
			public uint FileNameLength;
			public fixed char FileName[1];
		}

		[DllImport("api-ms-win-core-io-l1-1-1.dll")]
		public static extern bool CancelIoEx(
			IntPtr hFile,
			IntPtr lpOverlapped
		);

		[DllImport("api-ms-win-core-synch-l1-2-0.dll", SetLastError = true)]
		public static extern IntPtr CreateEvent(
			IntPtr lpEventAttributes,
			bool bManualReset,
			bool bInitialState,
			string lpName
		);

		[DllImport("api-ms-win-core-synch-l1-2-0.dll", SetLastError = true)]
		public static extern uint WaitForSingleObjectEx(
			IntPtr hHandle,
			uint dwMilliseconds,
			bool bAlertable
		);

		public delegate void LpOverlappedCompletionRoutine(
			uint dwErrorCode,
			uint dwNumberOfBytesTransferred,
			OVERLAPPED lpOverlapped
		);

		[DllImport("api-ms-win-core-file-l2-1-0.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public unsafe static extern bool ReadDirectoryChangesW(
			IntPtr hDirectory,
			byte* lpBuffer,
			int nBufferLength,
			bool bWatchSubtree,
			int dwNotifyFilter,
			int* lpBytesReturned,
			ref OVERLAPPED lpOverlapped,
			LpOverlappedCompletionRoutine lpCompletionRoutine
		);
	}
}
