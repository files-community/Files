// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Storage
{
	public unsafe class JumpListManager : IDisposable
	{
		public string AppId { get; }

		public JumpListManager(string appId)
		{
			if (string.IsNullOrEmpty(appId))
				throw new ArgumentException("App ID cannot be null or empty.", nameof(appId));

			AppId = appId;
			//_jumpList = new ConcurrentDictionary<string, IObjectArray>();
		}

		public IEnumerable<WindowsStorable> GetAutomaticDestinations()
		{
			return [];
		}

		public IEnumerable<WindowsStorable> GetCustomDestinations()
		{
			return [];
		}

		public void Dispose()
		{
		}
	}
}
