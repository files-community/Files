using System;
using System.Runtime.InteropServices;

namespace Files.App.Utils.Terminal.ConPTY
{
	/// <summary>
	/// PInvoke signatures for win32 process api
	/// </summary>
	static class ProcessApi
	{
		internal const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct STARTUPINFOEX
		{
			public STARTUPINFO StartupInfo;
			public nint lpAttributeList;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct STARTUPINFO
		{
			public int cb;
			public string lpReserved;
			public string lpDesktop;
			public string lpTitle;
			public int dwX;
			public int dwY;
			public int dwXSize;
			public int dwYSize;
			public int dwXCountChars;
			public int dwYCountChars;
			public int dwFillAttribute;
			public int dwFlags;
			public short wShowWindow;
			public short cbReserved2;
			public nint lpReserved2;
			public nint hStdInput;
			public nint hStdOutput;
			public nint hStdError;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct PROCESS_INFORMATION
		{
			public nint hProcess;
			public nint hThread;
			public int dwProcessId;
			public int dwThreadId;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct SECURITY_ATTRIBUTES
		{
			public int nLength;
			public nint lpSecurityDescriptor;
			public int bInheritHandle;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool InitializeProcThreadAttributeList(
			nint lpAttributeList, int dwAttributeCount, int dwFlags, ref nint lpSize);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool UpdateProcThreadAttribute(
			nint lpAttributeList, uint dwFlags, nint attribute, nint lpValue,
			nint cbSize, nint lpPreviousValue, nint lpReturnSize);

		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool CreateProcess(
			string lpApplicationName, string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes,
			ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags,
			nint lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFOEX lpStartupInfo,
			out PROCESS_INFORMATION lpProcessInformation);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool DeleteProcThreadAttributeList(nint lpAttributeList);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool CloseHandle(nint hObject);
	}
}
