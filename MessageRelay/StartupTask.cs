using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;

namespace MessageRelay
{
    public sealed class StartupTask : IBackgroundTask
    {
        private Guid thisConnectionGuid;
        private BackgroundTaskDeferral backgroundTaskDeferral;
        private static readonly ConcurrentDictionary<Guid, AppServiceConnection> Connections;

        static StartupTask()
        {
            Connections = new ConcurrentDictionary<Guid, AppServiceConnection>();
        }

        /// <summary>
        /// When an AppServiceConnection of type 'FilesInteropService' (as
        /// defined in Package.appxmanifest) is instantiated and OpenAsync() is called
        /// on it, then one of these StartupTask's in instantiated and Run() is called.
        /// </summary>
        /// <param name="taskInstance"></param>
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            try
            {
                // Get a service deferral so the service isn't terminated upon completion of Run()
                backgroundTaskDeferral = taskInstance.GetDeferral();
                // Save a unique identifier for each connection
                thisConnectionGuid = Guid.NewGuid();
                var triggerDetails = taskInstance.TriggerDetails as AppServiceTriggerDetails;
                var connection = triggerDetails?.AppServiceConnection;
                if (connection == null)
                {
                    System.Diagnostics.Debug.WriteLine("AppServiceConnection was null, ignorning this request");
                    backgroundTaskDeferral.Complete();
                    return;
                }
                // Save the guid and connection in a *static* list of all connections
                Connections.TryAdd(thisConnectionGuid, connection);
                System.Diagnostics.Debug.WriteLine($"Connection opened: {thisConnectionGuid}");
                taskInstance.Canceled += OnTaskCancelled;
                // Listen for incoming app service requests
                connection.RequestReceived += ConnectionRequestReceived;
                connection.ServiceClosed += ConnectionOnServiceClosed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in startup: {ex}");
            }
        }

        /// <summary>
        /// This happens when an app closes its connection normally.
        /// </summary>
        private async void OnTaskCancelled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            System.Diagnostics.Debug.WriteLine($"MessageRelay was cancelled, removing {thisConnectionGuid} from the list of active connections.");
            RemoveConnection(thisConnectionGuid);

            if (Connections.Count == 1)
            {
                // Last open connection is the fulltrust process, lets close it
                // This should be done better and handle fulltrust process crashes
                var value = new ValueSet() { { "Arguments", "Terminate" } };
                await SendMessageAsync(Connections.Single(), value);
            }

            if (backgroundTaskDeferral != null)
            {
                backgroundTaskDeferral.Complete();
                backgroundTaskDeferral = null;
            }
        }

        private void ConnectionOnServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            // I don't think this ever happens
            System.Diagnostics.Debug.WriteLine($"Connection closed: {thisConnectionGuid}");
            RemoveConnection(thisConnectionGuid);
        }

        private async void ConnectionRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            // Take out a deferral since we use await
            var appServiceDeferral = args.GetDeferral();
            try
            {
                System.Diagnostics.Debug.WriteLine($"Request initiated by {thisConnectionGuid}");

                // .ToList() required since connections may get removed during SendMessage()
                var otherConnections = Connections
                    .Where(i => i.Key != thisConnectionGuid)
                    .ToList();
                foreach (var connection in otherConnections)
                {
                    // Relay request to all the other listeners
                    // Break when a listener returns a response (message was handled)
                    var returnData = await SendMessageAsync(connection, args.Request.Message);
                    if (returnData?.Message?.Any() ?? false)
                    {
                        await args.Request.SendResponseAsync(returnData?.Message);
                        break;
                    }
                }
            }
            finally
            {
                appServiceDeferral.Complete();
            }
        }

        private async Task<AppServiceResponse> SendMessageAsync(KeyValuePair<Guid, AppServiceConnection> connection, ValueSet valueSet)
        {
            try
            {
                var result = await connection.Value.SendMessageAsync(valueSet);
                if (result.Status == AppServiceResponseStatus.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"Successfully sent message to {connection.Key}. Result = {result.Message}");
                    return result;
                }
                if (result.Status == AppServiceResponseStatus.Failure)
                {
                    // When an app with an open connection is terminated and it fails
                    //      to dispose of its connection, the connection object remains
                    //      in Connections.  When someone tries to send to it, it gets
                    //      an AppServiceResponseStatus.Failure response
                    System.Diagnostics.Debug.WriteLine($"Error sending to {connection.Key}.  Removing it from the list of active connections.");
                    RemoveConnection(connection.Key);
                    return result;
                }
                System.Diagnostics.Debug.WriteLine($"Error sending to {connection.Key} - {result.Status}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error SendMessage to {connection.Key}: {ex}");
                return null;
            }
        }

        private void RemoveConnection(Guid key)
        {
            if (Connections.TryRemove(key, out var connection))
            {
                connection.Dispose();
            }
        }
    }
}