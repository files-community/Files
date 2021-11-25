using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Core;

namespace ICSharpCode.SharpZipLib.Tar
{
	/// <summary>
	/// This class represents an entry in a Tar archive. It consists
	/// of the entry's header, as well as the entry's File. Entries
	/// can be instantiated in one of three ways, depending on how
	/// they are to be used.
	/// <p>
	/// TarEntries that are created from the header bytes read from
	/// an archive are instantiated with the TarEntry( byte[] )
	/// constructor. These entries will be used when extracting from
	/// or listing the contents of an archive. These entries have their
	/// header filled in using the header bytes. They also set the File
	/// to null, since they reference an archive entry not a file.</p>
	/// <p>
	/// TarEntries that are created from files that are to be written
	/// into an archive are instantiated with the CreateEntryFromFile(string)
	/// pseudo constructor. These entries have their header filled in using
	/// the File's information. They also keep a reference to the File
	/// for convenience when writing entries.</p>
	/// <p>
	/// Finally, TarEntries can be constructed from nothing but a name.
	/// This allows the programmer to construct the entry by hand, for
	/// instance when only an InputStream is available for writing to
	/// the archive, and the header information is constructed from
	/// other information. In this case the header fields are set to
	/// defaults and the File is set to null.</p>
	/// <see cref="TarHeader"/>
	/// </summary>
	public class TarEntry
	{
		#region Constructors

		/// <summary>
		/// Initialise a default instance of <see cref="TarEntry"/>.
		/// </summary>
		private TarEntry()
		{
			header = new TarHeader();
		}

