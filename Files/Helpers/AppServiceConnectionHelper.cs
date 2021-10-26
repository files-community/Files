using Files.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web;
using Windows.Storage;

namespace Files.Helpers
{
    public static class AppServiceConnectionHelper
    {
        public static Task<NamedPipeAsAppServiceConnection> Instance = BuildConnection(true);

        public static event EventHandler<Task<NamedPipeAsAppServiceConnection>> ConnectionChanged;

        public static void Register()
        {
            App.Current.Suspending += OnSuspending;
            App.Current.LeavingBackground += OnLeavingBackground;
        }

        private static async void OnLeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            if (await Instance == null)
            {
                // Need to reinitialize AppService when app is resuming
                Instance = BuildConnection(true);
                ConnectionChanged?.Invoke(null, Instance);
                if (App.MainViewModel != null)
                {
                    App.MainViewModel.IsFullTrustElevated = false;
                }
            }
        }

        private async static void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            var nullConn = Task.FromResult<NamedPipeAsAppServiceConnection>(null);
            ConnectionChanged?.Invoke(null, nullConn);
            (await Instance)?.SendMessageAsync(new ValueSet() { { "Arguments", "Terminate" } });
            (await Instance)?.Dispose();
            Instance = nullConn;
            deferral.Complete();
        }

        public static async Task<bool> Elevate(this NamedPipeAsAppServiceConnection connection)
        {
            if (connection == null)
            {
                App.MainViewModel.IsFullTrustElevated = false;
                return false;
            }

            bool wasElevated = false;

            var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet() { { "Arguments", "Elevate" } });
            if (status == AppServiceResponseStatus.Success)
            {
                var res = response.Get("Success", 1L);
                switch (res)
                {
                    case 0: // FTP is restarting as admin
                        var nullConn = Task.FromResult<NamedPipeAsAppServiceConnection>(null);
                        ConnectionChanged?.Invoke(null, nullConn);
                        (await Instance)?.Dispose();
                        Instance = BuildConnection(false); // Fulltrust process is already running
                        _ = await Instance;
                        ConnectionChanged?.Invoke(null, Instance);
                        wasElevated = true;
                        break;

                    case -1: // FTP is already admin
                        wasElevated = true;
                        break;

                    default: // Failed (e.g canceled UAC)
                        wasElevated = false;
                        break;
                }
            }

            App.MainViewModel.IsFullTrustElevated = wasElevated;

            return wasElevated;
        }

        private static async Task<NamedPipeAsAppServiceConnection> BuildConnection(bool launchFullTrust)
        {
            try
            {
                if (launchFullTrust)
                {
                    // Launch fulltrust process
                    ApplicationData.Current.LocalSettings.Values["PackageSid"] =
                        WebAuthenticationBroker.GetCurrentApplicationCallbackUri().Host.ToUpper();
                    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                }

                var connection = new NamedPipeAsAppServiceConnection();
                if (await connection.Connect(@"LOCAL\FilesInteropService_ServerPipe", TimeSpan.FromSeconds(15)))
                {
                    return connection;
                }
                connection.Dispose();
            }
            catch (Exception ex)
            {
                App.Logger.Warn(ex, "Could not initialize FTP connection!");
            }
            return null;
        }
    }

    public class NamedPipeAsAppServiceConnection : IDisposable
    {
        private NamedPipeServerStream serverStream;

        public event EventHandler<Dictionary<string, object>> RequestReceived;

        public event EventHandler ServiceClosed;

        private ConcurrentDictionary<string, TaskCompletionSource<Dictionary<string, object>>> messageList;

        public NamedPipeAsAppServiceConnection()
        {
            this.messageList = new ConcurrentDictionary<string, TaskCompletionSource<Dictionary<string, object>>>();
        }

        private void BeginRead((byte[] Buffer, StringBuilder Message) info)
        {
            serverStream?.BeginRead(info.Buffer, 0, info.Buffer.Length, EndReadCallBack, info);
        }

        private void EndReadCallBack(IAsyncResult result)
        {
            var readBytes = serverStream?.EndRead(result) ?? 0;
            if (readBytes > 0)
            {
                var info = ((byte[] Buffer, StringBuilder Message))result.AsyncState;

                // Get the read bytes and append them
                info.Message.Append(Encoding.UTF8.GetString(info.Buffer, 0, readBytes));

                if (!serverStream.IsMessageComplete) // Message is not complete, continue reading
                {
                    BeginRead(info);
                }
                else // Message is completed
                {
                    var message = info.Message.ToString().TrimEnd('\0');

                    var msg = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);
                    if (msg.Get("RequestID", (string)null) == null)
                    {
                        RequestReceived?.Invoke(this, msg);
                    }
                    else
                    {
                        if (messageList.TryRemove((string)msg["RequestID"], out var tcs))
                        {
                            tcs.TrySetResult(msg);
                        }
                    }

                    // Begin a new reading operation
                    var nextInfo = (Buffer: new byte[serverStream?.InBufferSize ?? 0], Message: new StringBuilder());
                    BeginRead(nextInfo);
                }
            }
        }

        public async Task<bool> Connect(string pipeName, TimeSpan timeout = default)
        {
            serverStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 2048, 2048);

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(timeout);
            await serverStream.WaitForConnectionAsync(cts.Token);

            var info = (Buffer: new byte[serverStream.InBufferSize], Message: new StringBuilder());
            BeginRead(info);

            return true;
        }

        public async Task<(AppServiceResponseStatus Status, Dictionary<string, object> Data)> SendMessageForResponseAsync(ValueSet valueSet)
        {
            if (serverStream == null)
            {
                return (AppServiceResponseStatus.Failure, null);
            }

            try
            {
                var guid = Guid.NewGuid().ToString();
                valueSet.Add("RequestID", guid);
                var tcs = new TaskCompletionSource<Dictionary<string, object>>();
                messageList.TryAdd(guid, tcs);
                var serialized = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Dictionary<string, object>(valueSet)));
                await serverStream.WriteAsync(serialized, 0, serialized.Length);
                var response = await tcs.Task;

                return (AppServiceResponseStatus.Success, response);
            }
            catch (System.IO.IOException)
            {
                // Pipe is disconnected
                ServiceClosed?.Invoke(this, null);
                this.Cleanup();
            }
            catch (Exception ex)
            {
                App.Logger.Warn(ex, "Error sending request on pipe.");
            }

            return (AppServiceResponseStatus.Failure, null);
        }

        public async Task<AppServiceResponseStatus> SendMessageAsync(ValueSet valueSet)
        {
            if (serverStream == null)
            {
                return AppServiceResponseStatus.Failure;
            }

            try
            {
                var guid = Guid.NewGuid().ToString();
                valueSet.Add("RequestID", guid);
                var serialized = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Dictionary<string, object>(valueSet)));
                await serverStream.WriteAsync(serialized, 0, serialized.Length);
                return AppServiceResponseStatus.Success;
            }
            catch (System.IO.IOException)
            {
                // Pipe is disconnected
                ServiceClosed?.Invoke(this, null);
                this.Cleanup();
            }
            catch (Exception ex)
            {
                App.Logger.Warn(ex, "Error sending request on pipe.");
            }

            return AppServiceResponseStatus.Failure;
        }

        public void Cleanup()
        {
            foreach (var m in messageList)
            {
                m.Value.TrySetCanceled();
            }
            messageList.Clear();
            serverStream?.Dispose();
            serverStream = null;
        }

        public void Dispose()
        {
            this.Cleanup();
        }
    }
}
