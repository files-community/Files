using System;

namespace ICSharpCode.SharpZipLib.Zip.Compression
{
	/// <summary>
	/// This is the Deflater class.  The deflater class compresses input
	/// with the deflate algorithm described in RFC 1951.  It has several
	/// compression levels and three different strategies described below.
	///
	/// This class is <i>not</i> thread safe.  This is inherent in the API, due
	/// to the split of deflate and setInput.
	///
	/// author of the original java version : Jochen Hoenicke
	/// </summary>
	public class Deflater
	{
		#region Deflater Documentation

		/*
		* The Deflater can do the following state transitions:
		*
		* (1) -> INIT_STATE   ----> INIT_FINISHING_STATE ---.
		*        /  | (2)      (5)                          |
		*       /   v          (5)                          |
		*   (3)| SETDICT_STATE ---> SETDICT_FINISHING_STATE |(3)
		*       \   | (3)                 |        ,--------'
		*        |  |                     | (3)   /
		*        v  v          (5)        v      v
		* (1) -> BUSY_STATE   ----> FINISHING_STATE
		*                                | (6)
		*                                v
		*                           FINISHED_STATE
		*    \_____________________________________/
		*                    | (7)
		*                    v
		*               CLOSED_STATE
		*
		* (1) If we should produce a header we start in INIT_STATE, otherwise
		*     we start in BUSY_STATE.
		* (2) A dictionary may be set only when we are in INIT_STATE, then
		*     we change the state as indicated.
		* (3) Whether a dictionary is set or not, on the first call of deflate
		*     we change to BUSY_STATE.
		* (4) -- intentionally left blank -- :)
		* (5) FINISHING_STATE is entered, when flush() is called to indicate that
		*     there is no more INPUT.  There are also states indicating, that
		*     the header wasn't written yet.
		* (6) FINISHED_STATE is entered, when everything has been flushed to the
		*     internal pending output buffer.
		* (7) At any time (7)
		*
		*/

		#endregion Deflater Documentation

		#region Public Constants

		/// <summary>
		/// The best and slowest compression level.  This tries to find very
		/// long and distant string repetitions.
		/// </summary>
		public const int BEST_COMPRESSION = 9;

		/// <summary>
		/// The worst but fastest compression level.
		/// </summary>
		public const int BEST_SPEED = 1;

		/// <summary>
		/// The default compression level.
		/// </summary>
		public const int DEFAULT_COMPRESSION = -1;

		/// <summary>
		/// This level won't compress at all but output uncompressed blocks.
		/// </summary>
		public const int NO_COMPRESSION = 0;

		/// <summary>
		/// The compression method.  This is the only method supported so far.
		/// There is no need to use this constant at all.
		/// </summary>
		public const int DEFLATED = 8;

		#endregion Public Constants

		#region Public Enum

		/// <summary>
		/// Compression Level as an enum for safer use
		/// </summary>
		public enum CompressionLevel
		{
			/// <summary>
			/// The best and slowest compression level.  This tries to find very
			/// long and distant string repetitions.
			/// </summary>
			BEST_COMPRESSION = Deflater.BEST_COMPRESSION,

			/// <summary>
			/// The worst but fastest compression level.
			/// </summary>
			BEST_SPEED = Deflater.BEST_SPEED,

			/// <summary>
			/// The default compression level.
			/// </summary>
			DEFAULT_COMPRESSION = Deflater.DEFAULT_COMPRESSION,

			/// <summary>
			/// This level won't compress at all but output uncompressed blocks.
			/// </summary>
			NO_COMPRESSION = Deflater.NO_COMPRESSION,

			/// <summary>
			/// The compression method.  This is the only method supported so far.
			/// There is no need to use this constant at all.
			/// </summary>
			DEFLATED = Deflater.DEFLATED
		}

		#endregion Public Enum

		#region Local Constants

		private const int IS_SETDICT = 0x01;
		private const int IS_FLUSHING = 0x04;
		private const int IS_FINISHING = 0x08;

		private const int INIT_STATE = 0x00;
		private const int SETDICT_STATE = 0x01;

		//		private static  int INIT_FINISHING_STATE    = 0x08;
		//		private static  int SETDICT_FINISHING_STATE = 0x09;
		private const int BUSY_STATE = 0x10;

