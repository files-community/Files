using System.Runtime.CompilerServices;

namespace ICSharpCode.SharpZipLib.Checksum
{
	internal static class CrcUtilities
	{
		/// <summary>
		/// The number of slicing lookup tables to generate.
		/// </summary>
		internal const int SlicingDegree = 16;

		/// <summary>
		/// Generates multiple CRC lookup tables for a given polynomial, stored
		/// in a linear array of uints. The first block (i.e. the first 256
		/// elements) is the same as the byte-by-byte CRC lookup table. 
		/// </summary>
		/// <param name="polynomial">The generating CRC polynomial</param>
		/// <param name="isReversed">Whether the polynomial is in reversed bit order</param>
		/// <returns>A linear array of 256 * <see cref="SlicingDegree"/> elements</returns>
		/// <remarks>
		/// This table could also be generated as a rectangular array, but the
		/// JIT compiler generates slower code than if we use a linear array.
		/// Known issue, see: https://github.com/dotnet/runtime/issues/30275
		/// </remarks>
		internal static uint[] GenerateSlicingLookupTable(uint polynomial, bool isReversed)
		{
			var table = new uint[256 * SlicingDegree];
			uint one = isReversed ? 1 : (1U << 31);

			for (int i = 0; i < 256; i++)
			{
				uint res = (uint)(isReversed ? i : i << 24);
				for (int j = 0; j < SlicingDegree; j++)
				{
					for (int k = 0; k < 8; k++)
					{
						if (isReversed)
						{
							res = (res & one) == 1 ? polynomial ^ (res >> 1) : res >> 1;
						}
						else
						{
							res = (res & one) != 0 ? polynomial ^ (res << 1) : res << 1;
						}
					}

					table[(256 * j) + i] = res;
				}
			}

			return table;
		}

		/// <summary>
		/// Mixes the first four bytes of input with <paramref name="checkValue"/>
		/// using normal ordering before calling <see cref="UpdateDataCommon"/>.
		/// </summary>
		/// <param name="input">Array of data to checksum</param>
		/// <param name="offset">Offset to start reading <paramref name="input"/> from</param>
		/// <param name="crcTable">The table to use for slicing-by-16 lookup</param>
		/// <param name="checkValue">Checksum state before this update call</param>
		/// <returns>A new unfinalized checksum value</returns>
		/// <seealso cref="UpdateDataForReversedPoly"/>
		/// <remarks>
		/// Assumes input[offset]..input[offset + 15] are valid array indexes.
		/// For performance reasons, this must be checked by the caller.
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static uint UpdateDataForNormalPoly(byte[] input, int offset, uint[] crcTable, uint checkValue)
		{
			byte x1 = (byte)((byte)(checkValue >> 24) ^ input[offset]);
			byte x2 = (byte)((byte)(checkValue >> 16) ^ input[offset + 1]);
			byte x3 = (byte)((byte)(checkValue >> 8) ^ input[offset + 2]);
			byte x4 = (byte)((byte)checkValue ^ input[offset + 3]);

			return UpdateDataCommon(input, offset, crcTable, x1, x2, x3, x4);
		}

		/// <summary>
		/// Mixes the first four bytes of input with <paramref name="checkValue"/>
		/// using reflected ordering before calling <see cref="UpdateDataCommon"/>.
		/// </summary>
		/// <param name="input">Array of data to checksum</param>
		/// <param name="offset">Offset to start reading <paramref name="input"/> from</param>
		/// <param name="crcTable">The table to use for slicing-by-16 lookup</param>
		/// <param name="checkValue">Checksum state before this update call</param>
		/// <returns>A new unfinalized checksum value</returns>
		/// <seealso cref="UpdateDataForNormalPoly"/>
		/// <remarks>
		/// Assumes input[offset]..input[offset + 15] are valid array indexes.
		/// For performance reasons, this must be checked by the caller.
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static uint UpdateDataForReversedPoly(byte[] input, int offset, uint[] crcTable, uint checkValue)
		{
			byte x1 = (byte)((byte)checkValue ^ input[offset]);
			byte x2 = (byte)((byte)(checkValue >>= 8) ^ input[offset + 1]);
			byte x3 = (byte)((byte)(checkValue >>= 8) ^ input[offset + 2]);
			byte x4 = (byte)((byte)(checkValue >>= 8) ^ input[offset + 3]);

			return UpdateDataCommon(input, offset, crcTable, x1, x2, x3, x4);
		}

		/// <summary>
		/// A shared method for updating an unfinalized CRC checksum using slicing-by-16.
		/// </summary>
		/// <param name="input">Array of data to checksum</param>
		/// <param name="offset">Offset to start reading <paramref name="input"/> from</param>
		/// <param name="crcTable">The table to use for slicing-by-16 lookup</param>
		/// <param name="x1">First byte of input after mixing with the old CRC</param>
		/// <param name="x2">Second byte of input after mixing with the old CRC</param>
		/// <param name="x3">Third byte of input after mixing with the old CRC</param>
		/// <param name="x4">Fourth byte of input after mixing with the old CRC</param>
		/// <returns>A new unfinalized checksum value</returns>
		/// <remarks>
		/// <para>
		/// Even though the first four bytes of input are fed in as arguments,
		/// <paramref name="offset"/> should be the same value passed to this
		/// function's caller (either <see cref="UpdateDataForNormalPoly"/> or
		/// <see cref="UpdateDataForReversedPoly"/>). This method will get inlined
		/// into both functions, so using the same offset produces faster code.
		/// </para>
		/// <para>
		/// Because most processors running C# have some kind of instruction-level
		/// parallelism, the order of XOR operations can affect performance. This
		/// ordering assumes that the assembly code generated by the just-in-time
		/// compiler will emit a bunch of arithmetic operations for checking array
		/// bounds. Then it opportunistically XORs a1 and a2 to keep the processor
		/// busy while those other parts of the pipeline handle the range check
		/// calculations.
		/// </para>
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint UpdateDataCommon(byte[] input, int offset, uint[] crcTable, byte x1, byte x2, byte x3, byte x4)
		{
			uint result;
			uint a1 = crcTable[x1 + 3840] ^ crcTable[x2 + 3584];
			uint a2 = crcTable[x3 + 3328] ^ crcTable[x4 + 3072];

			result = crcTable[input[offset + 4] + 2816];
			result ^= crcTable[input[offset + 5] + 2560];
			a1 ^= crcTable[input[offset + 9] + 1536];
			result ^= crcTable[input[offset + 6] + 2304];
			result ^= crcTable[input[offset + 7] + 2048];
			result ^= crcTable[input[offset + 8] + 1792];
			a2 ^= crcTable[input[offset + 13] + 512];
			result ^= crcTable[input[offset + 10] + 1280];
			result ^= crcTable[input[offset + 11] + 1024];
			result ^= crcTable[input[offset + 12] + 768];
			result ^= a1;
			result ^= crcTable[input[offset + 14] + 256];
			result ^= crcTable[input[offset + 15]];
			result ^= a2;

			return result;
		}
	}
}
