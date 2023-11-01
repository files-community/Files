using Microsoft.Win32.SafeHandles;
using System;
using static Files.App.Utils.Terminal.ConPTY.PseudoConsoleApi;

namespace Files.App.Utils.Terminal.ConPTY
{
	/// <summary>
	/// Utility functions around the new Pseudo Console APIs
	/// </summary>
	internal sealed class PseudoConsole : IDisposable
	{
		public static readonly nint PseudoConsoleThreadAttribute = (nint)PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE;

		public nint Handle { get; }

		private PseudoConsole(nint handle)
		{
			Handle = handle;
		}

		internal static PseudoConsole Create(SafeFileHandle inputReadSide, SafeFileHandle outputWriteSide, int width, int height)
		{
			var createResult = CreatePseudoConsole(
				new COORD { X = (short)width, Y = (short)height },
				inputReadSide, outputWriteSide,
				0, out nint hPC);
			if (createResult != 0)
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
