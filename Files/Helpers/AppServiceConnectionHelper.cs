using Files.Common;
using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace Files.Helpers
{
    public static class AppServiceConnectionHelper
    {
        public static Task<NamedPipeAsAppServiceConnection> Instance = BuildConnection();

        public static event EventHandler<Task<NamedPipeAsAppServiceConnection>> ConnectionChanged;

        static AppServiceConnectionHelper()
        {
            App.Current.Suspending += OnSuspending;
            App.Current.LeavingBackground += OnLeavingBackground;
        }

        private static async void OnLeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            if (await Instance == null)
            {
                // Need to reinitialize AppService when app is resuming
                Instance = BuildConnection();
                ConnectionChanged?.Invoke(null, Instance);
            }
        }

        private async static void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            (await Instance)?.SendMessageAsync(new ValueSet() { { "Arguments", "Terminate" } });
            (await Instance)?.Dispose();
            Instance = Task.FromResult<NamedPipeAsAppServiceConnection>(null);
            ConnectionChanged?.Invoke(null, Instance);
            deferral.Complete();
        }

        private static async Task<NamedPipeAsAppServiceConnection> BuildConnection()
        {
            try
            {
                SafePipeHandle PipeHandle = null;
                bool firstLoop = true;

                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(30));
                while (true)
                {
                    IntPtr Handle = NativeFileOperationsHelper.CreateFileFromApp(@$"\\.\pipe\{"FilesInteropService_ServerPipe"}", NativeFileOperationsHelper.GENERIC_READ | NativeFileOperationsHelper.GENERIC_WRITE, 0, IntPtr.Zero, NativeFileOperationsHelper.OPEN_EXISTING, 0x40000000, IntPtr.Zero);

                    PipeHandle = new SafePipeHandle(Handle, true);

                    if (!PipeHandle.IsInvalid)
                    {
                        break;
                    }
                    else if (cts.Token.IsCancellationRequested)
                    {
                        return null;
                    }
                    else if (firstLoop)
                    {
                        firstLoop = false;
                        // Launch fulltrust process
                        await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                    }
                    await Task.Delay(200);
                }

                var ClientStream = new NamedPipeClientStream(PipeDirection.InOut, true, true, PipeHandle);
                ClientStream.ReadMode = PipeTransmissionMode.Message;

                return new NamedPipeAsAppServiceConnection(ClientStream, PipeHandle);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, "Could not initialize AppServiceConnection!");
                return null;
            }
        }
    }

    public class NamedPipeAsAppServiceConnection : IDisposable
    {
        private NamedPipeClientStream clientStream;
        private SafePipeHandle pipeHandle;

        public event EventHandler<Dictionary<string, object>> RequestReceived;

        private ConcurrentDictionary<string, TaskCompletionSource<Dictionary<string, object>>> messageList;

        public NamedPipeAsAppServiceConnection(NamedPipeClientStream clientStream, SafePipeHandle pipeHandle)
        {
            this.clientStream = clientStream;
            this.pipeHandle = pipeHandle;
            this.messageList = new ConcurrentDictionary<string, TaskCompletionSource<Dictionary<string, object>>>();

            var info = (Buffer: new byte[clientStream.InBufferSize], Message: new StringBuilder());
            BeginRead(info);
        }

        private void BeginRead((byte[] Buffer, StringBuilder Message) info)
        {
            clientStream?.BeginRead(info.Buffer, 0, info.Buffer.Length, EndReadCallBack, info);
        }

        private void EndReadCallBack(IAsyncResult result)
        {
            var readBytes = clientStream?.EndRead(result) ?? 0;
            if (readBytes > 0)
            {
                var info = ((byte[] Buffer, StringBuilder Message))result.AsyncState;

                // Get the read bytes and append them
                info.Message.Append(Encoding.UTF8.GetString(info.Buffer, 0, readBytes));

                if (!clientStream.IsMessageComplete) // Message is not complete, continue reading
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
                    var nextInfo = (Buffer: new byte[clientStream.InBufferSize], Message: new StringBuilder());
                    BeginRead(nextInfo);
                }
            }
        }

        public async Task<(AppServiceResponseStatus Status, Dictionary<string, object> Data)> SendMessageForResponseAsync(ValueSet valueSet, TimeSpan timeout = default)
        {
            if (clientStream == null)
            {
                return (AppServiceResponseStatus.Failure, null);
            }

            using var cts = new CancellationTokenSource();
            cts.CancelAfter((int)timeout.TotalMilliseconds);
            do
            {
                try
                {
                    if (clientStream.IsConnected)
                    {
                        var guid = Guid.NewGuid().ToString();
                        valueSet.Add("RequestID", guid);
                        var tcs = new TaskCompletionSource<Dictionary<string, object>>();
                        messageList.TryAdd(guid, tcs);
                        var serialized = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Dictionary<string, object>(valueSet)));
                        await clientStream.WriteAsync(serialized, 0, serialized.Length);
                        var response = await tcs.Task;

                        return (AppServiceResponseStatus.Success, response);
                    }
                }
                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Warn(ex, "Error sending request on pipe.");
                    break;
                }
                if (!cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(200);
                }
            } while (!cts.Token.IsCancellationRequested);

            return (AppServiceResponseStatus.Failure, null);
        }

        public async Task<AppServiceResponseStatus> SendMessageAsync(ValueSet valueSet)
        {
            if (clientStream == null)
            {
                return AppServiceResponseStatus.Failure;
            }

            try
            {
                if (clientStream.IsConnected)
                {
                    var guid = Guid.NewGuid().ToString();
                    valueSet.Add("RequestID", guid);
                    var serialized = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Dictionary<string, object>(valueSet)));
                    await clientStream.WriteAsync(serialized, 0, serialized.Length);
                    return AppServiceResponseStatus.Success;
                }
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, "Error sending request on pipe.");
            }

            return AppServiceResponseStatus.Failure;
        }

        public void Dispose()
        {
            foreach (var m in messageList)
            {
                m.Value.TrySetCanceled();
            }
            messageList.Clear();
            clientStream?.Dispose();
            clientStream = null;
            pipeHandle?.Dispose();
            pipeHandle = null;
        }
    }
}