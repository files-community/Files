using Files.Common;
using FilesFullTrust.MessageHandlers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace FilesFullTrust
{
    internal class Program
    {
        public static Logger Logger { get; private set; }
        private static readonly LogWriter logWriter = new LogWriter();

        [STAThread]
        private static async Task Main(string[] args)
        {
            Logger = new Logger(logWriter);
            await logWriter.InitializeAsync("debug_fulltrust.log");
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

            if (HandleCommandLineArgs())
            {
                // Handles OpenShellCommandInExplorer
                return;
            }

            try
            {
                // Create message handlers
                messageHandlers = new List<IMessageHandler>();
                messageHandlers.Add(new RecycleBinHandler());
                messageHandlers.Add(new LibrariesHandler());
                messageHandlers.Add(new FileTagsHandler());
                messageHandlers.Add(new ApplicationLaunchHandler());
                messageHandlers.Add(new NetworkDrivesHandler());
                messageHandlers.Add(new FileOperationsHandler());
                messageHandlers.Add(new ContextMenuHandler());
                messageHandlers.Add(new QuickLookHandler());
                messageHandlers.Add(new Win32MessageHandler());

                // Connect to app service and wait until the connection gets closed
                appServiceExit = new ManualResetEvent(false);
                InitializeAppServiceConnection();

                // Initialize message handlers
                messageHandlers.ForEach(mh => mh.Initialize(connection));

                // Initialize device watcher
                deviceWatcher = new DeviceWatcher(connection);
                deviceWatcher.Start();

                // Update tags db
                messageHandlers.OfType<FileTagsHandler>().Single().UpdateTagsDb();

                // Wait until the connection gets closed
                appServiceExit.WaitOne();

                // Wait for ongoing file operations
                messageHandlers.OfType<FileOperationsHandler>().Single().WaitForCompletion();
            }
            finally
            {
                messageHandlers.ForEach(mh => mh.Dispose());
                deviceWatcher?.Dispose();
                connection?.Dispose();
                appServiceExit?.Dispose();
                appServiceExit = null;
            }
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Logger.UnhandledError(exception, exception.Message);
        }

        private static NamedPipeClientStream connection;
        private static ManualResetEvent appServiceExit;
        private static DeviceWatcher deviceWatcher;
        private static List<IMessageHandler> messageHandlers;

        private static async void InitializeAppServiceConnection()
        {
            var packageSid = ApplicationData.Current.LocalSettings.Values["PackageSid"];
            connection = new NamedPipeClientStream(".",
                $"Sessions\\{Process.GetCurrentProcess().SessionId}\\AppContainerNamedObjects\\{packageSid}\\FilesInteropService_ServerPipe",
                PipeDirection.InOut, PipeOptions.Asynchronous);

            try
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(15));
                await connection.ConnectAsync(cts.Token);
                connection.ReadMode = PipeTransmissionMode.Message;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not initialize pipe!");
            }

            await BeginRead();
            appServiceExit?.Set();
        }

        private static async Task BeginRead()
        {
            try
            {
                using var memoryStream = new MemoryStream();
                var buffer = new byte[connection.InBufferSize];
                while (connection.IsConnected)
                {
                    var readCount = await connection.ReadAsync(buffer);
                    memoryStream.Write(buffer, 0, readCount);
                    if (connection.IsMessageComplete)
                    {
                        var message = Encoding.UTF8.GetString(memoryStream.ToArray()).TrimEnd('\0');
                        OnConnectionRequestReceived(JsonConvert.DeserializeObject<Dictionary<string, object>>(message));
                        memoryStream.SetLength(0);
                    }
                }
            }
            catch
            {
            }
        }

        private static async void OnConnectionRequestReceived(Dictionary<string, object> message)
        {
            // Get a deferral because we use an awaitable API below to respond to the message
            // and we don't want this call to get cancelled while we are waiting.
            if (message == null)
            {
                return;
            }

            if (message.ContainsKey("Arguments"))
            {
                // This replaces launching the fulltrust process with arguments
                // Instead a single instance of the process is running
                // Requests from UWP app are sent via AppService connection
                var arguments = (string)message["Arguments"];
                Logger.Info($"Argument: {arguments}");

                await Extensions.IgnoreExceptions(async () =>
                {
                    await Task.Run(() => ParseArgumentsAsync(message, arguments));
                }, Logger);
            }
        }

        private static bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static async Task ParseArgumentsAsync(Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "Terminate":
                    // Exit fulltrust process (UWP is closed or suspended)
                    appServiceExit?.Set();
                    break;

                case "Elevate":
                    // Relaunch fulltrust process as admin
                    if (!IsAdministrator())
                    {
                        try
                        {
                            using (Process elevatedProcess = new Process())
                            {
                                elevatedProcess.StartInfo.Verb = "runas";
                                elevatedProcess.StartInfo.UseShellExecute = true;
                                elevatedProcess.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                                elevatedProcess.StartInfo.Arguments = "elevate";
                                elevatedProcess.Start();
                            }
                            await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", 0 } }, message.Get("RequestID", (string)null));
                            appServiceExit?.Set();
                        }
                        catch (Win32Exception)
                        {
                            // If user cancels UAC
                            await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", 1 } }, message.Get("RequestID", (string)null));
                        }
                    }
                    else
                    {
                        await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", -1 } }, message.Get("RequestID", (string)null));
                    }
                    break;

                default:
                    foreach (var mh in messageHandlers)
                    {
                        await mh.ParseArgumentsAsync(connection, message, arguments);
                    }
                    break;
            }
        }

        private static bool HandleCommandLineArgs()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var arguments = (string)localSettings.Values["Arguments"];
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                localSettings.Values.Remove("Arguments");

                if (arguments == "StartUwp")
                {
                    var folder = localSettings.Values.Get("Folder", "");
                    localSettings.Values.Remove("Folder");

                    using Process process = new Process();
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.FileName = "files.exe";
                    process.StartInfo.Arguments = folder;
                    process.Start();

                    TerminateProcess((int)localSettings.Values["pid"]);
                    return true;
                }
                else if (arguments == "TerminateUwp")
                {
                    TerminateProcess((int)localSettings.Values["pid"]);
                    return true;
                }
                else if (arguments == "ShellCommand")
                {
                    TerminateProcess((int)localSettings.Values["pid"]);

                    Win32API.OpenFolderInExistingShellWindow((string)localSettings.Values["ShellCommand"]);

                    return true;
                }
            }

            return false;
        }

        private static void TerminateProcess(int processId)
        {
            // Kill the process. This is a BRUTAL WAY to kill a process.
#if DEBUG
            // In debug mode this kills this process too??
#else
            Process.GetProcessById(processId).Kill();
#endif
        }
    }
}