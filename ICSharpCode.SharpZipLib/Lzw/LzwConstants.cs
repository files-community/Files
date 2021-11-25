namespace ICSharpCode.SharpZipLib.Lzw
{
	/// <summary>
	/// This class contains constants used for LZW
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "kept for backwards compatibility")]
	sealed public class LzwConstants
	{
		/// <summary>
		/// Magic number found at start of LZW header: 0x1f 0x9d
		/// </summary>
		public const int MAGIC = 0x1f9d;

		/// <summary>
		/// Maximum number of bits per code
		/// </summary>
		public const int MAX_BITS = 16;

		/* 3rd header byte:
         * bit 0..4 Number of compression bits
         * bit 5    Extended header
         * bit 6    Free
         * bit 7    Block mode
         */

		/// <summary>
		/// Mask for 'number of compression bits'
		/// </summary>
		public const int BIT_MASK = 0x1f;

		/// <summary>
		/// Indicates the presence of a fourth header byte
		/// </summary>
		public const int EXTENDED_MASK = 0x20;

		//public const int FREE_MASK      = 0x40;

		/// <summary>
		/// Reserved bits
		/// </summary>
		public const int RESERVED_MASK = 0x60;

		/// <summary>
		/// Block compression: if table is full and compression rate is dropping,
		/// clear the dictionary.
		/// </summary>
		public const int BLOCK_MODE_MASK = 0x80;

		/// <summary>
		/// LZW file header size (in bytes)
		/// </summary>
		public const int HDR_SIZE = 3;

		/// <summary>
		/// Initial number of bits per code
		/// </summary>
		public const int INIT_BITS = 9;

		private LzwConstants()
		{
		}
	}
}
