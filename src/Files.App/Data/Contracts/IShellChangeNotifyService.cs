// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	public interface IShellChangeNotifyService : IDisposable
	{
		event Action<string>? ItemUpdated;

		event Action<string>? AttributesChanged;

		void StartMonitoring(string path);

		void StopMonitoring();
	}
}
