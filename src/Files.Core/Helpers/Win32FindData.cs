using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices.Marshalling;

namespace Files.Core.Helpers;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
[NativeMarshalling(typeof(Win32FindDataMarshaller))]
public struct WIN32_FIND_DATA
{
	public uint dwFileAttributes;

	public FILETIME ftCreationTime;
	public FILETIME ftLastAccessTime;
	public FILETIME ftLastWriteTime;
	public uint nFileSizeHigh;
	public uint nFileSizeLow;
	public uint dwReserved0;
	public uint dwReserved1;
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
	public string cFileName;
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
	public string cAlternateFileName;
}