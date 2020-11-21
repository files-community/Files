using Files.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace FilesFullTrust
{
    public class DeviceWatcher : IDisposable
    {
        private ManagementEventWatcher insertWatcher, removeWatcher;
        private AppServiceConnection connection;

        private const string WpdGuid = "{6ac27878-a6fa-4155-ba85-f98f491d4f33}";

        public DeviceWatcher(AppServiceConnection connection)
        {
            this.connection = connection;
        }

        public void Start()
        {
            //WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2 or EventType = 3");
            WqlEventQuery insertQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_LogicalDisk'");
            insertWatcher = new ManagementEventWatcher(insertQuery);
            insertWatcher.EventArrived += new EventArrivedEventHandler(DeviceInsertedEvent);
            insertWatcher.Start();

            WqlEventQuery removeQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_LogicalDisk'");
            removeWatcher = new ManagementEventWatcher(removeQuery);
            removeWatcher.EventArrived += new EventArrivedEventHandler(DeviceRemovedEvent);
            removeWatcher.Start();
        }

        private async void DeviceRemovedEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject obj = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            var deviceName = (string)obj.Properties["Name"].Value;
            var deviceId = (string)obj.Properties["DeviceID"].Value;
            await SendEvent(deviceName, deviceId, DeviceEvent.Removed);
        }

        private async void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject obj = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            var deviceName = (string)obj.Properties["Name"].Value;
            var deviceId = (string)obj.Properties["DeviceID"].Value;
            await SendEvent(deviceName, deviceId, DeviceEvent.Inserted);
        }

        private async Task SendEvent(string deviceName, string deviceId, DeviceEvent eventType)
        {
            System.Diagnostics.Debug.WriteLine($"Drive connection event: {eventType}, {deviceName}, {deviceId}");
            if (connection != null)
            {
                await connection.SendMessageAsync(new ValueSet()
                {
                    { "DeviceID", deviceId },
                    { "EventType", (int)eventType }
                });
            }
        }

        public void Dispose()
        {
            insertWatcher?.Dispose();
            removeWatcher?.Dispose();
            insertWatcher = null;
            removeWatcher = null;
        }
    }
}
