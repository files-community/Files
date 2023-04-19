namespace Files.App.Helpers.MMI
{
	public class DeviceEventArgs : EventArgs
	{
		public string DeviceName { get; }
		public string DeviceId { get; }

		public DeviceEventArgs(string deviceName, string deviceId)
		{
			DeviceName = deviceName;
			DeviceId = deviceId;
		}
	}
}
