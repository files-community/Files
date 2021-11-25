using System;

namespace ICSharpCode.SharpZipLib.Zip.Compression.Streams
{
	/// <summary>
	/// Contains the output from the Inflation process.
	/// We need to have a window so that we can refer backwards into the output stream
	/// to repeat stuff.<br/>
	/// Author of the original java version : John Leuner
	/// </summary>
	public class OutputWindow
	{
		#region Constants

		private const int WindowSize = 1 << 15;
		private const int WindowMask = WindowSize - 1;

		#endregion Constants

		#region Instance Fields

		private byte[] window = new byte[WindowSize]; //The window is 2^15 bytes
		private int windowEnd;
		private int windowFilled;

		#endregion Instance Fields

		/// <summary>
		/// Write a byte to this output window
		/// </summary>
		/// <param name="value">value to write</param>
		/// <exception cref="InvalidOperationException">
		/// if window is full
		/// </exception>
		public void Write(int value)
		{
			if (windowFilled++ == WindowSize)
			{
				throw new InvalidOperationException("Window full");
			}
			window[windowEnd++] = (byte)value;
			windowEnd &= WindowMask;
		}

		private void SlowRepeat(int repStart, int length, int distance)
		{
			while (length-- > 0)
			{
				window[windowEnd++] = window[repStart++];
				windowEnd &= WindowMask;
				repStart &= WindowMask;
			}
		}

		/// <summary>
		/// Append a byte pattern already in the window itself
		/// </summary>
		/// <param name="length">length of pattern to copy</param>
		/// <param name="distance">distance from end of window pattern occurs</param>
		/// <exception cref="InvalidOperationException">
		/// If the repeated data overflows the window
		/// </exception>
		public void Repeat(int length, int distance)
		{
			if ((windowFilled += length) > WindowSize)
			{
				throw new InvalidOperationException("Window full");
			}

			int repStart = (windowEnd - distance) & WindowMask;
			int border = WindowSize - length;
			if ((repStart <= border) && (windowEnd < border))
			{
				if (length <= distance)
				{
					System.Array.Copy(window, repStart, window, windowEnd, length);
					windowEnd += length;
				}
				else
				{
					// We have to copy manually, since the repeat pattern overlaps.
					while (length-- > 0)
					{
						window[windowEnd++] = window[repStart++];
					}
				}
			}
			else
			{
				SlowRepeat(repStart, length, distance);
			}
		}

		/// <summary>
		/// Copy from input manipulator to internal window
		/// </summary>
		/// <param name="input">source of data</param>
		/// <param name="length">length of data to copy</param>
		/// <returns>the number of bytes copied</returns>
		public int CopyStored(StreamManipulator input, int length)
		{
			length = Math.Min(Math.Min(length, WindowSize - windowFilled), input.AvailableBytes);
			int copied;

			int tailLen = WindowSize - windowEnd;
			if (length > tailLen)
			{
				copied = input.CopyBytes(window, windowEnd, tailLen);
				if (copied == tailLen)
				{
					copied += input.CopyBytes(window, 0, length - tailLen);
				}
			}
			else
			{
				copied = input.CopyBytes(window, windowEnd, length);
			}

			windowEnd = (windowEnd + copied) & WindowMask;
			windowFilled += copied;
			return copied;
		}

		/// <summary>
		/// Copy dictionary to window
		/// </summary>
		/// <param name="dictionary">source dictionary</param>
		/// <param name="offset">offset of start in source dictionary</param>
		/// <param name="length">length of dictionary</param>
		/// <exception cref="InvalidOperationException">
		/// If window isnt empty
		/// </exception>
		public void CopyDict(byte[] dictionary, int offset, int length)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException(nameof(dictionary));
			}

			if (windowFilled > 0)
			{
				throw new InvalidOperationException();
			}

			if (length > WindowSize)
			{
				offset += length - WindowSize;
				length = WindowSize;
			}
			System.Array.Copy(dictionary, offset, window, 0, length);
			windowEnd = length & WindowMask;
		}

		/// <summary>
		/// Get remaining unfilled space in window
		/// </summary>
		/// <returns>Number of bytes left in window</returns>
		public int GetFreeSpace()
		{
			return WindowSize - windowFilled;
		}

		/// <summary>
		/// Get bytes available for output in window
		/// </summary>
		/// <returns>Number of bytes filled</returns>
		public int GetAvailable()
		{
			return windowFilled;
		}

		/// <summary>
		/// Copy contents of window to output
		/// </summary>
		/// <param name="output">buffer to copy to</param>
		/// <param name="offset">offset to start at</param>
		/// <param name="len">number of bytes to count</param>
		/// <returns>The number of bytes copied</returns>
		/// <exception cref="InvalidOperationException">
		/// If a window underflow occurs
		/// </exception>
		public int CopyOutput(byte[] output, int offset, int len)
		{
			int copyEnd = windowEnd;
			if (len > windowFilled)
			{
				len = windowFilled;
			}
			else
			{
				copyEnd = (windowEnd - windowFilled + len) & WindowMask;
			}

			int copied = len;
			int tailLen = len - copyEnd;

			if (tailLen > 0)
			{
				System.Array.Copy(window, WindowSize - tailLen, output, offset, tailLen);
				offset += tailLen;
				len = copyEnd;
			}
			System.Array.Copy(window, copyEnd - len, output, offset, len);
			windowFilled -= copied;
			if (windowFilled < 0)
			{
				throw new InvalidOperationException();
			}
			return copied;
		}

		/// <summary>
		/// Reset by clearing window so <see cref="GetAvailable">GetAvailable</see> returns 0
		/// </summary>
		public void Reset()
		{
			windowFilled = windowEnd = 0;
		}
	}
}
