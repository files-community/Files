using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace Files.Helpers
{
    public static class AppServiceConnectionHelper
    {
        public static Task<AppServiceConnection> Instance = BuildConnection();

        public static event EventHandler<Task<AppServiceConnection>> ConnectionChanged;

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
            (await Instance)?.Dispose();
            Instance = Task.FromResult<AppServiceConnection>(null);
            ConnectionChanged?.Invoke(null, Instance);
            deferral.Complete();
        }

        private static async Task<AppServiceConnection> BuildConnection()
        {
            try
            {
                var serviceConnection = new AppServiceConnection();
                serviceConnection.AppServiceName = "FilesInteropService";
                serviceConnection.PackageFamilyName = Package.Current.Id.FamilyName;
                serviceConnection.ServiceClosed += Connection_ServiceClosed;
                AppServiceConnectionStatus status = await serviceConnection.OpenAsync();
                if (status != AppServiceConnectionStatus.Success)
                {
                    // TODO: error handling
                    serviceConnection?.Dispose();
                    return null;
                }

                // Launch fulltrust process
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();

                return serviceConnection;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, "Could not initialize AppServiceConnection!");
                return null;
            }
        }

        public static async Task<(AppServiceResponseStatus Status, AppServiceResponse Data)> SendMessageWithRetryAsync(this AppServiceConnection serviceConnection, ValueSet valueSet, TimeSpan timeout)
        {
            if (serviceConnection == null)
            {
                return (AppServiceResponseStatus.Failure, null);
            }

            using var cts = new CancellationTokenSource();
            cts.CancelAfter((int)timeout.TotalMilliseconds);
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var resp = await serviceConnection.SendMessageAsync(valueSet);
                    if (resp.Status == AppServiceResponseStatus.Success && resp.Message.Any())
                    {
                        return (resp.Status, resp);
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                await Task.Delay(200);
            }
            return (AppServiceResponseStatus.Failure, null);
        }

        private static void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            sender?.Dispose();
        }
    }
}