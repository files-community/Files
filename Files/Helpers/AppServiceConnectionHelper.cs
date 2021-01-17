using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace Files.Helpers
{
    public static class AppServiceConnectionHelper
    {
        private static AppServiceConnection appServiceConnection;

        public static AppServiceConnection Connection
        {
            get
            {
                if (appServiceConnection == null)
                {
                    throw new InvalidOperationException("AppServiceConnectionHelper has not been initialized");
                }

                return appServiceConnection;
            }
        }

        public static async Task Initialize()
        {
            appServiceConnection = new AppServiceConnection
            {
                AppServiceName = "FilesInteropService",
                PackageFamilyName = Package.Current.Id.FamilyName
            };
            appServiceConnection.ServiceClosed += AppServiceConnection_ServiceClosed;
            var status = await appServiceConnection.OpenAsync();
            if (status != AppServiceConnectionStatus.Success)
            {
                // TODO: error handling
                appServiceConnection?.Dispose();
                appServiceConnection = null;
            }

            // Launch fulltrust process
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        private static void AppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            appServiceConnection?.Dispose();
            appServiceConnection = null;
        }

        public static async Task<bool> IsConnected()
        {
            var timestamp = DateTime.Now.Ticks.ToString();
            var response = await appServiceConnection.SendMessageAsync(new ValueSet { { "Arguments", "Marco" }, { "Timestamp", timestamp } });
            if (response.Status == AppServiceResponseStatus.Success)
            {
                return response.Message.ContainsKey("Marco") && (string)response.Message["Marco"] == "Polo" && string.Equals((string)response.Message["Timestamp"], timestamp);
            }

            return false;
        }
    }
}
