// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

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
