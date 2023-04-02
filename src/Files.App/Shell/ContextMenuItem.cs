using Files.Shared;
using System;
using System.Collections.Generic;

namespace Files.App.Shell
{
	public class ContextMenuItem : Win32ContextMenuItem, IDisposable
	{
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing && SubItems is not null)
			{
				foreach (var subItem in SubItems)
					(subItem as IDisposable)?.Dispose();

				SubItems = null;
			}
		}
	}
}
