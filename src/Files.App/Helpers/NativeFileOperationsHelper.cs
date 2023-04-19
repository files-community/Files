using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;

namespace Files.App.Helpers
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
		public const uint FILE_WRITE_ATTRIBUTES = 0x100;

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

		public static SafeFileHandle OpenFileForRead(string filePath, bool readWrite = false, uint flags = 0)
		{
			return new SafeFileHandle(CreateFileFromApp(filePath,
				GENERIC_READ | (readWrite ? GENERIC_WRITE : 0), FILE_SHARE_READ | (readWrite ? 0 : FILE_SHARE_WRITE), IntPtr.Zero, OPEN_EXISTING, (uint)File_Attributes.BackupSemantics | flags, IntPtr.Zero), true);
		}

		private const int MAXIMUM_REPARSE_DATA_BUFFER_SIZE = 16 * 1024;
		private const int FSCTL_GET_REPARSE_POINT = 0x000900A8;
		public const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;
		public const uint IO_REPARSE_TAG_SYMLINK = 0xA000000C;

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct REPARSE_DATA_BUFFER
		{
			public uint ReparseTag;
			public short ReparseDataLength;
			public short Reserved;
			public short SubsNameOffset;
			public short SubsNameLength;
			public short PrintNameOffset;
			public short PrintNameLength;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXIMUM_REPARSE_DATA_BUFFER_SIZE)]
			public char[] PathBuffer;
		}

		[DllImport("api-ms-win-core-io-l1-1-0.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeviceIoControl(
			IntPtr hDevice,
			uint dwIoControlCode,
			IntPtr lpInBuffer,
			uint nInBufferSize,
			//IntPtr lpOutBuffer,
			out REPARSE_DATA_BUFFER outBuffer,
			uint nOutBufferSize,
			out uint lpBytesReturned,
			IntPtr lpOverlapped);

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

		[DllImport("api-ms-win-core-file-l1-2-1.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool GetFileTime([In] IntPtr hFile, out FILETIME lpCreationTime, out FILETIME lpLastAccessTime, out FILETIME lpLastWriteTime);

		[DllImport("api-ms-win-core-file-l1-2-1.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool SetFileTime([In] IntPtr hFile, in FILETIME lpCreationTime, in FILETIME lpLastAccessTime, in FILETIME lpLastWriteTime);

		private enum FILE_INFO_BY_HANDLE_CLASS
		{
			FileBasicInfo = 0,
			FileStandardInfo = 1,
			FileNameInfo = 2,
			FileRenameInfo = 3,
			FileDispositionInfo = 4,
			FileAllocationInfo = 5,
			FileEndOfFileInfo = 6,
			FileStreamInfo = 7,
			FileCompressionInfo = 8,
			FileAttributeTagInfo = 9,
			FileIdBothDirectoryInfo = 10,// 0x0A
			FileIdBothDirectoryRestartInfo = 11, // 0xB
			FileIoPriorityHintInfo = 12, // 0xC
			FileRemoteProtocolInfo = 13, // 0xD
			FileFullDirectoryInfo = 14, // 0xE
			FileFullDirectoryRestartInfo = 15, // 0xF
			FileStorageInfo = 16, // 0x10
			FileAlignmentInfo = 17, // 0x11
			FileIdInfo = 18, // 0x12
			FileIdExtdDirectoryInfo = 19, // 0x13
			FileIdExtdDirectoryRestartInfo = 20, // 0x14
			MaximumFileInfoByHandlesClass
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct FILE_ID_BOTH_DIR_INFO
		{
			public uint NextEntryOffset;
			public uint FileIndex;
			public long CreationTime;
			public long LastAccessTime;
			public long LastWriteTime;
			public long ChangeTime;
			public long EndOfFile;
			public long AllocationSize;
			public uint FileAttributes;
			public uint FileNameLength;
			public uint EaSize;
			public char ShortNameLength;
			[MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 12)]
			public string ShortName;
			public long FileId;
			[MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 1)]
			public string FileName;
		}

		[DllImport("api-ms-win-core-file-l2-1-1.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		private static extern bool GetFileInformationByHandleEx(IntPtr hFile, FILE_INFO_BY_HANDLE_CLASS infoClass, out FILE_ID_BOTH_DIR_INFO dirInfo, uint dwBufferSize);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
		private struct FILE_STREAM_INFO
		{
			public uint NextEntryOffset;
			public uint StreamNameLength;
			public long StreamSize;
			public long StreamAllocationSize;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
			public string StreamName;
		}

		[DllImport("api-ms-win-core-file-l2-1-1.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		private static extern bool GetFileInformationByHandleEx(IntPtr hFile, FILE_INFO_BY_HANDLE_CLASS infoClass, IntPtr dirInfo, uint dwBufferSize);

		public static bool GetFileDateModified(string filePath, out FILETIME dateModified)
		{
			using var hFile = new SafeFileHandle(CreateFileFromApp(filePath, GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, (uint)File_Attributes.BackupSemantics, IntPtr.Zero), true);
			return GetFileTime(hFile.DangerousGetHandle(), out _, out _, out dateModified);
		}

		public static bool SetFileDateModified(string filePath, FILETIME dateModified)
		{
			using var hFile = new SafeFileHandle(CreateFileFromApp(filePath, FILE_WRITE_ATTRIBUTES, 0, IntPtr.Zero, OPEN_EXISTING, (uint)File_Attributes.BackupSemantics, IntPtr.Zero), true);
			return SetFileTime(hFile.DangerousGetHandle(), new(), new(), dateModified);
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
				using (MemoryStream ms = new MemoryStream())
				using (StreamReader reader = new StreamReader(ms, true))
				{
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

		// https://www.pinvoke.net/default.aspx/kernel32/GetFileInformationByHandleEx.html
		public static ulong? GetFolderFRN(string folderPath)
		{
			using var handle = OpenFileForRead(folderPath);
			if (!handle.IsInvalid)
			{
				var fileStruct = new FILE_ID_BOTH_DIR_INFO();
				if (GetFileInformationByHandleEx(handle.DangerousGetHandle(), FILE_INFO_BY_HANDLE_CLASS.FileIdBothDirectoryInfo, out fileStruct, (uint)Marshal.SizeOf(fileStruct)))
				{
					return (ulong)fileStruct.FileId;
				}
			}
			return null;
		}

		public static ulong? GetFileFRN(string filePath)
		{
			using var handle = OpenFileForRead(filePath);
			if (!handle.IsInvalid)
			{
				try
				{
					var fileID = Kernel32.GetFileInformationByHandleEx<Kernel32.FILE_ID_INFO>(handle, Kernel32.FILE_INFO_BY_HANDLE_CLASS.FileIdInfo);
					return BitConverter.ToUInt64(fileID.FileId.Identifier, 0);
				}
				catch { }
			}
			return null;
		}

		// https://github.com/rad1oactive/BetterExplorer/blob/master/Windows%20API%20Code%20Pack%201.1/source/WindowsAPICodePack/Shell/ReparsePoint.cs
		public static string ParseSymLink(string path)
		{
			using var handle = OpenFileForRead(path, false, 0x00200000);
			if (!handle.IsInvalid)
			{
				REPARSE_DATA_BUFFER buffer = new REPARSE_DATA_BUFFER();
				if (DeviceIoControl(handle.DangerousGetHandle(), FSCTL_GET_REPARSE_POINT, IntPtr.Zero, 0, out buffer, MAXIMUM_REPARSE_DATA_BUFFER_SIZE, out _, IntPtr.Zero))
				{
					var subsString = new string(buffer.PathBuffer, ((buffer.SubsNameOffset / 2) + 2), buffer.SubsNameLength / 2);
					var printString = new string(buffer.PathBuffer, ((buffer.PrintNameOffset / 2) + 2), buffer.PrintNameLength / 2);
					var normalisedTarget = printString ?? subsString;
					if (string.IsNullOrEmpty(normalisedTarget))
					{
						normalisedTarget = subsString;
						if (normalisedTarget.StartsWith(@"\??\", StringComparison.Ordinal))
						{
							normalisedTarget = normalisedTarget.Substring(4);
						}
					}
					if (buffer.ReparseTag == IO_REPARSE_TAG_SYMLINK && (normalisedTarget.Length < 2 || normalisedTarget[1] != ':'))
					{
						// Target is relative, get the absolute path
						normalisedTarget = normalisedTarget.TrimStart(Path.DirectorySeparatorChar);
						path = path.TrimEnd(Path.DirectorySeparatorChar);
						normalisedTarget = Path.GetFullPath(Path.Combine(path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar)), normalisedTarget));
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
						if (name.EndsWith(":$DATA") && name != "::$DATA")
						{
							yield return (name, fileStruct.StreamSize);
						}
						offset += fileStruct.NextEntryOffset;
					} while (fileStruct.NextEntryOffset != 0);
				}
				Marshal.FreeHGlobal(mem);
			}
		}
	}
}