		private const int FLUSHING_STATE = 0x14;
		private const int FINISHING_STATE = 0x1c;
		private const int FINISHED_STATE = 0x1e;
		private const int CLOSED_STATE = 0x7f;

		#endregion Local Constants

		#region Constructors

		/// <summary>
		/// Creates a new deflater with default compression level.
		/// </summary>
		public Deflater() : this(DEFAULT_COMPRESSION, false)
		{
		}

		/// <summary>
		/// Creates a new deflater with given compression level.
		/// </summary>
		/// <param name="level">
		/// the compression level, a value between NO_COMPRESSION
		/// and BEST_COMPRESSION, or DEFAULT_COMPRESSION.
		/// </param>
		/// <exception cref="System.ArgumentOutOfRangeException">if lvl is out of range.</exception>
		public Deflater(int level) : this(level, false)
		{
		}

		/// <summary>
		/// Creates a new deflater with given compression level.
		/// </summary>
		/// <param name="level">
		/// the compression level, a value between NO_COMPRESSION
		/// and BEST_COMPRESSION.
		/// </param>
		/// <param name="noZlibHeaderOrFooter">
		/// true, if we should suppress the Zlib/RFC1950 header at the
		/// beginning and the adler checksum at the end of the output.  This is
		/// useful for the GZIP/PKZIP formats.
		/// </param>
		/// <exception cref="System.ArgumentOutOfRangeException">if lvl is out of range.</exception>
		public Deflater(int level, bool noZlibHeaderOrFooter)
		{
			if (level == DEFAULT_COMPRESSION)
			{
				level = 6;
			}
			else if (level < NO_COMPRESSION || level > BEST_COMPRESSION)
			{
				throw new ArgumentOutOfRangeException(nameof(level));
			}

			pending = new DeflaterPending();
			engine = new DeflaterEngine(pending, noZlibHeaderOrFooter);
			this.noZlibHeaderOrFooter = noZlibHeaderOrFooter;
			SetStrategy(DeflateStrategy.Default);
			SetLevel(level);
			Reset();
		}

		#endregion Constructors

		/// <summary>
		/// Resets the deflater.  The deflater acts afterwards as if it was
		/// just created with the same compression level and strategy as it
		/// had before.
		/// </summary>
		public void Reset()
		{
			state = (noZlibHeaderOrFooter ? BUSY_STATE : INIT_STATE);
			totalOut = 0;
			pending.Reset();
			engine.Reset();
		}

		/// <summary>
		/// Gets the current adler checksum of the data that was processed so far.
		/// </summary>
		public int Adler
		{
			get
			{
				return engine.Adler;
			}
		}

		/// <summary>
		/// Gets the number of input bytes processed so far.
		/// </summary>
		public long TotalIn
		{
			get
			{
				return engine.TotalIn;
			}
		}

		/// <summary>
		/// Gets the number of output bytes so far.
		/// </summary>
		public long TotalOut
		{
			get
			{
				return totalOut;
			}
		}

		/// <summary>
		/// Flushes the current input block.  Further calls to deflate() will
		/// produce enough output to inflate everything in the current input
		/// block.  This is not part of Sun's JDK so I have made it package
		/// private.  It is used by DeflaterOutputStream to implement
		/// flush().
		/// </summary>
		public void Flush()
		{
			state |= IS_FLUSHING;
		}

		/// <summary>
		/// Finishes the deflater with the current input block.  It is an error
		/// to give more input after this method was called.  This method must
		/// be called to force all bytes to be flushed.
		/// </summary>
		public void Finish()
		{
			state |= (IS_FLUSHING | IS_FINISHING);
		}

		/// <summary>
		/// Returns true if the stream was finished and no more output bytes
		/// are available.
		/// </summary>
		public bool IsFinished
		{
			get
			{
				return (state == FINISHED_STATE) && pending.IsFlushed;
			}
		}

		/// <summary>
		/// Returns true, if the input buffer is empty.
		/// You should then call setInput().
		/// NOTE: This method can also return true when the stream
		/// was finished.
		/// </summary>
		public bool IsNeedingInput
		{
			get
			{
				return engine.NeedsInput();
			}
		}

