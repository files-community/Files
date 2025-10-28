using Microsoft.Win32.SafeHandles;
using System;
using static Files.App.Terminal.Native.PseudoConsoleApi;

namespace Files.App.Terminal
{
	/// <summary>
	/// Utility functions around the new Pseudo Console APIs
	/// </summary>
	internal sealed class PseudoConsole : IDisposable
	{
		public static readonly IntPtr PseudoConsoleThreadAttribute = (IntPtr)PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE;

		public IntPtr Handle { get; }

		private PseudoConsole(IntPtr handle)
		{
			this.Handle = handle;
		}

		internal static PseudoConsole Create(SafeFileHandle inputReadSide, SafeFileHandle outputWriteSide, int width, int height)
		{
			var createResult = CreatePseudoConsole(
				new COORD { X = (short)width, Y = (short)height },
				inputReadSide, outputWriteSide,
				0, out IntPtr hPC);
			if(createResult != 0)
			{
				throw new InvalidOperationException("Could not create pseudo console. Error Code " + createResult);
			}
			return new PseudoConsole(hPC);
		}

		internal void Resize(int width, int height)
		{
			ResizePseudoConsole(Handle, new COORD { X = (short)width, Y = (short)height });
		}

		public void Dispose()
		{
			ClosePseudoConsole(Handle);
		}
	}
}
