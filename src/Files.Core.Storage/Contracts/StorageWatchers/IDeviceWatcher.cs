// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Storage
{
	public interface IDeviceWatcher : IWatcher
	{
		/// <summary>
		/// Gets invoked when an item addition is detected by the watcher
		/// </summary>
		event EventHandler<DeviceEventArgs>? ItemAdded;

		/// <summary>
		/// Gets invoked when an item removal is detected by the watcher
		/// </summary>
		event EventHandler<DeviceEventArgs>? ItemDeleted;

		/// <summary>
		/// Gets invoked when an item changing is detected by the watcher
		/// </summary>
		event EventHandler<DeviceEventArgs>? ItemChanged;

		/// <summary>
		/// Gets invoked when an item renaming is detected by the watcher
		/// </summary>
		event EventHandler<DeviceEventArgs>? ItemRenamed;

		/// <summary>
		/// Gets invoked when an item renaming is detected by the watcher
		/// </summary>
		event EventHandler<DeviceEventArgs>? ItemInserted;

		/// <summary>
		/// Gets invoked when an item renaming is detected by the watcher
		/// </summary>
		event EventHandler<DeviceEventArgs>? ItemEjected;
	}
}
