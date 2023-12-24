﻿using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using static Files.Core.Helpers.Win32PInvoke;
using Vanara.PInvoke;

namespace Files.App.Helpers
{
	internal static class InteropHelper
	{
		public static bool GetWin32FindDataForPath(string targetPath, out Files.Core.Helpers.Win32PInvoke.WIN32_FIND_DATA findData)
		{
			FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;

			int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

			IntPtr hFile = FindFirstFileExFromApp(
				targetPath,
				findInfoLevel,
				out findData,
				FINDEX_SEARCH_OPS.FindExSearchNameMatch,
				IntPtr.Zero,
				additionalFlags);

			if (hFile.ToInt64() != -1)
			{
				FindClose(hFile);

				return true;
			}

			return false;
		}

		// https://stackoverflow.com/questions/54456140/how-to-detect-were-running-under-the-arm64-version-of-windows-10-in-net
		// https://learn.microsoft.com/windows/win32/sysinfo/image-file-machine-constants
		private static bool? isRunningOnArm = null;
		public static bool IsRunningOnArm
		{
			get
			{
				if (isRunningOnArm is null)
				{
					isRunningOnArm = IsArmProcessor();
					App.Logger.LogInformation("Running on ARM: {0}", isRunningOnArm);
				}
				return isRunningOnArm ?? false;
			}
		}

		private static bool IsArmProcessor()
		{
			var handle = Process.GetCurrentProcess().Handle;
			if (!IsWow64Process2(handle, out _, out var nativeMachine))
			{
				return false;
			}
			return (nativeMachine == 0xaa64 ||
					nativeMachine == 0x01c0 ||
					nativeMachine == 0x01c2 ||
					nativeMachine == 0x01c4);
		}

		private static bool? isHasThreadAccessPropertyPresent = null;

		public static bool IsHasThreadAccessPropertyPresent
		{
			get
			{
				isHasThreadAccessPropertyPresent ??= ApiInformation.IsPropertyPresent(typeof(DispatcherQueue).FullName, "HasThreadAccess");
				return isHasThreadAccessPropertyPresent ?? false;
			}
		}

		public static Task<string> GetFileAssociationAsync(string filePath)
			=> Win32API.GetFileAssociationAsync(filePath, true);


		/// <summary>
		/// Find out what process(es) have a lock on the specified file.
		/// </summary>
		/// <param name="path">Path of the file.</param>
		/// <returns>Processes locking the file</returns>
		/// <remarks>See also:
		/// http://msdn.microsoft.com/library/windows/desktop/aa373661(v=vs.85).aspx
		/// http://wyupdate.googlecode.com/svn-history/r401/trunk/frmFilesInUse.cs (no copyright in code at time of viewing)
		/// </remarks>
		public static List<Process> WhoIsLocking(string[] resources)
		{
			string key = Guid.NewGuid().ToString();
			List<Process> processes = new List<Process>();

			int res = RmStartSession(out uint handle, 0, key);
			if (res != 0) throw new Exception("Could not begin restart session.  Unable to determine file locker.");

			try
			{
				const int ERROR_MORE_DATA = 234;
				uint pnProcInfo = 0;
				uint lpdwRebootReasons = RmRebootReasonNone;

				res = RmRegisterResources(handle, (uint)resources.Length, resources, 0, null, 0, null);

				if (res != 0) throw new Exception("Could not register resource.");

				//Note: there's a race condition here -- the first call to RmGetList() returns
				//      the total number of process. However, when we call RmGetList() again to get
				//      the actual processes this number may have increased.
				res = RmGetList(handle, out uint pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);

				if (res == ERROR_MORE_DATA)
				{
					// Create an array to store the process results
					RM_PROCESS_INFO[] processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
					pnProcInfo = pnProcInfoNeeded;

					// Get the list
					res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);
					if (res == 0)
					{
						processes = new List<Process>((int)pnProcInfo);

						// Enumerate all of the results and add them to the
						// list to be returned
						for (int i = 0; i < pnProcInfo; i++)
						{
							try
							{
								processes.Add(Process.GetProcessById(processInfo[i].Process.dwProcessId));
							}
							// catch the error -- in case the process is no longer running
							catch (ArgumentException) { }
						}
					}
					else throw new Exception("Could not list processes locking resource.");
				}
				else if (res != 0) throw new Exception("Could not list processes locking resource. Failed to get size of result.");
			}
			finally
			{
				_ = RmEndSession(handle);
			}

			return processes;
		}


		public static SafeFileHandle CreateFileForWrite(string filePath, bool overwrite = true)
		{
			return new SafeFileHandle(CreateFileFromApp(filePath,
				GENERIC_WRITE, 0, IntPtr.Zero, overwrite ? CREATE_ALWAYS : OPEN_ALWAYS, (uint)File_Attributes.BackupSemantics, IntPtr.Zero), true);
		}

		public static SafeFileHandle OpenFileForRead(string filePath, bool readWrite = false, uint flags = 0)
		{
			return new SafeFileHandle(
				CreateFileFromApp(
					filePath,
					GENERIC_READ | (readWrite ? GENERIC_WRITE : 0), FILE_SHARE_READ | (readWrite ? 0 : FILE_SHARE_WRITE),
					IntPtr.Zero,
					OPEN_EXISTING,
					(uint)File_Attributes.BackupSemantics | flags,
					IntPtr.Zero),
				true);
		}

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

		public static void ChangeCursor(this UIElement uiElement, InputCursor cursor)
		{
			Type type = typeof(UIElement);

			type.InvokeMember(
				"ProtectedCursor",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance,
				null,
				uiElement,
				new object[] { cursor }
			);
		}

		public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
		{
			if (IntPtr.Size == 4)
				return SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
			else
				return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
		}
	}
}
