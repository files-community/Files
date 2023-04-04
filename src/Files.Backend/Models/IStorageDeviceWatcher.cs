using Files.Sdk.Storage.LocatableStorage;
using System;

namespace Files.Backend.Models
{
	public interface IStorageDeviceWatcher
	{
		event EventHandler<ILocatableFolder> DeviceAdded;
		event EventHandler<string> DeviceRemoved;
		event EventHandler EnumerationCompleted;
		event EventHandler<string> DeviceModified;
		bool CanBeStarted { get; }

		void Start();
		void Stop();
	}
}