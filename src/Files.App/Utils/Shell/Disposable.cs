// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Represents an abstracted implementation for IDisposable
	/// </summary>
	public abstract class Disposable : IDisposable
	{
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
		}
	}
}
