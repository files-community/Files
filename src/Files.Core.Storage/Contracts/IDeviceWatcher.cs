// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Storage.Contracts
{
	public interface IDeviceWatcher : IWatcher
	{
		/// <summary>
		/// Gets the value that indicates whether the watcher can be started.
		/// </summary>
		bool CanBeStarted { get; }

		/// <summary>
		/// Gets invoked when an item addition is detected by the watcher
		/// </summary>
		event EventHandler<DeviceEventArgs>? DeviceAdded;

		/// <summary>
		/// Gets invoked when an item changing is detected by the watcher
		/// </summary>
		event EventHandler<DeviceEventArgs>? DeviceChanged;

		/// <summary>
		/// Gets invoked when an item removal is detected by the watcher
		/// </summary>
		event EventHandler<DeviceEventArgs>? DeviceDeleted;

		/// <summary>
		/// Gets invoked when an item inserting is detected by the watcher
		/// </summary>
		event EventHandler<DeviceEventArgs>? DeviceInserted;

		/// <summary>
		/// Gets invoked when an item ejection is detected by the watcher
		/// </summary>
		event EventHandler<DeviceEventArgs>? DeviceEjected;

		/// <summary>
		/// Gets invoked when item enumeration completed.
		/// </summary>
		event EventHandler? EnumerationCompleted;
	}
}
