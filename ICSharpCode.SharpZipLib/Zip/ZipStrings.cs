using System;
using System.Text;
using ICSharpCode.SharpZipLib.Core;

namespace ICSharpCode.SharpZipLib.Zip
{
	internal static class EncodingExtensions
	{
		public static bool IsZipUnicode(this Encoding e)
			=> e.Equals(StringCodec.UnicodeZipEncoding);
	}
	
	/// <summary>
	/// Deprecated way of setting zip encoding provided for backwards compability.
	/// Use <see cref="StringCodec"/> when possible.
	/// </summary>
	/// <remarks>
	/// If any ZipStrings properties are being modified, it will enter a backwards compatibility mode, mimicking the
	/// old behaviour where a single instance was shared between all Zip* instances.
	/// </remarks>
	public static class ZipStrings
	{
		static readonly StringCodec CompatCodec = new StringCodec();

		private static bool compatibilityMode;
		
		/// <summary>
		/// Returns a new <see cref="StringCodec"/> instance or the shared backwards compatible instance.
		/// </summary>
		/// <returns></returns>
		public static StringCodec GetStringCodec() 
			=> compatibilityMode ? CompatCodec : new StringCodec();

		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		public static int CodePage
		{
			get => CompatCodec.CodePage;
			set
			{
				CompatCodec.CodePage = value;
				compatibilityMode = true;
			}
		}

		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		public static int SystemDefaultCodePage => StringCodec.SystemDefaultCodePage;

		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		public static bool UseUnicode
		{
			get => !CompatCodec.ForceZipLegacyEncoding;
			set
			{
				CompatCodec.ForceZipLegacyEncoding = !value;
				compatibilityMode = true;
			}
		}

		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		private static bool HasUnicodeFlag(int flags)
			=> ((GeneralBitFlags)flags).HasFlag(GeneralBitFlags.UnicodeText);
		
		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		public static string ConvertToString(byte[] data, int count)
			=> CompatCodec.ZipOutputEncoding.GetString(data, 0, count);

		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		public static string ConvertToString(byte[] data)
			=> CompatCodec.ZipOutputEncoding.GetString(data);
		
		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		public static string ConvertToStringExt(int flags, byte[] data, int count)
			=> CompatCodec.ZipEncoding(HasUnicodeFlag(flags)).GetString(data, 0, count);

		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		public static string ConvertToStringExt(int flags, byte[] data)
			=> CompatCodec.ZipEncoding(HasUnicodeFlag(flags)).GetString(data);

		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		public static byte[] ConvertToArray(string str)
			=> ConvertToArray(0, str);
		
		/// <inheritdoc cref="ZipStrings"/>
		[Obsolete("Use ZipFile/Zip*Stream StringCodec instead")]
		public static byte[] ConvertToArray(int flags, string str)
			=> (string.IsNullOrEmpty(str))
				? Empty.Array<byte>()
				: CompatCodec.ZipEncoding(HasUnicodeFlag(flags)).GetBytes(str);
	}

	/// <summary>
	/// Utility class for resolving the encoding used for reading and writing strings
	/// </summary>
	public class StringCodec
	{
		static StringCodec()
		{
			try
			{
				var platformCodepage = Encoding.Default.CodePage;
				SystemDefaultCodePage = (platformCodepage == 1 || platformCodepage == 2 || platformCodepage == 3 || platformCodepage == 42) ? FallbackCodePage : platformCodepage;
			}
			catch
			{
				SystemDefaultCodePage = FallbackCodePage;
			}

			SystemDefaultEncoding = Encoding.GetEncoding(SystemDefaultCodePage);
		}

		/// <summary>
		/// If set, use the encoding set by <see cref="CodePage"/> for zip entries instead of the defaults
		/// </summary>
		public bool ForceZipLegacyEncoding { get; set; }

		/// <summary>
		/// The default encoding used for ZipCrypto passwords in zip files, set to <see cref="SystemDefaultEncoding"/>
		/// for greatest compability.
		/// </summary>
		public static Encoding DefaultZipCryptoEncoding => SystemDefaultEncoding;

