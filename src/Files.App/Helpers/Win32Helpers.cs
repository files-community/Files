// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Shell;
using Files.Shared.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Helpers
{
	public static class Win32Helpers
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
	}
}