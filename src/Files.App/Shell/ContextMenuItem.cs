// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

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