		/// <summary>
		/// Sets the data which should be compressed next.  This should be only
		/// called when needsInput indicates that more input is needed.
		/// If you call setInput when needsInput() returns false, the
		/// previous input that is still pending will be thrown away.
		/// The given byte array should not be changed, before needsInput() returns
		/// true again.
		/// This call is equivalent to <code>setInput(input, 0, input.length)</code>.
		/// </summary>
		/// <param name="input">
		/// the buffer containing the input data.
		/// </param>
		/// <exception cref="System.InvalidOperationException">
		/// if the buffer was finished() or ended().
		/// </exception>
		public void SetInput(byte[] input)
		{
			SetInput(input, 0, input.Length);
		}

		/// <summary>
		/// Sets the data which should be compressed next.  This should be
		/// only called when needsInput indicates that more input is needed.
		/// The given byte array should not be changed, before needsInput() returns
		/// true again.
		/// </summary>
		/// <param name="input">
		/// the buffer containing the input data.
		/// </param>
		/// <param name="offset">
		/// the start of the data.
		/// </param>
		/// <param name="count">
		/// the number of data bytes of input.
		/// </param>
		/// <exception cref="System.InvalidOperationException">
		/// if the buffer was Finish()ed or if previous input is still pending.
		/// </exception>
		public void SetInput(byte[] input, int offset, int count)
		{
			if ((state & IS_FINISHING) != 0)
			{
				throw new InvalidOperationException("Finish() already called");
			}
			engine.SetInput(input, offset, count);
		}

		/// <summary>
		/// Sets the compression level.  There is no guarantee of the exact
		/// position of the change, but if you call this when needsInput is
		/// true the change of compression level will occur somewhere near
		/// before the end of the so far given input.
		/// </summary>
		/// <param name="level">
		/// the new compression level.
		/// </param>
		public void SetLevel(int level)
		{
			if (level == DEFAULT_COMPRESSION)
			{
				level = 6;
			}
			else if (level < NO_COMPRESSION || level > BEST_COMPRESSION)
			{
				throw new ArgumentOutOfRangeException(nameof(level));
			}

			if (this.level != level)
			{
				this.level = level;
				engine.SetLevel(level);
			}
		}

		/// <summary>
		/// Get current compression level
		/// </summary>
		/// <returns>Returns the current compression level</returns>
		public int GetLevel()
		{
			return level;
		}

		/// <summary>
		/// Sets the compression strategy. Strategy is one of
		/// DEFAULT_STRATEGY, HUFFMAN_ONLY and FILTERED.  For the exact
		/// position where the strategy is changed, the same as for
		/// SetLevel() applies.
		/// </summary>
		/// <param name="strategy">
		/// The new compression strategy.
		/// </param>
		public void SetStrategy(DeflateStrategy strategy)
		{
			engine.Strategy = strategy;
		}

		/// <summary>
		/// Deflates the current input block with to the given array.
		/// </summary>
		/// <param name="output">
		/// The buffer where compressed data is stored
		/// </param>
		/// <returns>
		/// The number of compressed bytes added to the output, or 0 if either
		/// IsNeedingInput() or IsFinished returns true or length is zero.
		/// </returns>
		public int Deflate(byte[] output)
		{
			return Deflate(output, 0, output.Length);
		}

