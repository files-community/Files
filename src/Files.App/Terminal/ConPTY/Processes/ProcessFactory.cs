using System;
using System.Runtime.InteropServices;
using static Files.App.Terminal.Native.ProcessApi;

namespace Files.App.Terminal
{
	/// <summary>
	/// Support for starting and configuring processes.
	/// </summary>
	/// <remarks>
	/// Possible to replace with managed code? The key is being able to provide the PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE attribute
	/// </remarks>
	static class ProcessFactory
	{
		/// <summary>
		/// Start and configure a process. The return value represents the process and should be disposed.
		/// </summary>
		internal static Process Start(string command, string directory, IntPtr attributes, IntPtr hPC)
		{
			var startupInfo = ConfigureProcessThread(hPC, attributes);
			var processInfo = RunProcess(ref startupInfo, command, directory);
			return new Process(startupInfo, processInfo);
		}

		private static STARTUPINFOEX ConfigureProcessThread(IntPtr hPC, IntPtr attributes)
		{
			// this method implements the behavior described in https://docs.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session#preparing-for-creation-of-the-child-process

			var lpSize = IntPtr.Zero;
			var success = InitializeProcThreadAttributeList(
				lpAttributeList: IntPtr.Zero,
				dwAttributeCount: 1,
				dwFlags: 0,
				lpSize: ref lpSize
			);
			if (success || lpSize == IntPtr.Zero) // we're not expecting `success` here, we just want to get the calculated lpSize
			{
				throw new InvalidOperationException("Could not calculate the number of bytes for the attribute list. " + Marshal.GetLastWin32Error());
			}

			var startupInfo = new STARTUPINFOEX();
			startupInfo.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();
			startupInfo.lpAttributeList = Marshal.AllocHGlobal(lpSize);

			success = InitializeProcThreadAttributeList(
				lpAttributeList: startupInfo.lpAttributeList,
				dwAttributeCount: 1,
				dwFlags: 0,
				lpSize: ref lpSize
			);
			if (!success)
			{
				throw new InvalidOperationException("Could not set up attribute list. " + Marshal.GetLastWin32Error());
			}

			success = UpdateProcThreadAttribute(
				lpAttributeList: startupInfo.lpAttributeList,
				dwFlags: 0,
				attribute: attributes,
				lpValue: hPC,
				cbSize: (IntPtr)IntPtr.Size,
				lpPreviousValue: IntPtr.Zero,
				lpReturnSize: IntPtr.Zero
			);
			if (!success)
			{
				throw new InvalidOperationException("Could not set pseudoconsole thread attribute. " + Marshal.GetLastWin32Error());
			}

			return startupInfo;
		}

		private static PROCESS_INFORMATION RunProcess(ref STARTUPINFOEX sInfoEx, string commandLine, string directory)
		{
			int securityAttributeSize = Marshal.SizeOf<SECURITY_ATTRIBUTES>();
			var pSec = new SECURITY_ATTRIBUTES { nLength = securityAttributeSize };
			var tSec = new SECURITY_ATTRIBUTES { nLength = securityAttributeSize };
			var success = CreateProcess(
				lpApplicationName: null,
				lpCommandLine: commandLine,
				lpProcessAttributes: ref pSec,
				lpThreadAttributes: ref tSec,
				bInheritHandles: false,
				dwCreationFlags: EXTENDED_STARTUPINFO_PRESENT,
				lpEnvironment: IntPtr.Zero,
				lpCurrentDirectory: directory,
				lpStartupInfo: ref sInfoEx,
				lpProcessInformation: out PROCESS_INFORMATION pInfo
			);
			if (!success)
			{
				throw new InvalidOperationException("Could not create process. " + Marshal.GetLastWin32Error());
			}

			return pInfo;
		}
	}
}