		/// <summary>
		/// Returns the encoding for an output <see cref="ZipEntry"/>.
		/// Unless overriden by <see cref="ForceZipLegacyEncoding"/> it returns <see cref="UnicodeZipEncoding"/>.
		/// </summary>
		public Encoding ZipOutputEncoding => ZipEncoding(!ForceZipLegacyEncoding);

		/// <summary>
		/// Returns <see cref="UnicodeZipEncoding"/> if <paramref name="unicode"/> is set, otherwise it returns the encoding indicated by <see cref="CodePage"/>
		/// </summary>
		public Encoding ZipEncoding(bool unicode) => unicode ? UnicodeZipEncoding : _legacyEncoding;

		/// <summary>
		/// Returns the appropriate encoding for an input <see cref="ZipEntry"/> according to <paramref name="flags"/>.
		/// If overridden by <see cref="ForceZipLegacyEncoding"/>, it always returns the encoding indicated by <see cref="CodePage"/>.
		/// </summary>
		/// <param name="flags"></param>
		/// <returns></returns>
		public Encoding ZipInputEncoding(GeneralBitFlags flags) => ZipInputEncoding((int)flags);

		/// <inheritdoc cref="ZipInputEncoding(GeneralBitFlags)"/>
		public Encoding ZipInputEncoding(int flags) => ZipEncoding(!ForceZipLegacyEncoding && (flags & (int)GeneralBitFlags.UnicodeText) != 0);

		/// <summary>Code page encoding, used for non-unicode strings</summary>
		/// <remarks>
		/// The original Zip specification (https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT) states
		/// that file names should only be encoded with IBM Code Page 437 or UTF-8.
		/// In practice, most zip apps use OEM or system encoding (typically cp437 on Windows).
		/// Let's be good citizens and default to UTF-8 http://utf8everywhere.org/
		/// </remarks>
		private Encoding _legacyEncoding = SystemDefaultEncoding;

		private Encoding _zipArchiveCommentEncoding;
		private Encoding _zipCryptoEncoding;

		/// <summary>
		/// Returns the UTF-8 code page (65001) used for zip entries with unicode flag set
		/// </summary>
		public static readonly Encoding UnicodeZipEncoding = Encoding.UTF8;

		/// <summary>
		/// Code page used for non-unicode strings and legacy zip encoding (if <see cref="ForceZipLegacyEncoding"/> is set).
		/// Default value is <see cref="SystemDefaultCodePage"/>
		/// </summary>
		public int CodePage
		{
			get => _legacyEncoding.CodePage;
			set => _legacyEncoding = (value < 4 || value > 65535 || value == 42)
				? throw new ArgumentOutOfRangeException(nameof(value))
				: Encoding.GetEncoding(value);
		}

		private const int FallbackCodePage = 437;

		/// <summary>
		/// Operating system default codepage, or if it could not be retrieved, the fallback code page IBM 437.
		/// </summary>
		public static int SystemDefaultCodePage { get; }

		/// <summary>
		/// The system default encoding, based on <see cref="SystemDefaultCodePage"/>
		/// </summary>
		public static Encoding SystemDefaultEncoding { get; }

		/// <summary>
		/// The encoding used for the zip archive comment. Defaults to the encoding for <see cref="CodePage"/>, since
		/// no unicode flag can be set for it in the files.
		/// </summary>
		public Encoding ZipArchiveCommentEncoding
		{
			get => _zipArchiveCommentEncoding ?? _legacyEncoding;
			set => _zipArchiveCommentEncoding = value;
		}

		/// <summary>
		/// The encoding used for the ZipCrypto passwords. Defaults to <see cref="DefaultZipCryptoEncoding"/>.
		/// </summary>
		public Encoding ZipCryptoEncoding
		{
			get => _zipCryptoEncoding ?? DefaultZipCryptoEncoding;
			set => _zipCryptoEncoding = value;
		}
	}
}
