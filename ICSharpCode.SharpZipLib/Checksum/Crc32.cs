using System;
using System.Runtime.CompilerServices;

namespace ICSharpCode.SharpZipLib.Checksum
{
	/// <summary>
	/// CRC-32 with reversed data and unreversed output
	/// </summary>
	/// <remarks>
	/// Generate a table for a byte-wise 32-bit CRC calculation on the polynomial:
	/// x^32+x^26+x^23+x^22+x^16+x^12+x^11+x^10+x^8+x^7+x^5+x^4+x^2+x^1+x^0.
	///
	/// Polynomials over GF(2) are represented in binary, one bit per coefficient,
	/// with the lowest powers in the most significant bit.  Then adding polynomials
	/// is just exclusive-or, and multiplying a polynomial by x is a right shift by
	/// one.  If we call the above polynomial p, and represent a byte as the
	/// polynomial q, also with the lowest power in the most significant bit (so the
	/// byte 0xb1 is the polynomial x^7+x^3+x+1), then the CRC is (q*x^32) mod p,
	/// where a mod b means the remainder after dividing a by b.
	///
	/// This calculation is done using the shift-register method of multiplying and
	/// taking the remainder.  The register is initialized to zero, and for each
	/// incoming bit, x^32 is added mod p to the register if the bit is a one (where
	/// x^32 mod p is p+x^32 = x^26+...+1), and the register is multiplied mod p by
	/// x (which is shifting right by one and adding x^32 mod p if the bit shifted
	/// out is a one).  We start with the highest power (least significant bit) of
	/// q and repeat for all eight bits of q.
	///
	/// This implementation uses sixteen lookup tables stored in one linear array
	/// to implement the slicing-by-16 algorithm, a variant of the slicing-by-8
	/// algorithm described in this Intel white paper:
	///
	/// https://web.archive.org/web/20120722193753/http://download.intel.com/technology/comms/perfnet/download/slicing-by-8.pdf
	///
	/// The first lookup table is simply the CRC of all possible eight bit values.
	/// Each successive lookup table is derived from the original table generated
	/// by Sarwate's algorithm. Slicing a 16-bit input and XORing the outputs
	/// together will produce the same output as a byte-by-byte CRC loop with
	/// fewer arithmetic and bit manipulation operations, at the cost of increased
	/// memory consumed by the lookup tables. (Slicing-by-16 requires a 16KB table,
	/// which is still small enough to fit in most processors' L1 cache.)
	/// </remarks>
	public sealed class Crc32 : IChecksum
	{
		#region Instance Fields

		private static readonly uint crcInit = 0xFFFFFFFF;
		private static readonly uint crcXor = 0xFFFFFFFF;

		private static readonly uint[] crcTable = CrcUtilities.GenerateSlicingLookupTable(0xEDB88320, isReversed: true);

		/// <summary>
		/// The CRC data checksum so far.
		/// </summary>
		private uint checkValue;

		#endregion Instance Fields

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static uint ComputeCrc32(uint oldCrc, byte bval)
		{
			return (uint)(Crc32.crcTable[(oldCrc ^ bval) & 0xFF] ^ (oldCrc >> 8));
		}

		/// <summary>
		/// Initialise a default instance of <see cref="Crc32"></see>
		/// </summary>
		public Crc32()
		{
			Reset();
		}

		/// <summary>
		/// Resets the CRC data checksum as if no update was ever called.
		/// </summary>
		public void Reset()
		{
			checkValue = crcInit;
		}

		/// <summary>
		/// Returns the CRC data checksum computed so far.
		/// </summary>
		/// <remarks>Reversed Out = false</remarks>
		public long Value
		{
			get
			{
				return (long)(checkValue ^ crcXor);
			}
		}

		/// <summary>
		/// Updates the checksum with the int bval.
		/// </summary>
		/// <param name = "bval">
		/// the byte is taken as the lower 8 bits of bval
		/// </param>
		/// <remarks>Reversed Data = true</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Update(int bval)
		{
			checkValue = unchecked(crcTable[(checkValue ^ bval) & 0xFF] ^ (checkValue >> 8));
		}

		/// <summary>
		/// Updates the CRC data checksum with the bytes taken from
		/// a block of data.
		/// </summary>
		/// <param name="buffer">Contains the data to update the CRC with.</param>
		public void Update(byte[] buffer)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			Update(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Update CRC data checksum based on a portion of a block of data
		/// </summary>
		/// <param name = "segment">
		/// The chunk of data to add
		/// </param>
		public void Update(ArraySegment<byte> segment)
		{
			Update(segment.Array, segment.Offset, segment.Count);
		}

		/// <summary>
		/// Internal helper function for updating a block of data using slicing.
		/// </summary>
		/// <param name="data">The array containing the data to add</param>
		/// <param name="offset">Range start for <paramref name="data"/> (inclusive)</param>
		/// <param name="count">The number of bytes to checksum starting from <paramref name="offset"/></param>
		private void Update(byte[] data, int offset, int count)
		{
			int remainder = count % CrcUtilities.SlicingDegree;
			int end = offset + count - remainder;

			while (offset != end)
			{
				checkValue = CrcUtilities.UpdateDataForReversedPoly(data, offset, crcTable, checkValue);
				offset += CrcUtilities.SlicingDegree;
			}

			if (remainder != 0)
			{
				SlowUpdateLoop(data, offset, end + remainder);
			}
		}

		/// <summary>
		/// A non-inlined function for updating data that doesn't fit in a 16-byte
		/// block. We don't expect to enter this function most of the time, and when
		/// we do we're not here for long, so disabling inlining here improves
		/// performance overall.
		/// </summary>
		/// <param name="data">The array containing the data to add</param>
		/// <param name="offset">Range start for <paramref name="data"/> (inclusive)</param>
		/// <param name="end">Range end for <paramref name="data"/> (exclusive)</param>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void SlowUpdateLoop(byte[] data, int offset, int end)
		{
			while (offset != end)
			{
				Update(data[offset++]);
			}
		}
	}
}
