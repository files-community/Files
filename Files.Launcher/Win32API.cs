using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        public static Task StartSTATask(Action func)
        {
            var tcs = new TaskCompletionSource<object>();
            var thread = new Thread(() =>
            {
                try
                {
                    func();
                    tcs.SetResult(null);
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

        private class ThreadWithMessageQueue : System.Windows.Forms.Form
        {
            private Process process;

            public ThreadWithMessageQueue(Process process)
            {
                this.process = process;
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                this.ShowInTaskbar = false;
                this.Load += OnLoadEvent;
            }

            public async void OnLoadEvent(object sender, EventArgs args)
            {
                this.Size = new System.Drawing.Size(0, 0);
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(2 * 1000);

                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        process.Refresh();
                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            uint winId = Vanara.PInvoke.User32.GetWindowThreadProcessId(process.MainWindowHandle, out var _);
                            uint myId = Vanara.PInvoke.User32.GetWindowThreadProcessId(Handle, out var _);
                            uint foreId = Vanara.PInvoke.User32.GetWindowThreadProcessId(Vanara.PInvoke.User32.GetForegroundWindow(), out var _);
                            if (winId != myId)
                                Debug.WriteLine("Attached my->target: {0}", Vanara.PInvoke.User32.AttachThreadInput(myId, winId, true));
                            if (foreId != myId)
                                Debug.WriteLine("Attached my->fore: {0}", Vanara.PInvoke.User32.AttachThreadInput(myId, foreId, true));
                            if (winId != foreId)
                                Debug.WriteLine("Attached fore->target: {0}", Vanara.PInvoke.User32.AttachThreadInput(foreId, winId, true));
                            Vanara.PInvoke.User32.SetForegroundWindow(process.MainWindowHandle);
                            Vanara.PInvoke.User32.SetWindowPos(process.MainWindowHandle, Vanara.PInvoke.User32.SpecialWindowHandles.HWND_TOPMOST,
                                0, 0, 0, 0, Vanara.PInvoke.User32.SetWindowPosFlags.SWP_NOSIZE | Vanara.PInvoke.User32.SetWindowPosFlags.SWP_NOMOVE);
                            Vanara.PInvoke.User32.SetWindowPos(process.MainWindowHandle, Vanara.PInvoke.User32.SpecialWindowHandles.HWND_NOTOPMOST,
                                0, 0, 0, 0, Vanara.PInvoke.User32.SetWindowPosFlags.SWP_SHOWWINDOW | Vanara.PInvoke.User32.SetWindowPosFlags.SWP_NOSIZE | Vanara.PInvoke.User32.SetWindowPosFlags.SWP_NOMOVE);
                            if (winId != myId)
                                Vanara.PInvoke.User32.AttachThreadInput(myId, winId, false);
                            if (foreId != myId)
                                Vanara.PInvoke.User32.AttachThreadInput(myId, foreId, false);
                            if (winId != foreId)
                                Vanara.PInvoke.User32.AttachThreadInput(foreId, winId, false);
                            break;
                        }
                        await Task.Delay(100);
                    }
                    catch (InvalidOperationException)
                    {
                        break;
                    }
                }

                this.Close();
            }
        }

        public static void Foreground(this Process process)
        {
            StartSTATask(() => System.Windows.Forms.Application.Run(new ThreadWithMessageQueue(process)));
        }
    }
}
