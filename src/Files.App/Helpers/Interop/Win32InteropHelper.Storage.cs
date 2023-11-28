// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Vanara.PInvoke;

namespace Files.App.Helpers
{
	public static partial class Win32InteropHelper
	{
		public static SafeFileHandle CreateFileForWrite(string filePath, bool overwrite = true)
		{
			return new SafeFileHandle(Win32Interop.CreateFileFromApp(filePath,
				Win32Interop.GENERIC_WRITE, 0, nint.Zero, overwrite ? Win32Interop.CREATE_ALWAYS : Win32Interop.OPEN_ALWAYS, (uint)Win32Interop.File_Attributes.BackupSemantics, nint.Zero), true);
		}

		public static SafeFileHandle OpenFileForRead(string filePath, bool readWrite = false, uint flags = 0)
		{
			return new SafeFileHandle(Win32Interop.CreateFileFromApp(filePath,
				Win32Interop.GENERIC_READ | (readWrite ? Win32Interop.GENERIC_WRITE : 0), Win32Interop.FILE_SHARE_READ | (readWrite ? 0 : Win32Interop.FILE_SHARE_WRITE), nint.Zero, Win32Interop.OPEN_EXISTING, (uint)Win32Interop.File_Attributes.BackupSemantics | flags, nint.Zero), true);
		}

		public static bool GetFileDateModified(string filePath, out FILETIME dateModified)
		{
			using var hFile = new SafeFileHandle(Win32Interop.CreateFileFromApp(filePath, Win32Interop.GENERIC_READ, Win32Interop.FILE_SHARE_READ, nint.Zero, Win32Interop.OPEN_EXISTING, (uint)Win32Interop.File_Attributes.BackupSemantics, nint.Zero), true);

			return Win32Interop.GetFileTime(hFile.DangerousGetHandle(), out _, out _, out dateModified);
		}

		public static bool SetFileDateModified(string filePath, FILETIME dateModified)
		{
			using var hFile = new SafeFileHandle(Win32Interop.CreateFileFromApp(filePath, Win32Interop.FILE_WRITE_ATTRIBUTES, 0, nint.Zero, Win32Interop.OPEN_EXISTING, (uint)Win32Interop.File_Attributes.BackupSemantics, nint.Zero), true);

			return Win32Interop.SetFileTime(hFile.DangerousGetHandle(), new(), new(), dateModified);
		}

		public static bool HasFileAttribute(string lpFileName, FileAttributes dwAttrs)
		{
			if (Win32Interop.GetFileAttributesExFromApp(
				lpFileName,
				Win32Interop.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard,
				out var lpFileInfo))
			{
				return (lpFileInfo.dwFileAttributes & dwAttrs) == dwAttrs;
			}
			return false;
		}

		public static bool SetFileAttribute(string lpFileName, FileAttributes dwAttrs)
		{
			if (!Win32Interop.GetFileAttributesExFromApp(
				lpFileName,
				Win32Interop.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard,
				out var lpFileInfo))
			{
				return false;
			}

			return Win32Interop.SetFileAttributesFromApp(lpFileName, lpFileInfo.dwFileAttributes | dwAttrs);
		}

		public static bool UnsetFileAttribute(string lpFileName, FileAttributes dwAttrs)
		{
			if (!Win32Interop.GetFileAttributesExFromApp(
				lpFileName,
				Win32Interop.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard,
				out var lpFileInfo))
			{
				return false;
			}

			return Win32Interop.SetFileAttributesFromApp(lpFileName, lpFileInfo.dwFileAttributes & ~dwAttrs);
		}

