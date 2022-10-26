using Files.Shared.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace Files.App.Helpers
{
    public static class AppServiceConnectionHelper
    {
        private static readonly JsonElement defaultJson = JsonSerializer.SerializeToElement("{}");

        public static Task<NamedPipeAsAppServiceConnection> Instance = BuildConnection(true);

        public static event EventHandler<Task<NamedPipeAsAppServiceConnection>> ConnectionChanged;

        public static async Task<bool> Elevate(this NamedPipeAsAppServiceConnection connection)
        {
            if (connection == null)
            {
                App.AppModel.IsAppElevated = false;
                return false;
            }

            bool wasElevated = false;

            var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet() { { "Arguments", "Elevate" } });
            if (status == AppServiceResponseStatus.Success)
            {
                var res = response.Get("Success", defaultJson).GetInt64();
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

            App.AppModel.IsAppElevated = wasElevated;

            return wasElevated;
        }

        private static async Task<NamedPipeAsAppServiceConnection> BuildConnection(bool launchFullTrust)
        {
            try
            {
                if (launchFullTrust)
                {
                    var ftpPath = Path.Combine(Package.Current.InstalledLocation.Path, "Files.FullTrust", "FilesFullTrust.exe");
                    System.Diagnostics.Process.Start(ftpPath);
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
        private readonly JsonElement defaultJson = JsonSerializer.SerializeToElement("{}");

        private NamedPipeServerStream serverStream;

        public event EventHandler<Dictionary<string, JsonElement>> RequestReceived;

        public event EventHandler ServiceClosed;

        private ConcurrentDictionary<string, TaskCompletionSource<Dictionary<string, JsonElement>>> messageList;

        public NamedPipeAsAppServiceConnection()
        {
            this.messageList = new ConcurrentDictionary<string, TaskCompletionSource<Dictionary<string, JsonElement>>>();
        }

        private async Task BeginRead(NamedPipeServerStream serverStream)
        {
            using var memoryStream = new MemoryStream();
            var buffer = new byte[serverStream.InBufferSize];

            try
            {
                while (serverStream.IsConnected)
                {
                    var readCount = await serverStream.ReadAsync(buffer, 0, buffer.Length);
                    memoryStream.Write(buffer, 0, readCount);
                    if (serverStream.IsMessageComplete)
                    {
                        var message = Encoding.UTF8.GetString(memoryStream.ToArray()).TrimEnd('\0');
                        var msg = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message);
                        if (msg != null && msg.Get("RequestID", defaultJson).GetString() == null)
                        {
                            RequestReceived?.Invoke(this, msg);
                        }
                        else if (msg != null && messageList.TryRemove(msg["RequestID"].GetString(), out var tcs))
                        {
                            tcs.TrySetResult(msg);
                        }

                        memoryStream.SetLength(0);
                    }
                }
            }
            catch
            {
            }
        }

        public async Task<bool> Connect(string pipeName, TimeSpan timeout = default)
        {
            serverStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 2048, 2048);

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(timeout);
            await serverStream.WaitForConnectionAsync(cts.Token);

            _ = BeginRead(serverStream);

            return true;
        }

        public async Task<(AppServiceResponseStatus Status, Dictionary<string, JsonElement> Data)> SendMessageForResponseAsync(ValueSet valueSet)
        {
            if (serverStream == null)
            {
                return (AppServiceResponseStatus.Failure, null);
            }

            try
            {
                var guid = Guid.NewGuid().ToString();
                valueSet.Add("RequestID", guid);
                var tcs = new TaskCompletionSource<Dictionary<string, JsonElement>>();
                messageList.TryAdd(guid, tcs);
                var serialized = JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>(valueSet));
                await serverStream.WriteAsync(serialized, 0, serialized.Length);
                var response = await tcs.Task;

                return (AppServiceResponseStatus.Success, response);
            }
            catch (System.IO.IOException)
            {
                // Pipe is disconnected
                ServiceClosed?.Invoke(this, EventArgs.Empty);
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
                var serialized = JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>(valueSet));
                await serverStream.WriteAsync(serialized, 0, serialized.Length);
                return AppServiceResponseStatus.Success;
            }
            catch (System.IO.IOException)
            {
                // Pipe is disconnected
                ServiceClosed?.Invoke(this, EventArgs.Empty);
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
