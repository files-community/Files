using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.SharpZipLib.Tar
{
	/// <summary>
	/// Reads the extended header of a Tar stream
	/// </summary>
	public class TarExtendedHeaderReader
	{
		private const byte LENGTH = 0;
		private const byte KEY = 1;
		private const byte VALUE = 2;
		private const byte END = 3;

		private readonly Dictionary<string, string> headers = new Dictionary<string, string>();

		private string[] headerParts = new string[3];

		private int bbIndex;
		private byte[] byteBuffer;
		private char[] charBuffer;

		private readonly StringBuilder sb = new StringBuilder();
		private readonly Decoder decoder = Encoding.UTF8.GetDecoder();

		private int state = LENGTH;

		private static readonly byte[] StateNext = new[] { (byte)' ', (byte)'=', (byte)'\n' };

		/// <summary>
		/// Creates a new <see cref="TarExtendedHeaderReader"/>.
		/// </summary>
		public TarExtendedHeaderReader()
		{
			ResetBuffers();
		}

		/// <summary>
		/// Read <paramref name="length"/> bytes from <paramref name="buffer"/>
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="length"></param>
		public void Read(byte[] buffer, int length)
		{
			for (int i = 0; i < length; i++)
			{
				byte next = buffer[i];

				if (next == StateNext[state])
				{
					Flush();
					headerParts[state] = sb.ToString();
					sb.Clear();

					if (++state == END)
					{
						headers.Add(headerParts[KEY], headerParts[VALUE]);
						headerParts = new string[3];
						state = LENGTH;
					}
				}
				else
				{
					byteBuffer[bbIndex++] = next;
					if (bbIndex == 4)
						Flush();
				}
			}
		}

		private void Flush()
		{
			decoder.Convert(byteBuffer, 0, bbIndex, charBuffer, 0, 4, false, out int bytesUsed, out int charsUsed, out bool completed);

			sb.Append(charBuffer, 0, charsUsed);
			ResetBuffers();
		}

		private void ResetBuffers()
		{
			charBuffer = new char[4];
			byteBuffer = new byte[4];
			bbIndex = 0;
		}

		/// <summary>
		/// Returns the parsed headers as key-value strings
		/// </summary>
		public Dictionary<string, string> Headers
		{
			get
			{
				// TODO: Check for invalid state? -NM 2018-07-01
				return headers;
			}
		}
	}
}
