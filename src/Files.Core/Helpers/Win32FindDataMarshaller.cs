using System.Runtime.InteropServices.Marshalling;

namespace Files.Core.Helpers;

[CustomMarshaller(typeof(WIN32_FIND_DATA), MarshalMode.ManagedToUnmanagedOut, typeof(Win32FindDataMarshaller))]
internal static unsafe class Win32FindDataMarshaller
{
	public struct WIN32_FIND_DATA_UNMANAGED
	{
		public SystemIO.FileAttributes dwFileAttributes;
		public uint ftCreationTime_dwLowDateTime;
		public uint ftCreationTime_dwHighDateTime;
		public uint ftLastAccessTime_dwLowDateTime;
		public uint ftLastAccessTime_dwHighDateTime;
		public uint ftLastWriteTime_dwLowDateTime;
		public uint ftLastWriteTime_dwHighDateTime;
		public uint nFileSizeHigh;
		public uint nFileSizeLow;
		public uint dwReserved0;
		public uint dwReserved1;
		public fixed ushort cFileName[256];
		public fixed ushort cAlternateFileName[14];
	}


	public static WIN32_FIND_DATA ConvertToManaged(WIN32_FIND_DATA_UNMANAGED unmanaged)
	{
		var managed = new WIN32_FIND_DATA();
		managed.dwFileAttributes = (uint)unmanaged.dwFileAttributes;
		managed.ftCreationTime.dwLowDateTime = (int)unmanaged.ftCreationTime_dwLowDateTime;
		managed.ftCreationTime.dwHighDateTime = (int)unmanaged.ftCreationTime_dwHighDateTime;
		managed.ftLastAccessTime.dwLowDateTime = (int)unmanaged.ftLastAccessTime_dwLowDateTime;
		managed.ftLastAccessTime.dwHighDateTime = (int)unmanaged.ftLastAccessTime_dwHighDateTime;
		managed.ftLastWriteTime.dwLowDateTime = (int)unmanaged.ftLastWriteTime_dwLowDateTime;
		managed.ftLastWriteTime.dwHighDateTime = (int)unmanaged.ftLastWriteTime_dwHighDateTime;
		managed.nFileSizeHigh = unmanaged.nFileSizeHigh;
		managed.nFileSizeLow = unmanaged.nFileSizeLow;

		managed.cFileName = GetStringFromBuffer(unmanaged.cFileName);
		managed.cAlternateFileName = GetStringFromBuffer(unmanaged.cAlternateFileName);
		return managed;
	}

	public static WIN32_FIND_DATA_UNMANAGED ConvertToUnmanaged(WIN32_FIND_DATA managed)
	{
		WIN32_FIND_DATA_UNMANAGED unmanaged = new WIN32_FIND_DATA_UNMANAGED();
		unmanaged.dwFileAttributes = (SystemIO.FileAttributes)managed.dwFileAttributes;
		unmanaged.ftCreationTime_dwLowDateTime = (uint)managed.ftCreationTime.dwLowDateTime;
		unmanaged.ftCreationTime_dwHighDateTime = (uint)managed.ftCreationTime.dwHighDateTime;
		unmanaged.ftLastAccessTime_dwLowDateTime = (uint)managed.ftLastAccessTime.dwLowDateTime;
		unmanaged.ftLastAccessTime_dwHighDateTime = (uint)managed.ftLastAccessTime.dwHighDateTime;
		unmanaged.ftLastWriteTime_dwLowDateTime = (uint)managed.ftLastWriteTime.dwLowDateTime;
		unmanaged.ftLastWriteTime_dwHighDateTime = (uint)managed.ftLastWriteTime.dwHighDateTime;
		unmanaged.nFileSizeHigh = managed.nFileSizeHigh;
		unmanaged.nFileSizeLow = managed.nFileSizeLow;

		CopyStringToBuffer(managed.cFileName, unmanaged.cFileName);
		CopyStringToBuffer(managed.cAlternateFileName, unmanaged.cAlternateFileName);
		return unmanaged;
	}

	private static void CopyStringToBuffer(string source, ushort* destination)
	{
		int index = 0;
		foreach (char c in source)
		{
			destination[index++] = c;
			if (index >= source.Length)
				break;
		}

		destination[index] = '\0'; // Null-terminate the string
	}

	private static string GetStringFromBuffer(ushort* source)
	{
		int index = 0;
		char[] chars = new char[256]; // Assuming max buffer size for file name
		while (source[index] != '\0' && index < 256)
		{
			chars[index] = (char)source[index];
			index++;
		}

		return new string(chars, 0, index);
	}

	public static void Free(WIN32_FIND_DATA_UNMANAGED unmanaged)
	{
		throw new NotImplementedException();
	}
}