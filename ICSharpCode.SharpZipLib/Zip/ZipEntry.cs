using System;
using System.IO;
using System.Text;

namespace ICSharpCode.SharpZipLib.Zip
{
	/// <summary>
	/// Defines known values for the <see cref="HostSystemID"/> property.
	/// </summary>
	public enum HostSystemID
	{
		/// <summary>
		/// Host system = MSDOS
		/// </summary>
		Msdos = 0,

		/// <summary>
		/// Host system = Amiga
		/// </summary>
		Amiga = 1,

		/// <summary>
		/// Host system = Open VMS
		/// </summary>
		OpenVms = 2,

		/// <summary>
		/// Host system = Unix
		/// </summary>
		Unix = 3,

		/// <summary>
		/// Host system = VMCms
		/// </summary>
		VMCms = 4,

		/// <summary>
		/// Host system = Atari ST
		/// </summary>
		AtariST = 5,

		/// <summary>
		/// Host system = OS2
		/// </summary>
		OS2 = 6,

		/// <summary>
		/// Host system = Macintosh
		/// </summary>
		Macintosh = 7,

		/// <summary>
		/// Host system = ZSystem
		/// </summary>
		ZSystem = 8,

		/// <summary>
		/// Host system = Cpm
		/// </summary>
		Cpm = 9,

		/// <summary>
		/// Host system = Windows NT
		/// </summary>
		WindowsNT = 10,

		/// <summary>
		/// Host system = MVS
		/// </summary>
		MVS = 11,

		/// <summary>
		/// Host system = VSE
		/// </summary>
		Vse = 12,

		/// <summary>
		/// Host system = Acorn RISC
		/// </summary>
		AcornRisc = 13,

		/// <summary>
		/// Host system = VFAT
		/// </summary>
		Vfat = 14,

		/// <summary>
		/// Host system = Alternate MVS
		/// </summary>
		AlternateMvs = 15,

		/// <summary>
		/// Host system = BEOS
		/// </summary>
		BeOS = 16,

		/// <summary>
		/// Host system = Tandem
		/// </summary>
		Tandem = 17,

		/// <summary>
		/// Host system = OS400
		/// </summary>
		OS400 = 18,

		/// <summary>
		/// Host system = OSX
		/// </summary>
		OSX = 19,

		/// <summary>
		/// Host system = WinZIP AES
		/// </summary>
		WinZipAES = 99,
	}

	/// <summary>
	/// This class represents an entry in a zip archive.  This can be a file
	/// or a directory
	/// ZipFile and ZipInputStream will give you instances of this class as
	/// information about the members in an archive.  ZipOutputStream
	/// uses an instance of this class when creating an entry in a Zip file.
	/// <br/>
	/// <br/>Author of the original java version : Jochen Hoenicke
	/// </summary>
	public class ZipEntry
	{
		[Flags]
		private enum Known : byte
		{
			None = 0,
			Size = 0x01,
			CompressedSize = 0x02,
			Crc = 0x04,
			Time = 0x08,
			ExternalAttributes = 0x10,
		}

		#region Constructors

		/// <summary>
		/// Creates a zip entry with the given name.
		/// </summary>
		/// <param name="name">
		/// The name for this entry. Can include directory components.
		/// The convention for names is 'unix' style paths with relative names only.
		/// There are with no device names and path elements are separated by '/' characters.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// The name passed is null
		/// </exception>
		public ZipEntry(string name)
			: this(name, 0, ZipConstants.VersionMadeBy, CompressionMethod.Deflated, true)
		{
		}

		/// <summary>
		/// Creates a zip entry with the given name and version required to extract
		/// </summary>
		/// <param name="name">
		/// The name for this entry. Can include directory components.
		/// The convention for names is 'unix'  style paths with no device names and
		/// path elements separated by '/' characters.  This is not enforced see <see cref="CleanName(string)">CleanName</see>
		/// on how to ensure names are valid if this is desired.
		/// </param>
		/// <param name="versionRequiredToExtract">
		/// The minimum 'feature version' required this entry
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// The name passed is null
		/// </exception>
		internal ZipEntry(string name, int versionRequiredToExtract)
			: this(name, versionRequiredToExtract, ZipConstants.VersionMadeBy,
			CompressionMethod.Deflated, true)
		{
		}