		public static string ReadStringFromFile(string filePath)
		{
			IntPtr hFile = Win32Interop.CreateFileFromApp(filePath,
				Win32Interop.GENERIC_READ,
				Win32Interop.FILE_SHARE_READ,
				nint.Zero,
				Win32Interop.OPEN_EXISTING,
				(uint)Win32Interop.File_Attributes.BackupSemantics,
				nint.Zero);

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
				using (MemoryStream ms = new())
				using (StreamReader reader = new(ms, true))
				{
					while (true)
					{
						fixed (byte* pBuffer = buffer)
						{
							if (Win32Interop.ReadFile(hFile, pBuffer, BUFFER_LENGTH - 1, &dwBytesRead, nint.Zero) && dwBytesRead > 0)
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

			Win32Interop.CloseHandle(hFile);

			return szRead;
		}

		public static bool WriteStringToFile(string filePath, string str, Win32Interop.File_Attributes flags = 0)
		{
			IntPtr hStream = Win32Interop.CreateFileFromApp(
				filePath,
				Win32Interop.GENERIC_WRITE,
				0,
				nint.Zero,
				Win32Interop.CREATE_ALWAYS,
				(uint)(Win32Interop.File_Attributes.BackupSemantics | flags),
				nint.Zero);

			if (hStream.ToInt64() == -1)
				return false;

			byte[] buff = Encoding.UTF8.GetBytes(str);
			int dwBytesWritten;

			unsafe
			{
				fixed (byte* pBuff = buff)
					Win32Interop.WriteFile(hStream, pBuff, buff.Length, &dwBytesWritten, nint.Zero);
			}

			Win32Interop.CloseHandle(hStream);

			return true;
		}

		public static bool WriteBufferToFileWithProgress(string filePath, byte[] buffer, Win32Interop.LPOVERLAPPED_COMPLETION_ROUTINE callback)
		{
			using var hFile = CreateFileForWrite(filePath);

			if (hFile.IsInvalid)
				return false;

			NativeOverlapped nativeOverlapped = new();

			bool result = Win32Interop.WriteFileEx(hFile.DangerousGetHandle(), buffer, (uint)buffer.LongLength, ref nativeOverlapped, callback);

			if (!result)
				Debug.WriteLine(Marshal.GetLastWin32Error());

			return result;
		}

		// https://www.pinvoke.net/default.aspx/kernel32/GetFileInformationByHandleEx.html
		public static ulong? GetFolderFRN(string folderPath)
		{
			using var handle = OpenFileForRead(folderPath);

			if (!handle.IsInvalid)
			{
				var fileStruct = new Win32Interop.FILE_ID_BOTH_DIR_INFO();

				if (Win32Interop.GetFileInformationByHandleEx(handle.DangerousGetHandle(), Win32Interop.FILE_INFO_BY_HANDLE_CLASS.FileIdBothDirectoryInfo, out fileStruct, (uint)Marshal.SizeOf(fileStruct)))
					return (ulong)fileStruct.FileId;
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

		public static long? GetFileSizeOnDisk(string filePath)
		{
			using var handle = OpenFileForRead(filePath);
			if (!handle.IsInvalid)
			{
				try
				{
					var fileAllocationInfo = Kernel32.GetFileInformationByHandleEx<Kernel32.FILE_STANDARD_INFO>(handle, Kernel32.FILE_INFO_BY_HANDLE_CLASS.FileStandardInfo);

					return fileAllocationInfo.AllocationSize;
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
				Win32Interop.REPARSE_DATA_BUFFER buffer = new();

				if (Win32Interop.DeviceIoControl(handle.DangerousGetHandle(), Win32Interop.FSCTL_GET_REPARSE_POINT, nint.Zero, 0, out buffer, Win32Interop.MAXIMUM_REPARSE_DATA_BUFFER_SIZE, out _, nint.Zero))
				{
					var subsString = new string(buffer.PathBuffer, ((buffer.SubsNameOffset / 2) + 2), buffer.SubsNameLength / 2);
					var printString = new string(buffer.PathBuffer, ((buffer.PrintNameOffset / 2) + 2), buffer.PrintNameLength / 2);
	
					var normalisedTarget = printString ?? subsString;

					if (string.IsNullOrEmpty(normalisedTarget))
					{
						normalisedTarget = subsString;

						if (normalisedTarget.StartsWith(@"\??\", StringComparison.Ordinal))
							normalisedTarget = normalisedTarget.Substring(4);
					}

					if (buffer.ReparseTag == Win32Interop.IO_REPARSE_TAG_SYMLINK && (normalisedTarget.Length < 2 || normalisedTarget[1] != ':'))
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
				var bufferSize = Marshal.SizeOf(typeof(Win32Interop.FILE_STREAM_INFO)) * 10;
				var mem = Marshal.AllocHGlobal(bufferSize);
				if (Win32Interop.GetFileInformationByHandleEx(handle.DangerousGetHandle(), Win32Interop.FILE_INFO_BY_HANDLE_CLASS.FileStreamInfo, mem, (uint)bufferSize))
				{
					uint offset = 0;
					Win32Interop.FILE_STREAM_INFO fileStruct;
					do
					{
						fileStruct = Marshal.PtrToStructure<Win32Interop.FILE_STREAM_INFO>(new IntPtr(mem.ToInt64() + offset));
						var name = fileStruct.StreamName.Substring(0, (int)fileStruct.StreamNameLength / 2);

						if (name.EndsWith(":$DATA") && name != "::$DATA")
							yield return (name, fileStruct.StreamSize);

						offset += fileStruct.NextEntryOffset;

					} while (fileStruct.NextEntryOffset != 0);
				}

				Marshal.FreeHGlobal(mem);
			}
		}
	}
}
