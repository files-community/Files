// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Models;

public interface IStorageDeviceWatcher
{
	event EventHandler? Changed;
	bool CanBeStarted { get; }
	void Start();
	void Stop();
}
