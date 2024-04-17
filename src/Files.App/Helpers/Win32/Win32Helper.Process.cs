// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper for Win32.
	/// </summary>
	public static partial class Win32Helper
	{
		public static async Task<bool> InvokeWin32ComponentAsync(string applicationPath, IShellPage associatedInstance, string arguments = null, bool runAsAdmin = false, string workingDirectory = null)
		{
			return await InvokeWin32ComponentsAsync(applicationPath.CreateEnumerable(), associatedInstance, arguments, runAsAdmin, workingDirectory);
		}

		public static async Task<bool> InvokeWin32ComponentsAsync(IEnumerable<string> applicationPaths, IShellPage associatedInstance, string arguments = null, bool runAsAdmin = false, string workingDirectory = null)
		{
			Debug.WriteLine("Launching EXE in FullTrustProcess");

			if (string.IsNullOrEmpty(workingDirectory))
			{
				workingDirectory = associatedInstance.FilesystemViewModel.WorkingDirectory;
			}

			var application = applicationPaths.FirstOrDefault();
			if (string.IsNullOrEmpty(workingDirectory))
			{
				workingDirectory = associatedInstance?.FilesystemViewModel?.WorkingDirectory;
			}

			if (runAsAdmin)
			{
				return await LaunchHelper.LaunchAppAsync(application, "RunAs", workingDirectory);
			}
			else
			{
				return await LaunchHelper.LaunchAppAsync(application, arguments, workingDirectory);
			}
		}

		/// <summary>
		/// Gets process(es) that have a lock on the specified file.
		/// </summary>
		/// <param name="path">Path of the file.</param>
		/// <returns>Processes locking the file.</returns>
		/// <remarks>
		/// For more info, visit
		/// <br/>
		/// - <a href="https://learn.microsoft.com/ja-jp/windows/win32/api/restartmanager/nf-restartmanager-rmgetlist"/>
		/// <br/>
		/// - <a href="https://stackoverflow.com/questions/317071/how-do-i-find-out-which-process-is-locking-a-file-using-net/317209#317209"/>
		/// </remarks>
		public static List<Process> WhoIsLocking(string[] resources)
		{
			string key = Guid.NewGuid().ToString();
			List<Process> processes = [];

			int res = Win32PInvoke.RmStartSession(out uint handle, 0, key);
			if (res != 0) throw new Exception("Could not begin restart session.  Unable to determine file locker.");

			try
			{
				const int ERROR_MORE_DATA = 234;
				uint pnProcInfo = 0;
				uint lpdwRebootReasons = Win32PInvoke.RmRebootReasonNone;

				res = Win32PInvoke.RmRegisterResources(handle, (uint)resources.Length, resources, 0, null, 0, null);

				if (res != 0) throw new Exception("Could not register resource.");

				// Note:
				//  There's a race condition here -- the first call to RmGetList() returns the total number of process.
				//  However, when we call RmGetList() again to get the actual processes this number may have increased.
				res = Win32PInvoke.RmGetList(handle, out uint pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);

				if (res == ERROR_MORE_DATA)
				{
					// Create an array to store the process results
					var processInfo = new Win32PInvoke.RM_PROCESS_INFO[pnProcInfoNeeded];
					pnProcInfo = pnProcInfoNeeded;

					// Get the list
					res = Win32PInvoke.RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);
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
				_ = Win32PInvoke.RmEndSession(handle);
			}

			return processes;
		}
	}
}
