using System;
using System.Text;

namespace ICSharpCode.SharpZipLib.GZip
{
	/// <summary>
	/// This class contains constants used for gzip.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "kept for backwards compatibility")]
	sealed public class GZipConstants
	{
		/// <summary>
		/// First GZip identification byte
		/// </summary>
		public const byte ID1 = 0x1F;

		/// <summary>
		/// Second GZip identification byte
		/// </summary>
		public const byte ID2 = 0x8B;

		/// <summary>
		/// Deflate compression method
		/// </summary>
		public const byte CompressionMethodDeflate = 0x8;

		/// <summary>
		/// Get the GZip specified encoding (CP-1252 if supported, otherwise ASCII)
		/// </summary>
		public static Encoding Encoding
		{
			get
			{
				try
				{
					return Encoding.GetEncoding(1252);
				}
				catch
				{
					return Encoding.ASCII;
				}
			}
		}

	}

	/// <summary>
	/// GZip header flags
	/// </summary>
	[Flags]
	public enum GZipFlags: byte
	{
		/// <summary>
		/// Text flag hinting that the file is in ASCII
		/// </summary>
		FTEXT = 0x1 << 0,

		/// <summary>
		/// CRC flag indicating that a CRC16 preceeds the data
		/// </summary>
		FHCRC = 0x1 << 1,

		/// <summary>
		/// Extra flag indicating that extra fields are present
		/// </summary>
		FEXTRA = 0x1 << 2,

		/// <summary>
		/// Filename flag indicating that the original filename is present
		/// </summary>
		FNAME = 0x1 << 3,

		/// <summary>
		/// Flag bit mask indicating that a comment is present
		/// </summary>
		FCOMMENT = 0x1 << 4,
	}
}
