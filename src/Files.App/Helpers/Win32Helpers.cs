using Files.App.Shell;
using Files.Shared.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Helpers
{
    public static class Win32Helpers
    {
        public static async Task<bool> InvokeWin32ComponentAsync(string applicationPath,
                                                                 string workingDirectory,
                                                                 string? arguments = null,
                                                                 bool runAsAdmin = false)
        {
            return await InvokeWin32ComponentsAsync(applicationPath.CreateEnumerable(), workingDirectory, arguments, runAsAdmin);
        }

        public static async Task<bool> InvokeWin32ComponentsAsync(IEnumerable<string> applicationPaths,
                                                                  string workingDirectory,
                                                                  string? arguments = null,
                                                                  bool runAsAdmin = false)
        {
            Debug.WriteLine("Launching EXE in FullTrustProcess");

            if (string.IsNullOrEmpty(workingDirectory))
            {
                return await Task.FromResult(false);
            }

            var application = applicationPaths.FirstOrDefault();

            if (runAsAdmin)
            {
                return await LaunchHelper.LaunchAppAsync(application, "runas", workingDirectory);
            }
            else
            {
                return await LaunchHelper.LaunchAppAsync(application, arguments, workingDirectory);
            }
        }
    }
}