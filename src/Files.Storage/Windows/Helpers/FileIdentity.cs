// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;

namespace Files.App.Storage;

public static class FileIdentity
{
	public static string? PathFromFileId(ulong frn, string volumeHint)
	{
		string? volumePath = Path.GetPathRoot(volumeHint);
		if (string.IsNullOrEmpty(volumePath))
			return null;

		using var volumeHandle = PInvoke.CreateFile(volumePath, (uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_READ, FILE_SHARE_MODE.FILE_SHARE_READ, null, FILE_CREATION_DISPOSITION.OPEN_EXISTING, FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS, null);
		if (volumeHandle.IsInvalid)
			return null;

		FILE_ID_DESCRIPTOR fileId = new() { dwSize = (uint)Marshal.SizeOf<FILE_ID_DESCRIPTOR>(), Type = FILE_ID_TYPE.FileIdType };
		fileId.FileId = (long)frn;

		using var hFile = PInvoke.OpenFileById(volumeHandle, fileId, (uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_READ, FILE_SHARE_MODE.FILE_SHARE_READ, null, FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS);
		if (hFile.IsInvalid)
			return null;

		Span<char> path = stackalloc char[4096];
		uint length = PInvoke.GetFinalPathNameByHandle(hFile, path, GETFINALPATHNAMEBYHANDLE_FLAGS.FILE_NAME_NORMALIZED | GETFINALPATHNAMEBYHANDLE_FLAGS.VOLUME_NAME_DOS);
		return length is > 0 and < 4096 ? path[..(int)length].ToString() : null;
	}

	public static unsafe ulong? GetFileFRN(string filePath)
	{
		using var handle = PInvoke.CreateFile(filePath, (uint)FILE_ACCESS_RIGHTS.FILE_READ_ATTRIBUTES, FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE | FILE_SHARE_MODE.FILE_SHARE_DELETE, null, FILE_CREATION_DISPOSITION.OPEN_EXISTING, FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS, null);
		if (!handle.IsInvalid)
		{
			try
			{
				FILE_ID_INFO fileId = default;
				if (PInvoke.GetFileInformationByHandleEx(handle, FILE_INFO_BY_HANDLE_CLASS.FileIdInfo, MemoryMarshal.AsBytes(new Span<FILE_ID_INFO>(ref fileId))))
					return BitConverter.ToUInt64(fileId.FileId.Identifier.AsReadOnlySpan());
			}
			catch { }
		}
		return null;
	}
}
