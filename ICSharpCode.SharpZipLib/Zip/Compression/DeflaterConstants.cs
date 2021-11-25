using System;

namespace ICSharpCode.SharpZipLib.Zip.Compression
{
	/// <summary>
	/// This class contains constants used for deflation.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "kept for backwards compatibility")]
	public static class DeflaterConstants
	{
		/// <summary>
		/// Set to true to enable debugging
		/// </summary>
		public const bool DEBUGGING = false;

		/// <summary>
		/// Written to Zip file to identify a stored block
		/// </summary>
		public const int STORED_BLOCK = 0;

		/// <summary>
		/// Identifies static tree in Zip file
		/// </summary>
		public const int STATIC_TREES = 1;

		/// <summary>
		/// Identifies dynamic tree in Zip file
		/// </summary>
		public const int DYN_TREES = 2;

		/// <summary>
		/// Header flag indicating a preset dictionary for deflation
		/// </summary>
		public const int PRESET_DICT = 0x20;

		/// <summary>
		/// Sets internal buffer sizes for Huffman encoding
		/// </summary>
		public const int DEFAULT_MEM_LEVEL = 8;

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public const int MAX_MATCH = 258;

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public const int MIN_MATCH = 3;

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public const int MAX_WBITS = 15;

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public const int WSIZE = 1 << MAX_WBITS;

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public const int WMASK = WSIZE - 1;

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public const int HASH_BITS = DEFAULT_MEM_LEVEL + 7;

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public const int HASH_SIZE = 1 << HASH_BITS;

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public const int HASH_MASK = HASH_SIZE - 1;

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public const int HASH_SHIFT = (HASH_BITS + MIN_MATCH - 1) / MIN_MATCH;

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public const int MIN_LOOKAHEAD = MAX_MATCH + MIN_MATCH + 1;

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public const int MAX_DIST = WSIZE - MIN_LOOKAHEAD;

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public const int PENDING_BUF_SIZE = 1 << (DEFAULT_MEM_LEVEL + 8);

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public static int MAX_BLOCK_SIZE = Math.Min(65535, PENDING_BUF_SIZE - 5);

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public const int DEFLATE_STORED = 0;

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public const int DEFLATE_FAST = 1;

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public const int DEFLATE_SLOW = 2;

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public static int[] GOOD_LENGTH = { 0, 4, 4, 4, 4, 8, 8, 8, 32, 32 };

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public static int[] MAX_LAZY = { 0, 4, 5, 6, 4, 16, 16, 32, 128, 258 };

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public static int[] NICE_LENGTH = { 0, 8, 16, 32, 16, 32, 128, 128, 258, 258 };

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public static int[] MAX_CHAIN = { 0, 4, 8, 32, 16, 32, 128, 256, 1024, 4096 };

		/// <summary>
		/// Internal compression engine constant
		/// </summary>
		public static int[] COMPR_FUNC = { 0, 1, 1, 1, 1, 2, 2, 2, 2, 2 };
	}
}
