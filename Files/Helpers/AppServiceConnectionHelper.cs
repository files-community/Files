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
        public static async Task<AppServiceConnection> BuildConnection()
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
                var resp = await serviceConnection.SendMessageAsync(valueSet);
                if (resp.Status == AppServiceResponseStatus.Success && resp.Message.Any())
                {
                    return (resp.Status, resp);
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