		/// <summary>
		/// Construct an entry from an archive's header bytes. File is set
		/// to null.
		/// </summary>
		/// <param name = "headerBuffer">
		/// The header bytes from a tar archive entry.
		/// </param>
		[Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
		public TarEntry(byte[] headerBuffer) : this(headerBuffer, null)
		{
		}

		/// <summary>
		/// Construct an entry from an archive's header bytes. File is set
		/// to null.
		/// </summary>
		/// <param name = "headerBuffer">
		/// The header bytes from a tar archive entry.
		/// </param>
		/// <param name = "nameEncoding">
		/// The <see cref="Encoding"/> used for the Name fields, or null for ASCII only
		/// </param>
		public TarEntry(byte[] headerBuffer, Encoding nameEncoding)
		{
			header = new TarHeader();
			header.ParseBuffer(headerBuffer, nameEncoding);
		}

		/// <summary>
		/// Construct a TarEntry using the <paramref name="header">header</paramref> provided
		/// </summary>
		/// <param name="header">Header details for entry</param>
		public TarEntry(TarHeader header)
		{
			if (header == null)
			{
				throw new ArgumentNullException(nameof(header));
			}

			this.header = (TarHeader)header.Clone();
		}

		#endregion Constructors

		#region ICloneable Members

		/// <summary>
		/// Clone this tar entry.
		/// </summary>
		/// <returns>Returns a clone of this entry.</returns>
		public object Clone()
		{
			var entry = new TarEntry();
			entry.file = file;
			entry.header = (TarHeader)header.Clone();
			entry.Name = Name;
			return entry;
		}

		#endregion ICloneable Members

		/// <summary>
		/// Construct an entry with only a <paramref name="name">name</paramref>.
		/// This allows the programmer to construct the entry's header "by hand".
		/// </summary>
		/// <param name="name">The name to use for the entry</param>
		/// <returns>Returns the newly created <see cref="TarEntry"/></returns>
		public static TarEntry CreateTarEntry(string name)
		{
			var entry = new TarEntry();
			TarEntry.NameTarHeader(entry.header, name);
			return entry;
		}

		/// <summary>
		/// Construct an entry for a file. File is set to file, and the
		/// header is constructed from information from the file.
		/// </summary>
		/// <param name = "fileName">The file name that the entry represents.</param>
		/// <returns>Returns the newly created <see cref="TarEntry"/></returns>
		public static TarEntry CreateEntryFromFile(string fileName)
		{
			var entry = new TarEntry();
			entry.GetFileTarHeader(entry.header, fileName);
			return entry;
		}

		/// <summary>
		/// Determine if the two entries are equal. Equality is determined
		/// by the header names being equal.
		/// </summary>
		/// <param name="obj">The <see cref="Object"/> to compare with the current Object.</param>
		/// <returns>
		/// True if the entries are equal; false if not.
		/// </returns>
		public override bool Equals(object obj)
		{
			var localEntry = obj as TarEntry;

			if (localEntry != null)
			{
				return Name.Equals(localEntry.Name);
			}
			return false;
		}

		/// <summary>
		/// Derive a Hash value for the current <see cref="Object"/>
		/// </summary>
		/// <returns>A Hash code for the current <see cref="Object"/></returns>
		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		/// <summary>
		/// Determine if the given entry is a descendant of this entry.
		/// Descendancy is determined by the name of the descendant
		/// starting with this entry's name.
		/// </summary>
		/// <param name = "toTest">
		/// Entry to be checked as a descendent of this.
		/// </param>
		/// <returns>
		/// True if entry is a descendant of this.
		/// </returns>
		public bool IsDescendent(TarEntry toTest)
		{
			if (toTest == null)
			{
				throw new ArgumentNullException(nameof(toTest));
			}

			return toTest.Name.StartsWith(Name, StringComparison.Ordinal);
		}

		/// <summary>
		/// Get this entry's header.
		/// </summary>
		/// <returns>
		/// This entry's TarHeader.
		/// </returns>
		public TarHeader TarHeader
		{
			get
			{
				return header;
			}
		}

		/// <summary>
		/// Get/Set this entry's name.
		/// </summary>
		public string Name
		{
			get
			{
				return header.Name;
			}
			set
			{
				header.Name = value;
			}
		}

		/// <summary>
		/// Get/set this entry's user id.
		/// </summary>
		public int UserId
		{
			get
			{
				return header.UserId;
			}
			set
			{
				header.UserId = value;
			}
		}

		/// <summary>
		/// Get/set this entry's group id.
		/// </summary>
		public int GroupId
		{
			get
			{
				return header.GroupId;
			}
			set
			{
				header.GroupId = value;
			}
		}

		/// <summary>
		/// Get/set this entry's user name.
		/// </summary>
		public string UserName
		{
			get
			{
				return header.UserName;
			}
			set
			{
				header.UserName = value;
			}
		}

		/// <summary>
		/// Get/set this entry's group name.
		/// </summary>
		public string GroupName
		{
			get
			{
				return header.GroupName;
			}
			set
			{
				header.GroupName = value;
			}
		}

		/// <summary>
		/// Convenience method to set this entry's group and user ids.
		/// </summary>
		/// <param name="userId">
		/// This entry's new user id.
		/// </param>
		/// <param name="groupId">
		/// This entry's new group id.
		/// </param>
		public void SetIds(int userId, int groupId)
		{
			UserId = userId;
			GroupId = groupId;
		}

		/// <summary>
		/// Convenience method to set this entry's group and user names.
		/// </summary>
		/// <param name="userName">
		/// This entry's new user name.
		/// </param>
		/// <param name="groupName">
		/// This entry's new group name.
		/// </param>
		public void SetNames(string userName, string groupName)
		{
			UserName = userName;
			GroupName = groupName;
		}

		/// <summary>
		/// Get/Set the modification time for this entry
		/// </summary>
		public DateTime ModTime
		{
			get
			{
				return header.ModTime;
			}
			set
			{
				header.ModTime = value;
			}
		}

		/// <summary>
		/// Get this entry's file.
		/// </summary>
		/// <returns>
		/// This entry's file.
		/// </returns>
		public string File
		{
			get
			{
				return file;
			}
		}

		/// <summary>
		/// Get/set this entry's recorded file size.
		/// </summary>
		public long Size
		{
			get
			{
				return header.Size;
			}
			set
			{
				header.Size = value;
			}
		}

		/// <summary>
		/// Return true if this entry represents a directory, false otherwise
		/// </summary>
		/// <returns>
		/// True if this entry is a directory.
		/// </returns>
		public bool IsDirectory
		{
			get
			{
				if (file != null)
				{
					return Directory.Exists(file);
				}

				if (header != null)
				{
					if ((header.TypeFlag == TarHeader.LF_DIR) || Name.EndsWith("/", StringComparison.Ordinal))
					{
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>
		/// Fill in a TarHeader with information from a File.
		/// </summary>
		/// <param name="header">
		/// The TarHeader to fill in.
		/// </param>
		/// <param name="file">
		/// The file from which to get the header information.
		/// </param>
		public void GetFileTarHeader(TarHeader header, string file)
		{
			if (header == null)
			{
				throw new ArgumentNullException(nameof(header));
			}

			if (file == null)
			{
				throw new ArgumentNullException(nameof(file));
			}

			this.file = file;

			// bugfix from torhovl from #D forum:
			string name = file;

			// 23-Jan-2004 GnuTar allows device names in path where the name is not local to the current directory
			if (name.IndexOf(Directory.GetCurrentDirectory(), StringComparison.Ordinal) == 0)
			{
				name = name.Substring(Directory.GetCurrentDirectory().Length);
			}

			/*
						if (Path.DirectorySeparatorChar == '\\')
						{
							// check if the OS is Windows
							// Strip off drive letters!
							if (name.Length > 2)
							{
								char ch1 = name[0];
								char ch2 = name[1];

								if (ch2 == ':' && Char.IsLetter(ch1))
								{
									name = name.Substring(2);
								}
							}
						}
			*/

			name = name.Replace(Path.DirectorySeparatorChar, '/');

			// No absolute pathnames
			// Windows (and Posix?) paths can start with UNC style "\\NetworkDrive\",
			// so we loop on starting /'s.
			while (name.StartsWith("/", StringComparison.Ordinal))
			{
				name = name.Substring(1);
			}

			header.LinkName = String.Empty;
			header.Name = name;

			if (Directory.Exists(file))
			{
				header.Mode = 1003; // Magic number for security access for a UNIX filesystem
				header.TypeFlag = TarHeader.LF_DIR;
				if ((header.Name.Length == 0) || header.Name[header.Name.Length - 1] != '/')
				{
					header.Name = header.Name + "/";
				}

				header.Size = 0;
			}
			else
			{
				header.Mode = 33216; // Magic number for security access for a UNIX filesystem
				header.TypeFlag = TarHeader.LF_NORMAL;
				header.Size = new FileInfo(file.Replace('/', Path.DirectorySeparatorChar)).Length;
			}

			header.ModTime = System.IO.File.GetLastWriteTime(file.Replace('/', Path.DirectorySeparatorChar)).ToUniversalTime();
			header.DevMajor = 0;
			header.DevMinor = 0;
		}

		/// <summary>
		/// Get entries for all files present in this entries directory.
		/// If this entry doesnt represent a directory zero entries are returned.
		/// </summary>
		/// <returns>
		/// An array of TarEntry's for this entry's children.
		/// </returns>
		public TarEntry[] GetDirectoryEntries()
		{
			if ((file == null) || !Directory.Exists(file))
			{
				return Empty.Array<TarEntry>();
			}

			string[] list = Directory.GetFileSystemEntries(file);
			TarEntry[] result = new TarEntry[list.Length];

			for (int i = 0; i < list.Length; ++i)
			{
				result[i] = TarEntry.CreateEntryFromFile(list[i]);
			}

			return result;
		}

		/// <summary>
		/// Write an entry's header information to a header buffer.
		/// </summary>
		/// <param name = "outBuffer">
		/// The tar entry header buffer to fill in.
		/// </param>
		[Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
		public void WriteEntryHeader(byte[] outBuffer)
		{
			WriteEntryHeader(outBuffer, null);
		}

		/// <summary>
		/// Write an entry's header information to a header buffer.
		/// </summary>
		/// <param name = "outBuffer">
		/// The tar entry header buffer to fill in.
		/// </param>
		/// <param name = "nameEncoding">
		/// The <see cref="Encoding"/> used for the Name fields, or null for ASCII only
		/// </param>
		public void WriteEntryHeader(byte[] outBuffer, Encoding nameEncoding)
		{
			header.WriteHeader(outBuffer, nameEncoding);
		}

		/// <summary>
		/// Convenience method that will modify an entry's name directly
		/// in place in an entry header buffer byte array.
		/// </summary>
		/// <param name="buffer">
		/// The buffer containing the entry header to modify.
		/// </param>
		/// <param name="newName">
		/// The new name to place into the header buffer.
		/// </param>
		[Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
		static public void AdjustEntryName(byte[] buffer, string newName)
		{
			AdjustEntryName(buffer, newName, null);
		}

		/// <summary>
		/// Convenience method that will modify an entry's name directly
		/// in place in an entry header buffer byte array.
		/// </summary>
		/// <param name="buffer">
		/// The buffer containing the entry header to modify.
		/// </param>
		/// <param name="newName">
		/// The new name to place into the header buffer.
		/// </param>
		/// <param name="nameEncoding">
		/// The <see cref="Encoding"/> used for the Name fields, or null for ASCII only
		/// </param>
		static public void AdjustEntryName(byte[] buffer, string newName, Encoding nameEncoding)
		{
			TarHeader.GetNameBytes(newName, buffer, 0, TarHeader.NAMELEN, nameEncoding);
		}

		/// <summary>
		/// Fill in a TarHeader given only the entry's name.
		/// </summary>
		/// <param name="header">
		/// The TarHeader to fill in.
		/// </param>
		/// <param name="name">
		/// The tar entry name.
		/// </param>
		static public void NameTarHeader(TarHeader header, string name)
		{
			if (header == null)
			{
				throw new ArgumentNullException(nameof(header));
			}

			if (name == null)
			{
				throw new ArgumentNullException(nameof(name));
			}

			bool isDir = name.EndsWith("/", StringComparison.Ordinal);

			header.Name = name;
			header.Mode = isDir ? 1003 : 33216;
			header.UserId = 0;
			header.GroupId = 0;
			header.Size = 0;

			header.ModTime = DateTime.UtcNow;

			header.TypeFlag = isDir ? TarHeader.LF_DIR : TarHeader.LF_NORMAL;

			header.LinkName = String.Empty;
			header.UserName = String.Empty;
			header.GroupName = String.Empty;

			header.DevMajor = 0;
			header.DevMinor = 0;
		}

		#region Instance Fields

		/// <summary>
		/// The name of the file this entry represents or null if the entry is not based on a file.
		/// </summary>
		private string file;

		/// <summary>
		/// The entry's header information.
		/// </summary>
		private TarHeader header;

		#endregion Instance Fields
	}
}
