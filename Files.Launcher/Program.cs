using Files.Common;
using FilesFullTrust.MessageHandlers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
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

                // Wait until the connection gets closed
                appServiceExit.WaitOne();
            }
            finally
            {
                messageHandlers.ForEach(mh => mh.Dispose());
                deviceWatcher?.Dispose();
                connection?.Dispose();
                appServiceExit?.Dispose();
            }
        }

        public static string ReadFileTag(string filePath)
        {
            using var hStream = Kernel32.CreateFile($"{filePath}:files",
                Kernel32.FileAccess.GENERIC_READ, 0, null, FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
            if (hStream.IsInvalid) return null;
            var bytes = new byte[4096];
            var ret = Kernel32.ReadFile(hStream, bytes, (uint)bytes.Length, out var read, IntPtr.Zero);
            if (!ret) return null;
            return System.Text.Encoding.UTF8.GetString(bytes, 0, (int)read);
        }

        private static void UpdateTagsDb()
        {
            string FileTagsDbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "filetags.db");
            using var dbInstance = new Common.FileTagsDb(FileTagsDbPath, true);
            foreach (var file in dbInstance.GetAll())
            {
                var pathFromFrn = Win32API.PathFromFileId(file.Frn ?? 0, file.FilePath);
                if (pathFromFrn != null)
                {
                    // Frn is valid, update file path
                    var tag = ReadFileTag(pathFromFrn.Replace(@"\\?\", ""));
                    if (tag != null)
                    {
                        dbInstance.UpdateTag(file.Frn ?? 0, null, pathFromFrn.Replace(@"\\?\", ""));
                        dbInstance.SetTag(pathFromFrn.Replace(@"\\?\", ""), file.Frn, tag);
                    }
                    else
                    {
                        dbInstance.SetTag(null, file.Frn, null);
                    }
                }
                else
                {
                    var tag = ReadFileTag(file.FilePath);
                    if (tag != null)
                    {
                        using var si = new ShellItem(file.FilePath);
                        var frn = si.Properties["System.FileFRN"];
                        dbInstance.UpdateTag(file.FilePath, (ulong)frn, null);
                        dbInstance.SetTag(file.FilePath, (ulong)frn, tag);
                    }
                    else
                    {
                        dbInstance.SetTag(file.FilePath, null, null);
                    }
                }
            }
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Logger.UnhandledError(exception, exception.Message);
        }

        private static NamedPipeServerStream connection;
        private static ManualResetEvent appServiceExit;
        private static DeviceWatcher deviceWatcher;
        private static List<IMessageHandler> messageHandlers;

        private static async void InitializeAppServiceConnection()
        {
            connection = new NamedPipeServerStream($@"FilesInteropService_ServerPipe", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 2048, 2048, null, HandleInheritability.None, PipeAccessRights.ChangePermissions);

            PipeSecurity Security = connection.GetAccessControl();
            PipeAccessRule ClientRule = new PipeAccessRule(new SecurityIdentifier("S-1-15-2-1"), PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow);
            PipeAccessRule OwnerRule = new PipeAccessRule(WindowsIdentity.GetCurrent().Owner, PipeAccessRights.FullControl, AccessControlType.Allow);
            Security.AddAccessRule(ClientRule);
            Security.AddAccessRule(OwnerRule);
            if (IsAdministrator())
            {
                PipeAccessRule EveryoneRule = new PipeAccessRule(new SecurityIdentifier("S-1-1-0"), PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow);
                Security.AddAccessRule(EveryoneRule); // TODO: find the minimum permission to allow connection when admin
            }
            connection.SetAccessControl(Security);

            try
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(10));
                await connection.WaitForConnectionAsync(cts.Token);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not initialize pipe!");
            }

            if (connection.IsConnected)
            {
                var info = (Buffer: new byte[connection.InBufferSize], Message: new StringBuilder());
                BeginRead(info);
            }
            else
            {
                appServiceExit.Set();
            }
        }

        private static void BeginRead((byte[] Buffer, StringBuilder Message) info)
        {
            var isConnected = connection.IsConnected;
            if (isConnected)
            {
                try
                {
                    connection.BeginRead(info.Buffer, 0, info.Buffer.Length, EndReadCallBack, info);
                }
                catch
                {
                    isConnected = false;
                }
            }
            if (!isConnected)
            {
                appServiceExit.Set();
            }
        }

        private static void EndReadCallBack(IAsyncResult result)
        {
            var info = ((byte[] Buffer, StringBuilder Message))result.AsyncState;
            var readBytes = connection.EndRead(result);
            if (readBytes > 0)
            {
                // Get the read bytes and append them
                info.Message.Append(Encoding.UTF8.GetString(info.Buffer, 0, readBytes));

                if (connection.IsMessageComplete) // Message is completed
                {
                    var message = info.Message.ToString().TrimEnd('\0');

                    Connection_RequestReceived(connection, JsonConvert.DeserializeObject<Dictionary<string, object>>(message));

                    // Begin a new reading operation
                    var nextInfo = (Buffer: new byte[connection.InBufferSize], Message: new StringBuilder());
                    BeginRead(nextInfo);

                    return;
                }
            }
            BeginRead(info);
        }

        private static async void Connection_RequestReceived(NamedPipeServerStream conn, Dictionary<string, object> message)
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

                await ParseArgumentsAsync(message, arguments);
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
                    appServiceExit.Set();
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
                            appServiceExit.Set();
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

                if (arguments == "ShellCommand")
                {
                    // Kill the process. This is a BRUTAL WAY to kill a process.
#if DEBUG
                    // In debug mode this kills this process too??
#else
                    var pid = (int)localSettings.Values["pid"];
                    Process.GetProcessById(pid).Kill();
#endif

                    using Process process = new Process();
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.FileName = "explorer.exe";
                    process.StartInfo.CreateNoWindow = false;
                    process.StartInfo.Arguments = (string)localSettings.Values["ShellCommand"];
                    process.Start();

                    return true;
                }
            }
            return false;
        }
    }
}