		/// <summary>
		/// Initializes an entry with the given name and made by information
		/// </summary>
		/// <param name="name">Name for this entry</param>
		/// <param name="madeByInfo">Version and HostSystem Information</param>
		/// <param name="versionRequiredToExtract">Minimum required zip feature version required to extract this entry</param>
		/// <param name="method">Compression method for this entry.</param>
		/// <param name="unicode">Whether the entry uses unicode for name and comment</param>
		/// <exception cref="ArgumentNullException">
		/// The name passed is null
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// versionRequiredToExtract should be 0 (auto-calculate) or > 10
		/// </exception>
		/// <remarks>
		/// This constructor is used by the ZipFile class when reading from the central header
		/// It is not generally useful, use the constructor specifying the name only.
		/// </remarks>
		internal ZipEntry(string name, int versionRequiredToExtract, int madeByInfo,
			CompressionMethod method, bool unicode)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name));
			}

			if (name.Length > 0xffff)
			{
				throw new ArgumentException("Name is too long", nameof(name));
			}

			if ((versionRequiredToExtract != 0) && (versionRequiredToExtract < 10))
			{
				throw new ArgumentOutOfRangeException(nameof(versionRequiredToExtract));
			}

			this.DateTime = DateTime.Now;
			this.name = name;
			this.versionMadeBy = (ushort)madeByInfo;
			this.versionToExtract = (ushort)versionRequiredToExtract;
			this.method = method;

			IsUnicodeText = unicode;
		}

		/// <summary>
		/// Creates a deep copy of the given zip entry.
		/// </summary>
		/// <param name="entry">
		/// The entry to copy.
		/// </param>
		[Obsolete("Use Clone instead")]
		public ZipEntry(ZipEntry entry)
		{
			if (entry == null)
			{
				throw new ArgumentNullException(nameof(entry));
			}

			known = entry.known;
			name = entry.name;
			size = entry.size;
			compressedSize = entry.compressedSize;
			crc = entry.crc;
			dateTime = entry.DateTime;
			method = entry.method;
			comment = entry.comment;
			versionToExtract = entry.versionToExtract;
			versionMadeBy = entry.versionMadeBy;
			externalFileAttributes = entry.externalFileAttributes;
			flags = entry.flags;

			zipFileIndex = entry.zipFileIndex;
			offset = entry.offset;

			forceZip64_ = entry.forceZip64_;

			if (entry.extra != null)
			{
				extra = new byte[entry.extra.Length];
				Array.Copy(entry.extra, 0, extra, 0, entry.extra.Length);
			}
		}

		#endregion Constructors

		/// <summary>
		/// Get a value indicating whether the entry has a CRC value available.
		/// </summary>
		public bool HasCrc => (known & Known.Crc) != 0;

		/// <summary>
		/// Get/Set flag indicating if entry is encrypted.
		/// A simple helper routine to aid interpretation of <see cref="Flags">flags</see>
		/// </summary>
		/// <remarks>This is an assistant that interprets the <see cref="Flags">flags</see> property.</remarks>
		public bool IsCrypted
		{
			get => this.HasFlag(GeneralBitFlags.Encrypted);
			set => this.SetFlag(GeneralBitFlags.Encrypted, value);
		}

		/// <summary>
		/// Get / set a flag indicating whether entry name and comment text are
		/// encoded in <a href="http://www.unicode.org">unicode UTF8</a>.
		/// </summary>
		/// <remarks>This is an assistant that interprets the <see cref="Flags">flags</see> property.</remarks>
		public bool IsUnicodeText
		{
			get => this.HasFlag(GeneralBitFlags.UnicodeText);
			set => this.SetFlag(GeneralBitFlags.UnicodeText, value);
		}

		/// <summary>
		/// Value used during password checking for PKZIP 2.0 / 'classic' encryption.
		/// </summary>
		internal byte CryptoCheckValue
		{
			get => cryptoCheckValue_;
			set => cryptoCheckValue_ = value;
		}

		/// <summary>
		/// Get/Set general purpose bit flag for entry
		/// </summary>
		/// <remarks>
		/// General purpose bit flag<br/>
		/// <br/>
		/// Bit 0: If set, indicates the file is encrypted<br/>
		/// Bit 1-2 Only used for compression type 6 Imploding, and 8, 9 deflating<br/>
		/// Imploding:<br/>
		/// Bit 1 if set indicates an 8K sliding dictionary was used.  If clear a 4k dictionary was used<br/>
		/// Bit 2 if set indicates 3 Shannon-Fanno trees were used to encode the sliding dictionary, 2 otherwise<br/>
		/// <br/>
		/// Deflating:<br/>
		///   Bit 2    Bit 1<br/>
		///     0        0       Normal compression was used<br/>
		///     0        1       Maximum compression was used<br/>
		///     1        0       Fast compression was used<br/>
		///     1        1       Super fast compression was used<br/>
		/// <br/>
		/// Bit 3: If set, the fields crc-32, compressed size
		/// and uncompressed size are were not able to be written during zip file creation
		/// The correct values are held in a data descriptor immediately following the compressed data. <br/>
		/// Bit 4: Reserved for use by PKZIP for enhanced deflating<br/>
		/// Bit 5: If set indicates the file contains compressed patch data<br/>
		/// Bit 6: If set indicates strong encryption was used.<br/>
		/// Bit 7-10: Unused or reserved<br/>
		/// Bit 11: If set the name and comments for this entry are in <a href="http://www.unicode.org">unicode</a>.<br/>
		/// Bit 12-15: Unused or reserved<br/>
		/// </remarks>
		/// <seealso cref="IsUnicodeText"></seealso>
		/// <seealso cref="IsCrypted"></seealso>
		public int Flags
		{
			get => flags;
			set => flags = value;
		}

		/// <summary>
		/// Get/Set index of this entry in Zip file
		/// </summary>
		/// <remarks>This is only valid when the entry is part of a <see cref="ZipFile"></see></remarks>
		public long ZipFileIndex
		{
			get => zipFileIndex;
			set => zipFileIndex = value;
		}

		/// <summary>
		/// Get/set offset for use in central header
		/// </summary>
		public long Offset
		{
			get => offset;
			set => offset = value;
		}

		/// <summary>
		/// Get/Set external file attributes as an integer.
		/// The values of this are operating system dependent see
		/// <see cref="HostSystem">HostSystem</see> for details
		/// </summary>
		public int ExternalFileAttributes
		{
			get => (known & Known.ExternalAttributes) == 0 ? -1 : externalFileAttributes;

			set
			{
				externalFileAttributes = value;
				known |= Known.ExternalAttributes;
			}
		}

		/// <summary>
		/// Get the version made by for this entry or zero if unknown.
		/// The value / 10 indicates the major version number, and
		/// the value mod 10 is the minor version number
		/// </summary>
		public int VersionMadeBy => versionMadeBy & 0xff;

		/// <summary>
		/// Get a value indicating this entry is for a DOS/Windows system.
		/// </summary>
		public bool IsDOSEntry
			=> (HostSystem == (int)HostSystemID.Msdos) 
			|| (HostSystem == (int)HostSystemID.WindowsNT);

		/// <summary>
		/// Test the external attributes for this <see cref="ZipEntry"/> to
		/// see if the external attributes are Dos based (including WINNT and variants)
		/// and match the values
		/// </summary>
		/// <param name="attributes">The attributes to test.</param>
		/// <returns>Returns true if the external attributes are known to be DOS/Windows
		/// based and have the same attributes set as the value passed.</returns>
		private bool HasDosAttributes(int attributes)
		{
			bool result = false;
			if ((known & Known.ExternalAttributes) != 0)
			{
				result |= (((HostSystem == (int)HostSystemID.Msdos) ||
					(HostSystem == (int)HostSystemID.WindowsNT)) &&
					(ExternalFileAttributes & attributes) == attributes);
			}
			return result;
		}

		/// <summary>
		/// Gets the compatibility information for the <see cref="ExternalFileAttributes">external file attribute</see>
		/// If the external file attributes are compatible with MS-DOS and can be read
		/// by PKZIP for DOS version 2.04g then this value will be zero.  Otherwise the value
		/// will be non-zero and identify the host system on which the attributes are compatible.
		/// </summary>
		///
		/// <remarks>
		/// The values for this as defined in the Zip File format and by others are shown below.  The values are somewhat
		/// misleading in some cases as they are not all used as shown.  You should consult the relevant documentation
		/// to obtain up to date and correct information.  The modified appnote by the infozip group is
		/// particularly helpful as it documents a lot of peculiarities.  The document is however a little dated.
		/// <list type="table">
		/// <item>0 - MS-DOS and OS/2 (FAT / VFAT / FAT32 file systems)</item>
		/// <item>1 - Amiga</item>
		/// <item>2 - OpenVMS</item>
		/// <item>3 - Unix</item>
		/// <item>4 - VM/CMS</item>
		/// <item>5 - Atari ST</item>
		/// <item>6 - OS/2 HPFS</item>
		/// <item>7 - Macintosh</item>
		/// <item>8 - Z-System</item>
		/// <item>9 - CP/M</item>
		/// <item>10 - Windows NTFS</item>
		/// <item>11 - MVS (OS/390 - Z/OS)</item>
		/// <item>12 - VSE</item>
		/// <item>13 - Acorn Risc</item>
		/// <item>14 - VFAT</item>
		/// <item>15 - Alternate MVS</item>
		/// <item>16 - BeOS</item>
		/// <item>17 - Tandem</item>
		/// <item>18 - OS/400</item>
		/// <item>19 - OS/X (Darwin)</item>
		/// <item>99 - WinZip AES</item>
		/// <item>remainder - unused</item>
		/// </list>
		/// </remarks>
		public int HostSystem
		{
			get => (versionMadeBy >> 8) & 0xff;

			set
			{
				versionMadeBy &= 0x00ff;
				versionMadeBy |= (ushort)((value & 0xff) << 8);
			}
		}

		/// <summary>
		/// Get minimum Zip feature version required to extract this entry
		/// </summary>
		/// <remarks>
		/// Minimum features are defined as:<br/>
		/// 1.0 - Default value<br/>
		/// 1.1 - File is a volume label<br/>
		/// 2.0 - File is a folder/directory<br/>
		/// 2.0 - File is compressed using Deflate compression<br/>
		/// 2.0 - File is encrypted using traditional encryption<br/>
		/// 2.1 - File is compressed using Deflate64<br/>
		/// 2.5 - File is compressed using PKWARE DCL Implode<br/>
		/// 2.7 - File is a patch data set<br/>
		/// 4.5 - File uses Zip64 format extensions<br/>
		/// 4.6 - File is compressed using BZIP2 compression<br/>
		/// 5.0 - File is encrypted using DES<br/>
		/// 5.0 - File is encrypted using 3DES<br/>
		/// 5.0 - File is encrypted using original RC2 encryption<br/>
		/// 5.0 - File is encrypted using RC4 encryption<br/>
		/// 5.1 - File is encrypted using AES encryption<br/>
		/// 5.1 - File is encrypted using corrected RC2 encryption<br/>
		/// 5.1 - File is encrypted using corrected RC2-64 encryption<br/>
		/// 6.1 - File is encrypted using non-OAEP key wrapping<br/>
		/// 6.2 - Central directory encryption (not confirmed yet)<br/>
		/// 6.3 - File is compressed using LZMA<br/>
		/// 6.3 - File is compressed using PPMD+<br/>
		/// 6.3 - File is encrypted using Blowfish<br/>
		/// 6.3 - File is encrypted using Twofish<br/>
		/// </remarks>
		/// <seealso cref="CanDecompress"></seealso>
		public int Version
		{
			get
			{
				// Return recorded version if known.
				if (versionToExtract != 0)
					// Only lower order byte. High order is O/S file system.
					return versionToExtract & 0x00ff;

				if (AESKeySize > 0)
					// Ver 5.1 = AES
					return ZipConstants.VERSION_AES;

				if (CompressionMethod.BZip2 == method)
					return ZipConstants.VersionBZip2;

				if (CentralHeaderRequiresZip64)
					return ZipConstants.VersionZip64;

				if (CompressionMethod.Deflated == method || IsDirectory || IsCrypted)
					return 20;
				
				if (HasDosAttributes(0x08))
					return 11;
				
				return 10;
			}
		}

		/// <summary>
		/// Get a value indicating whether this entry can be decompressed by the library.
		/// </summary>
		/// <remarks>This is based on the <see cref="Version"></see> and
		/// whether the <see cref="IsCompressionMethodSupported()">compression method</see> is supported.</remarks>
		public bool CanDecompress 
			=> Version <= ZipConstants.VersionMadeBy 
			&& (Version == 10 || Version == 11 || Version == 20 || Version == 45 || Version == 46 || Version == 51) 
			&& IsCompressionMethodSupported();

		/// <summary>
		/// Force this entry to be recorded using Zip64 extensions.
		/// </summary>
		public void ForceZip64() => forceZip64_ = true;

		/// <summary>
		/// Get a value indicating whether Zip64 extensions were forced.
		/// </summary>
		/// <returns>A <see cref="bool"/> value of true if Zip64 extensions have been forced on; false if not.</returns>
		public bool IsZip64Forced() => forceZip64_;

		/// <summary>
		/// Gets a value indicating if the entry requires Zip64 extensions
		/// to store the full entry values.
		/// </summary>
		/// <value>A <see cref="bool"/> value of true if a local header requires Zip64 extensions; false if not.</value>
		public bool LocalHeaderRequiresZip64
		{
			get
			{
				bool result = forceZip64_;

				if (!result)
				{
					ulong trueCompressedSize = compressedSize;

					if ((versionToExtract == 0) && IsCrypted)
					{
						trueCompressedSize += (ulong)this.EncryptionOverheadSize;
					}

					// TODO: A better estimation of the true limit based on compression overhead should be used
					// to determine when an entry should use Zip64.
					result =
						((this.size >= uint.MaxValue) || (trueCompressedSize >= uint.MaxValue)) &&
						((versionToExtract == 0) || (versionToExtract >= ZipConstants.VersionZip64));
				}

				return result;
			}
		}

		/// <summary>
		/// Get a value indicating whether the central directory entry requires Zip64 extensions to be stored.
		/// </summary>
		public bool CentralHeaderRequiresZip64 
			=> LocalHeaderRequiresZip64 || (offset >= uint.MaxValue);

		/// <summary>
		/// Get/Set DosTime value.
		/// </summary>
		/// <remarks>
		/// The MS-DOS date format can only represent dates between 1/1/1980 and 12/31/2107.
		/// </remarks>
		public long DosTime
		{
			get
			{
				if ((known & Known.Time) == 0)
				{
					return 0;
				}

				var year = (uint)DateTime.Year;
				var month = (uint)DateTime.Month;
				var day = (uint)DateTime.Day;
				var hour = (uint)DateTime.Hour;
				var minute = (uint)DateTime.Minute;
				var second = (uint)DateTime.Second;

				if (year < 1980)
				{
					year = 1980;
					month = 1;
					day = 1;
					hour = 0;
					minute = 0;
					second = 0;
				}
				else if (year > 2107)
				{
					year = 2107;
					month = 12;
					day = 31;
					hour = 23;
					minute = 59;
					second = 59;
				}

				return ((year - 1980) & 0x7f) << 25 |
				       (month << 21) |
				       (day << 16) |
				       (hour << 11) |
				       (minute << 5) |
				       (second >> 1);
			}

			set
			{
				unchecked
				{
					var dosTime = (uint)value;
					uint sec = Math.Min(59, 2 * (dosTime & 0x1f));
					uint min = Math.Min(59, (dosTime >> 5) & 0x3f);
					uint hrs = Math.Min(23, (dosTime >> 11) & 0x1f);
					uint mon = Math.Max(1, Math.Min(12, ((uint)(value >> 21) & 0xf)));
					uint year = ((dosTime >> 25) & 0x7f) + 1980;
					int day = Math.Max(1, Math.Min(DateTime.DaysInMonth((int)year, (int)mon), (int)((value >> 16) & 0x1f)));
					DateTime = new DateTime((int)year, (int)mon, day, (int)hrs, (int)min, (int)sec, DateTimeKind.Unspecified);
				}
			}
		}

		/// <summary>
		/// Gets/Sets the time of last modification of the entry.
		/// </summary>
		/// <remarks>
		/// The <see cref="DosTime"></see> property is updated to match this as far as possible.
		/// </remarks>
		public DateTime DateTime
		{
			get => dateTime;

			set
			{
				dateTime = value;
				known |= Known.Time;
			}
		}

		/// <summary>
		/// Returns the entry name.
		/// </summary>
		/// <remarks>
		/// The unix naming convention is followed.
		/// Path components in the entry should always separated by forward slashes ('/').
		/// Dos device names like C: should also be removed.
		/// See the <see cref="ZipNameTransform"/> class, or <see cref="CleanName(string)"/>
		///</remarks>
		public string Name
		{
			get => name;
			internal set => name = value;
		}

		/// <summary>
		/// Gets/Sets the size of the uncompressed data.
		/// </summary>
		/// <returns>
		/// The size or -1 if unknown.
		/// </returns>
		/// <remarks>Setting the size before adding an entry to an archive can help
		/// avoid compatibility problems with some archivers which don't understand Zip64 extensions.</remarks>
		public long Size
		{
			get => (known & Known.Size) != 0 ? (long)size : -1L;
			set
			{
				size = (ulong)value;
				known |= Known.Size;
			}
		}

		/// <summary>
		/// Gets/Sets the size of the compressed data.
		/// </summary>
		/// <returns>
		/// The compressed entry size or -1 if unknown.
		/// </returns>
		public long CompressedSize
		{
			get => (known & Known.CompressedSize) != 0 ? (long)compressedSize : -1L;
			set
			{
				compressedSize = (ulong)value;
				known |= Known.CompressedSize;
			}
		}

		/// <summary>
		/// Gets/Sets the crc of the uncompressed data.
		/// </summary>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// Crc is not in the range 0..0xffffffffL
		/// </exception>
		/// <returns>
		/// The crc value or -1 if unknown.
		/// </returns>
		public long Crc
		{
			get => (known & Known.Crc) != 0 ? crc & 0xffffffffL : -1L;
			set
			{
				if ((crc & 0xffffffff00000000L) != 0)
				{
					throw new ArgumentOutOfRangeException(nameof(value));
				}
				this.crc = (uint)value;
				this.known |= Known.Crc;
			}
		}

		/// <summary>
		/// Gets/Sets the compression method.
		/// </summary>
		/// <returns>
		/// The compression method for this entry
		/// </returns>
		public CompressionMethod CompressionMethod
		{
			get => method;
			set => method = value;
		}

		/// <summary>
		/// Gets the compression method for outputting to the local or central header.
		/// Returns same value as CompressionMethod except when AES encrypting, which
		/// places 99 in the method and places the real method in the extra data.
		/// </summary>
		internal CompressionMethod CompressionMethodForHeader 
			=> (AESKeySize > 0) ? CompressionMethod.WinZipAES : method;

		/// <summary>
		/// Gets/Sets the extra data.
		/// </summary>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// Extra data is longer than 64KB (0xffff) bytes.
		/// </exception>
		/// <returns>
		/// Extra data or null if not set.
		/// </returns>
		public byte[] ExtraData
		{
			// TODO: This is slightly safer but less efficient.  Think about whether it should change.
			//				return (byte[]) extra.Clone();
			get => extra;

			set
			{
				if (value == null)
				{
					extra = null;
				}
				else
				{
					if (value.Length > 0xffff)
					{
						throw new System.ArgumentOutOfRangeException(nameof(value));
					}

					extra = new byte[value.Length];
					Array.Copy(value, 0, extra, 0, value.Length);
				}
			}
		}

		/// <summary>
		/// For AES encrypted files returns or sets the number of bits of encryption (128, 192 or 256).
		/// When setting, only 0 (off), 128 or 256 is supported.
		/// </summary>
		public int AESKeySize
		{
			get
			{
				// the strength (1 or 3) is in the entry header
				switch (_aesEncryptionStrength)
				{
					case 0:
						return 0;   // Not AES
					case 1:
						return 128;

					case 2:
						return 192; // Not used by WinZip
					case 3:
						return 256;

					default:
						throw new ZipException("Invalid AESEncryptionStrength " + _aesEncryptionStrength);
				}
			}
			set
			{
				switch (value)
				{
					case 0:
						_aesEncryptionStrength = 0;
						break;

					case 128:
						_aesEncryptionStrength = 1;
						break;

					case 256:
						_aesEncryptionStrength = 3;
						break;

					default:
						throw new ZipException("AESKeySize must be 0, 128 or 256: " + value);
				}
			}
		}

		/// <summary>
		/// AES Encryption strength for storage in extra data in entry header.
		/// 1 is 128 bit, 2 is 192 bit, 3 is 256 bit.
		/// </summary>
		internal byte AESEncryptionStrength => (byte)_aesEncryptionStrength;

		/// <summary>
		/// Returns the length of the salt, in bytes
		/// </summary>
		/// Key size -> Salt length: 128 bits = 8 bytes, 192 bits = 12 bytes, 256 bits = 16 bytes.
		internal int AESSaltLen => AESKeySize / 16;

		/// <summary>
		/// Number of extra bytes required to hold the AES Header fields (Salt, Pwd verify, AuthCode)
		/// </summary>
		/// File format:
		/// Bytes	 |	Content
		/// ---------+---------------------------
		/// Variable |	Salt value
		/// 2		 |	Password verification value
		/// Variable |	Encrypted file data
		/// 10		 |	Authentication code
		internal int AESOverheadSize => 12 + AESSaltLen;

		/// <summary>
		/// Number of extra bytes required to hold the encryption header fields.
		/// </summary>
		internal int EncryptionOverheadSize =>
			!IsCrypted
				// Entry is not encrypted - no overhead
				? 0
				: _aesEncryptionStrength == 0
					// Entry is encrypted using ZipCrypto
					? ZipConstants.CryptoHeaderSize
					// Entry is encrypted using AES
					: AESOverheadSize;

		/// <summary>
		/// Process extra data fields updating the entry based on the contents.
		/// </summary>
		/// <param name="localHeader">True if the extra data fields should be handled
		/// for a local header, rather than for a central header.
		/// </param>
		internal void ProcessExtraData(bool localHeader)
		{
			var extraData = new ZipExtraData(this.extra);

			if (extraData.Find(0x0001))
			{
				// Version required to extract is ignored here as some archivers dont set it correctly
				// in theory it should be version 45 or higher

				// The recorded size will change but remember that this is zip64.
				forceZip64_ = true;

				if (extraData.ValueLength < 4)
				{
					throw new ZipException("Extra data extended Zip64 information length is invalid");
				}

				// (localHeader ||) was deleted, because actually there is no specific difference with reading sizes between local header & central directory
				// https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT
				// ...
				// 4.4  Explanation of fields
				// ...
				//	4.4.8 compressed size: (4 bytes)
				//	4.4.9 uncompressed size: (4 bytes)
				//
				//		The size of the file compressed (4.4.8) and uncompressed,
				//		(4.4.9) respectively.  When a decryption header is present it
				//		will be placed in front of the file data and the value of the
				//		compressed file size will include the bytes of the decryption
				//		header.  If bit 3 of the general purpose bit flag is set,
				//		these fields are set to zero in the local header and the
				//		correct values are put in the data descriptor and
				//		in the central directory.  If an archive is in ZIP64 format
				//		and the value in this field is 0xFFFFFFFF, the size will be
				//		in the corresponding 8 byte ZIP64 extended information
				//		extra field.  When encrypting the central directory, if the
				//		local header is not in ZIP64 format and general purpose bit
				//		flag 13 is set indicating masking, the value stored for the
				//		uncompressed size in the Local Header will be zero.
				//
				// Otherwise there is problem with minizip implementation
				if (size == uint.MaxValue)
				{
					size = (ulong)extraData.ReadLong();
				}

				if (compressedSize == uint.MaxValue)
				{
					compressedSize = (ulong)extraData.ReadLong();
				}

				if (!localHeader && (offset == uint.MaxValue))
				{
					offset = extraData.ReadLong();
				}

				// Disk number on which file starts is ignored
			}
			else
			{
				if (
					((versionToExtract & 0xff) >= ZipConstants.VersionZip64) &&
					((size == uint.MaxValue) || (compressedSize == uint.MaxValue))
				)
				{
					throw new ZipException("Zip64 Extended information required but is missing.");
				}
			}

			DateTime = GetDateTime(extraData) ?? DateTime;
			if (method == CompressionMethod.WinZipAES)
			{
				ProcessAESExtraData(extraData);
			}
		}

		private static DateTime? GetDateTime(ZipExtraData extraData)
		{
			// Check for NT timestamp
			// NOTE: Disable by default to match behavior of InfoZIP
#if RESPECT_NT_TIMESTAMP
			NTTaggedData ntData = extraData.GetData<NTTaggedData>();
			if (ntData != null)
				return ntData.LastModificationTime;
#endif

			// Check for Unix timestamp
			ExtendedUnixData unixData = extraData.GetData<ExtendedUnixData>();
			if (unixData != null && unixData.Include.HasFlag(ExtendedUnixData.Flags.ModificationTime))
				return unixData.ModificationTime;

			return null;
		}

		// For AES the method in the entry is 99, and the real compression method is in the extradata
		private void ProcessAESExtraData(ZipExtraData extraData)
		{
			if (extraData.Find(0x9901))
			{
				// Set version for Zipfile.CreateAndInitDecryptionStream
				versionToExtract = ZipConstants.VERSION_AES;            // Ver 5.1 = AES see "Version" getter

				//
				// Unpack AES extra data field see http://www.winzip.com/aes_info.htm
				int length = extraData.ValueLength;         // Data size currently 7
				if (length < 7)
					throw new ZipException("AES Extra Data Length " + length + " invalid.");
				int ver = extraData.ReadShort();            // Version number (1=AE-1 2=AE-2)
				int vendorId = extraData.ReadShort();       // 2-character vendor ID 0x4541 = "AE"
				int encrStrength = extraData.ReadByte();    // encryption strength 1 = 128 2 = 192 3 = 256
				int actualCompress = extraData.ReadShort(); // The actual compression method used to compress the file
				_aesVer = ver;
				_aesEncryptionStrength = encrStrength;
				method = (CompressionMethod)actualCompress;
			}
			else
				throw new ZipException("AES Extra Data missing");
		}

		/// <summary>
		/// Gets/Sets the entry comment.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">
		/// If comment is longer than 0xffff.
		/// </exception>
		/// <returns>
		/// The comment or null if not set.
		/// </returns>
		/// <remarks>
		/// A comment is only available for entries when read via the <see cref="ZipFile"/> class.
		/// The <see cref="ZipInputStream"/> class doesn't have the comment data available.
		/// </remarks>
		public string Comment
		{
			get => comment;
			set
			{
				// This test is strictly incorrect as the length is in characters
				// while the storage limit is in bytes.
				// While the test is partially correct in that a comment of this length or greater
				// is definitely invalid, shorter comments may also have an invalid length
				// where there are multi-byte characters
				// The full test is not possible here however as the code page to apply conversions with
				// isn't available.
				if ((value != null) && (value.Length > 0xffff))
				{
					throw new ArgumentOutOfRangeException(nameof(value), "cannot exceed 65535");
				}

				comment = value;
			}
		}

		/// <summary>
		/// Gets a value indicating if the entry is a directory.
		/// however.
		/// </summary>
		/// <remarks>
		/// A directory is determined by an entry name with a trailing slash '/'.
		/// The external file attributes can also indicate an entry is for a directory.
		/// Currently only dos/windows attributes are tested in this manner.
		/// The trailing slash convention should always be followed.
		/// </remarks>
		public bool IsDirectory 
			=> name.Length > 0 
			&& (name[name.Length - 1] == '/' || name[name.Length - 1] == '\\') || HasDosAttributes(16);

		/// <summary>
		/// Get a value of true if the entry appears to be a file; false otherwise
		/// </summary>
		/// <remarks>
		/// This only takes account of DOS/Windows attributes.  Other operating systems are ignored.
		/// For linux and others the result may be incorrect.
		/// </remarks>
		public bool IsFile => !IsDirectory && !HasDosAttributes(8);

		/// <summary>
		/// Test entry to see if data can be extracted.
		/// </summary>
		/// <returns>Returns true if data can be extracted for this entry; false otherwise.</returns>
		public bool IsCompressionMethodSupported() => IsCompressionMethodSupported(CompressionMethod);

		#region ICloneable Members

		/// <summary>
		/// Creates a copy of this zip entry.
		/// </summary>
		/// <returns>An <see cref="Object"/> that is a copy of the current instance.</returns>
		public object Clone()
		{
			var result = (ZipEntry)this.MemberwiseClone();

			// Ensure extra data is unique if it exists.
			if (extra != null)
			{
				result.extra = new byte[extra.Length];
				Array.Copy(extra, 0, result.extra, 0, extra.Length);
			}

			return result;
		}

		#endregion ICloneable Members

		/// <summary>
		/// Gets a string representation of this ZipEntry.
		/// </summary>
		/// <returns>A readable textual representation of this <see cref="ZipEntry"/></returns>
		public override string ToString() => name;

		/// <summary>
		/// Test a <see cref="CompressionMethod">compression method</see> to see if this library
		/// supports extracting data compressed with that method
		/// </summary>
		/// <param name="method">The compression method to test.</param>
		/// <returns>Returns true if the compression method is supported; false otherwise</returns>
		public static bool IsCompressionMethodSupported(CompressionMethod method) 
			=> method == CompressionMethod.Deflated
			|| method == CompressionMethod.Stored
			|| method == CompressionMethod.BZip2;

		/// <summary>
		/// Cleans a name making it conform to Zip file conventions.
		/// Devices names ('c:\') and UNC share names ('\\server\share') are removed
		/// and back slashes ('\') are converted to forward slashes ('/').
		/// Names are made relative by trimming leading slashes which is compatible
		/// with the ZIP naming convention.
		/// </summary>
		/// <param name="name">The name to clean</param>
		/// <returns>The 'cleaned' name.</returns>
		/// <remarks>
		/// The <seealso cref="ZipNameTransform">Zip name transform</seealso> class is more flexible.
		/// </remarks>
		public static string CleanName(string name)
		{
			if (name == null)
			{
				return string.Empty;
			}

			if (Path.IsPathRooted(name))
			{
				// NOTE:
				// for UNC names...  \\machine\share\zoom\beet.txt gives \zoom\beet.txt
				name = name.Substring(Path.GetPathRoot(name).Length);
			}

			name = name.Replace(@"\", "/");

			while ((name.Length > 0) && (name[0] == '/'))
			{
				name = name.Remove(0, 1);
			}
			return name;
		}

		#region Instance Fields

		private Known known;
		private int externalFileAttributes = -1;     // contains external attributes (O/S dependant)

		private ushort versionMadeBy;                   // Contains host system and version information
														// only relevant for central header entries

		private string name;
		private ulong size;
		private ulong compressedSize;
		private ushort versionToExtract;                // Version required to extract (library handles <= 2.0)
		private uint crc;
		private DateTime dateTime;

		private CompressionMethod method = CompressionMethod.Deflated;
		private byte[] extra;
		private string comment;

		private int flags;                             // general purpose bit flags

		private long zipFileIndex = -1;                // used by ZipFile
		private long offset;                           // used by ZipFile and ZipOutputStream

		private bool forceZip64_;
		private byte cryptoCheckValue_;
		private int _aesVer;                            // Version number (2 = AE-2 ?). Assigned but not used.
		private int _aesEncryptionStrength;             // Encryption strength 1 = 128 2 = 192 3 = 256

		#endregion Instance Fields
	}
}
