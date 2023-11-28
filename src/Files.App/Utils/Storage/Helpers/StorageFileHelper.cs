// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.Storage
{
	/// <summary>
	/// Provides static helper for storable file item.
	/// </summary>
	/// <remarks>
	/// For more information, visit
	/// </remarks>
	public static class StorageFileHelper
	{
		/// <summary>
		/// Finds what process(es) have a lock on the specified file.
		/// </summary>
		/// <param name="path">Path of the file.</param>
		/// <returns>A list of process(es) locking the specific file</returns>
		/// <remarks>
		/// For more information, visit
		/// <br/>
		/// - <a href="https://stackoverflow.com/questions/317071/how-do-i-find-out-which-process-is-locking-a-file-using-net/317209#317209"/>
		/// <br/>
		/// - <a href="http://msdn.microsoft.com/library/windows/desktop/aa373661(v=vs.85).aspx"/>
		/// </remarks>
		public static List<Process> WhoIsLocking(string[] resources)
		{
			string key = Guid.NewGuid().ToString();
			List<Process> processes = new();

			int res = Core.Helpers.Win32Interop.RmStartSession(out uint handle, 0, key);
			if (res != 0) throw new Exception("Could not begin restart session.  Unable to determine file locker.");

			try
			{
				const int ERROR_MORE_DATA = 234;
				uint pnProcInfo = 0;
				uint lpdwRebootReasons = Core.Helpers.Win32Interop.RmRebootReasonNone;

				res = Core.Helpers.Win32Interop.RmRegisterResources(handle, (uint)resources.Length, resources, 0, null, 0, null);

				if (res != 0) throw new Exception("Could not register resource.");

				//Note:
				//  there's a race condition here. the first call to RmGetList() returns the total number of process.
				//  However, when we call RmGetList() again to get the actual processes this number may have increased.
				res = Core.Helpers.Win32Interop.RmGetList(handle, out uint pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);

				if (res == ERROR_MORE_DATA)
				{
					// Create an array to store the process results
					Core.Helpers.Win32Interop.RM_PROCESS_INFO[] processInfo = new Core.Helpers.Win32Interop.RM_PROCESS_INFO[pnProcInfoNeeded];
					pnProcInfo = pnProcInfoNeeded;

					// Get the list
					res = Core.Helpers.Win32Interop.RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);
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
				_ = Core.Helpers.Win32Interop.RmEndSession(handle);
			}

			return processes;
		}
	}
}
