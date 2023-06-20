// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;

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
