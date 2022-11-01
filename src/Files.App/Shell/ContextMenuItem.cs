using Files.Shared;
using System;
using System.Collections.Generic;

namespace Files.App.Shell
{
	public class ContextMenuItem : Win32ContextMenuItem, IDisposable
	{
		public ContextMenuItem()
		{
			SubItems = new List<Win32ContextMenuItem>();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (SubItems is not null)
				{
					foreach (var si in SubItems)
					{
						(si as IDisposable)?.Dispose();
					}

					SubItems = null;
				}
			}
		}
	}
}
