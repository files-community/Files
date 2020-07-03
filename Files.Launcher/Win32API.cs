using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace FilesFullTrust
{
    internal static class Win32API
    {
        public static Task<T> StartSTATask<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            Thread thread = new Thread(() =>
            {
                try
                {
                    tcs.SetResult(func());
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Ansi)]
        public static extern IntPtr FindExecutable(string lpFile, string lpDirectory, [Out] StringBuilder lpResult);

        public static async Task<string> GetFileAssociation(string filename)
        {
            // Find UWP apps
            var uwp_apps = await Windows.System.Launcher.FindFileHandlersAsync(System.IO.Path.GetExtension(filename));
            if (uwp_apps.Any()) return uwp_apps.First().PackageFamilyName;
            // Find desktop apps
            var lpResult = new StringBuilder();
            var hResult = FindExecutable(filename, null, lpResult);
            if (hResult.ToInt64() > 32) return lpResult.ToString();
            return null;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int LoadString(IntPtr hInstance, int ID, StringBuilder lpBuffer, int nBufferMax);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        public static string ExtractStringFromDLL(string file, int number)
        {
            IntPtr lib = LoadLibrary(file);
            StringBuilder result = new StringBuilder(2048);
            LoadString(lib, number, result, result.Capacity);
            FreeLibrary(lib);
            return result.ToString();
        }

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        [DllImport("kernel32.dll")]
        public static extern IntPtr LocalFree(IntPtr hMem);

        public static string[] CommandLineToArgs(string commandLine)
        {
            if (String.IsNullOrEmpty(commandLine))
                return Array.Empty<string>();

            var argv = CommandLineToArgvW(commandLine, out int argc);
            if (argv == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception();
            try
            {
                var args = new string[argc];
                for (var i = 0; i < args.Length; i++)
                {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }

                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }

        public static void UnlockBitlockerDrive(string drive, string password)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.Verb = "runas";
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.Arguments = $"-command \"$SecureString = ConvertTo-SecureString '{password}' -AsPlainText -Force; Unlock-BitLocker -MountPoint '{drive}' -Password $SecureString\"";
                process.Start();
                process.WaitForExit(30 * 1000);
            }
            catch (Win32Exception)
            {
                // If user cancels UAC
            }
        }

        public static void Foreground(this Process process)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(2 * 1000);

            Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        User32.SetForegroundWindow(process.MainWindowHandle);
                        break;
                    }
                    await Task.Delay(100);
                }
            });
        }
    }
}