		/// <summary>
		/// Deflates the current input block to the given array.
		/// </summary>
		/// <param name="output">
		/// Buffer to store the compressed data.
		/// </param>
		/// <param name="offset">
		/// Offset into the output array.
		/// </param>
		/// <param name="length">
		/// The maximum number of bytes that may be stored.
		/// </param>
		/// <returns>
		/// The number of compressed bytes added to the output, or 0 if either
		/// needsInput() or finished() returns true or length is zero.
		/// </returns>
		/// <exception cref="System.InvalidOperationException">
		/// If Finish() was previously called.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// If offset or length don't match the array length.
		/// </exception>
		public int Deflate(byte[] output, int offset, int length)
		{
			int origLength = length;

			if (state == CLOSED_STATE)
			{
				throw new InvalidOperationException("Deflater closed");
			}

			if (state < BUSY_STATE)
			{
				// output header
				int header = (DEFLATED +
					((DeflaterConstants.MAX_WBITS - 8) << 4)) << 8;
				int level_flags = (level - 1) >> 1;
				if (level_flags < 0 || level_flags > 3)
				{
					level_flags = 3;
				}
				header |= level_flags << 6;
				if ((state & IS_SETDICT) != 0)
				{
					// Dictionary was set
					header |= DeflaterConstants.PRESET_DICT;
				}
				header += 31 - (header % 31);

				pending.WriteShortMSB(header);
				if ((state & IS_SETDICT) != 0)
				{
					int chksum = engine.Adler;
					engine.ResetAdler();
					pending.WriteShortMSB(chksum >> 16);
					pending.WriteShortMSB(chksum & 0xffff);
				}

				state = BUSY_STATE | (state & (IS_FLUSHING | IS_FINISHING));
			}

			for (; ; )
			{
				int count = pending.Flush(output, offset, length);
				offset += count;
				totalOut += count;
				length -= count;

				if (length == 0 || state == FINISHED_STATE)
				{
					break;
				}

				if (!engine.Deflate((state & IS_FLUSHING) != 0, (state & IS_FINISHING) != 0))
				{
					switch (state)
					{
						case BUSY_STATE:
							// We need more input now
							return origLength - length;

						case FLUSHING_STATE:
							if (level != NO_COMPRESSION)
							{
								/* We have to supply some lookahead.  8 bit lookahead
								 * is needed by the zlib inflater, and we must fill
								 * the next byte, so that all bits are flushed.
								 */
								int neededbits = 8 + ((-pending.BitCount) & 7);
								while (neededbits > 0)
								{
									/* write a static tree block consisting solely of
									 * an EOF:
									 */
									pending.WriteBits(2, 10);
									neededbits -= 10;
								}
							}
							state = BUSY_STATE;
							break;

						case FINISHING_STATE:
							pending.AlignToByte();

							// Compressed data is complete.  Write footer information if required.
							if (!noZlibHeaderOrFooter)
							{
								int adler = engine.Adler;
								pending.WriteShortMSB(adler >> 16);
								pending.WriteShortMSB(adler & 0xffff);
							}
							state = FINISHED_STATE;
							break;
					}
				}
			}
			return origLength - length;
		}

		/// <summary>
		/// Sets the dictionary which should be used in the deflate process.
		/// This call is equivalent to <code>setDictionary(dict, 0, dict.Length)</code>.
		/// </summary>
		/// <param name="dictionary">
		/// the dictionary.
		/// </param>
		/// <exception cref="System.InvalidOperationException">
		/// if SetInput () or Deflate () were already called or another dictionary was already set.
		/// </exception>
		public void SetDictionary(byte[] dictionary)
		{
			SetDictionary(dictionary, 0, dictionary.Length);
		}

		/// <summary>
		/// Sets the dictionary which should be used in the deflate process.
		/// The dictionary is a byte array containing strings that are
		/// likely to occur in the data which should be compressed.  The
		/// dictionary is not stored in the compressed output, only a
		/// checksum.  To decompress the output you need to supply the same
		/// dictionary again.
		/// </summary>
		/// <param name="dictionary">
		/// The dictionary data
		/// </param>
		/// <param name="index">
		/// The index where dictionary information commences.
		/// </param>
		/// <param name="count">
		/// The number of bytes in the dictionary.
		/// </param>
		/// <exception cref="System.InvalidOperationException">
		/// If SetInput () or Deflate() were already called or another dictionary was already set.
		/// </exception>
		public void SetDictionary(byte[] dictionary, int index, int count)
		{
			if (state != INIT_STATE)
			{
				throw new InvalidOperationException();
			}

			state = SETDICT_STATE;
			engine.SetDictionary(dictionary, index, count);
		}

		#region Instance Fields

		/// <summary>
		/// Compression level.
		/// </summary>
		private int level;

		/// <summary>
		/// If true no Zlib/RFC1950 headers or footers are generated
		/// </summary>
		private bool noZlibHeaderOrFooter;

		/// <summary>
		/// The current state.
		/// </summary>
		private int state;

		/// <summary>
		/// The total bytes of output written.
		/// </summary>
		private long totalOut;

		/// <summary>
		/// The pending output.
		/// </summary>
		private DeflaterPending pending;

		/// <summary>
		/// The deflater engine.
		/// </summary>
		private DeflaterEngine engine;

		#endregion Instance Fields
	}
}
