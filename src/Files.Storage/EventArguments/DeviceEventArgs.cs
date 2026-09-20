// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.Storage.EventArguments
{
	public sealed class DeviceEventArgs : EventArgs
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
