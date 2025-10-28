using Microsoft.Win32.SafeHandles;
using System;
using static Files.App.Terminal.Native.PseudoConsoleApi;

namespace Files.App.Terminal
{
	/// <summary>
	/// A pipe used to talk to the pseudoconsole, as described in:
	/// https://docs.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session
	/// </summary>
	/// <remarks>
	/// We'll have two instances of this class, one for input and one for output.
	/// </remarks>
	internal sealed class PseudoConsolePipe : IDisposable
	{
		public readonly SafeFileHandle ReadSide;
		public readonly SafeFileHandle WriteSide;

		public PseudoConsolePipe()
		{
			if (!CreatePipe(out ReadSide, out WriteSide, IntPtr.Zero, 0))
			{
				throw new InvalidOperationException("failed to create pipe");
			}
		}

		#region IDisposable

		void Dispose(bool disposing)
		{
			if (disposing)
			{
				ReadSide?.Dispose();
				WriteSide?.Dispose();
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
