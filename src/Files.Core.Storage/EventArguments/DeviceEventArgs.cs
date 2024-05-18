// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Storage.EventArguments
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
