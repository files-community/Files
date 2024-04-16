using Files.Shared.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace Files.Uwp.Helpers
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

            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var value = new ValueSet()
                {
                    { "Arguments", "LaunchApp" },
                    { "WorkingDirectory", string.IsNullOrEmpty(workingDirectory) ? associatedInstance?.FilesystemViewModel?.WorkingDirectory : workingDirectory },
                    { "Application", applicationPaths.FirstOrDefault() }
                };

                if (runAsAdmin)
                {
                    value.Add("Parameters", "runas");
                }
                else
                {
                    value.Add("Parameters", arguments);
                }

                return await connection.SendMessageAsync(value) == AppServiceResponseStatus.Success;
            }

            return false;
        }
    }
}