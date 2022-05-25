//====================================================================================================
//The Free Edition of Java to C# Converter limits conversion output to 100 lines per file.

//To purchase the Premium Edition, visit our website:
//https://www.tangiblesoftwaresolutions.com/order/order-java-to-csharp.html
//====================================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/*
 * Copyright 2012 dorkbox, llc
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
namespace dorkbox.peParser
{


	using COFFFileHeader = dorkbox.peParser.headers.COFFFileHeader;
	using Header = dorkbox.peParser.headers.Header;
	using OptionalHeader = dorkbox.peParser.headers.OptionalHeader;
	using SectionTable = dorkbox.peParser.headers.SectionTable;
	using SectionTableEntry = dorkbox.peParser.headers.SectionTableEntry;
	using ResourceDataEntry = dorkbox.peParser.headers.resources.ResourceDataEntry;
	using ResourceDirectoryEntry = dorkbox.peParser.headers.resources.ResourceDirectoryEntry;
	using ResourceDirectoryHeader = dorkbox.peParser.headers.resources.ResourceDirectoryHeader;
	using DirEntry = dorkbox.peParser.misc.DirEntry;
	using dorkbox.peParser.types;
	using ImageDataDir = dorkbox.peParser.types.ImageDataDir;

	public class PeFile
	{
		// info from:
		// http://evilzone.org/tutorials/(paper)-portable-executable-format-and-its-rsrc-section/
		// http://www.skynet.ie/~caolan/pub/winresdump/winresdump/doc/pefile.html  (older version of the doc...)
		// http://www.csn.ul.ie/~caolan/pub/winresdump/winresdump/doc/pefile2.html
		// http://msdn.microsoft.com/en-us/library/ms809762.aspx

		/// <summary>
		/// Gets the version number.
		/// </summary>
		public static string Version
		{
			get
			{
				return "3.1";
			}
		}

		private const int PE_OFFSET_LOCATION = 0x3c;
		private static readonly sbyte[] PE_SIG = "PE\0\0".GetBytes();

		static PeFile()
		{
		}

		// TODO: should use an input stream to load header info, instead of the entire thing!
		public ByteArray fileBytes = null;

		private COFFFileHeader coffHeader;
		public OptionalHeader optionalHeader;
		private SectionTable sectionTable;
		private bool invalidFile;


		public PeFile(string fileName)
		{
			throw new NotSupportedException("Use Stream constructor.");
		}

		public PeFile(Stream inputStream)
		{
			try
			{
				fromInputStream(inputStream);
			}
			catch (FileNotFoundException e)
			{
				Console.WriteLine(e.ToString());
				Console.Write(e.StackTrace);
			}
			catch (IOException e)
			{
				Console.WriteLine(e.ToString());
				Console.Write(e.StackTrace);
			}
		}

		//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
		//ORIGINAL LINE: private void fromInputStream(java.io.InputStream inputStream) throws FileNotFoundException, java.io.IOException
		private void fromInputStream(Stream inputStream)
		{
			MemoryStream baos = new MemoryStream(8192);

			byte[] buffer = new byte[4096];
			int read = 0;
			while ((read = inputStream.Read(buffer, 0, buffer.Length)) > 0)
			{
				baos.Write(buffer, 0, read);
			}
			baos.Flush();
			inputStream.Close();

			byte[] bytes = baos.ToArray();
			invalidFile = bytes.Length == 0;

			this.fileBytes = new ByteArray(Array.ConvertAll(bytes, x => unchecked((sbyte)(x))));

			// initialize header info
			if (PE)
			{
				int offset = PEOffset + PE_SIG.Length;
				this.fileBytes.seek(offset);

				this.coffHeader = new COFFFileHeader(this.fileBytes);
				this.optionalHeader = new OptionalHeader(this.fileBytes);

				int numberOfEntries = this.coffHeader.NumberOfSections.get().intValue();
				this.sectionTable = new SectionTable(this.fileBytes, numberOfEntries);

				// now the bytes are positioned at the start of the section table. ALl other info MUST be done relative to byte offsets/locations!

				// fixup directory names -> table names (from spec!)
				foreach (SectionTableEntry section in this.sectionTable.sections)
				{
					long sectionAddress = section.VIRTUAL_ADDRESS.get().longValue();
					long sectionSize = section.SIZE_OF_RAW_DATA.get().longValue();

					foreach (ImageDataDir entry in this.optionalHeader.tables)
					{
						long optionAddress = entry.get().longValue();

						if (sectionAddress <= optionAddress && sectionAddress + sectionSize > optionAddress)
						{

							entry.Section = section;
							break;
						}
					}
				}

				// fixup directories
				foreach (ImageDataDir entry in this.optionalHeader.tables)
				{
					if (entry.Type == DirEntry.RESOURCE)
					{
						// fixup resources
						SectionTableEntry section = entry.Section;
						if (section != null)
						{
							long delta = section.VIRTUAL_ADDRESS.get().longValue() - section.POINTER_TO_RAW_DATA.get().longValue();
							long offsetInFile = entry.get().longValue() - delta;

							if (offsetInFile > int.MaxValue)
							{
								throw new Exception("Unable to set offset to more than 2gb!");
							}

							this.fileBytes.seek((int)offsetInFile);
							this.fileBytes.mark(); // resource data is offset from the beginning of the header!

							Header root = new ResourceDirectoryHeader(this.fileBytes, section, 0);
							entry.data = root;
						}
					}
				}
			}
		}

		public virtual string Info
		{
			get
			{
				if (PE)
				{
					StringBuilder b = new StringBuilder();

					b.Append("PE signature offset: ").Append(PEOffset).Append(System.Environment.NewLine).Append("PE signature correct: ").Append("yes").Append(System.Environment.NewLine).Append(System.Environment.NewLine).Append("----------------").Append(System.Environment.NewLine).Append("COFF header info").Append(System.Environment.NewLine).Append("----------------").Append(System.Environment.NewLine);

					//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in C#:
					//ORIGINAL LINE: for (dorkbox.peParser.types.ByteDefinition<?> bd : this.coffHeader.headers)
					foreach (ByteDefinition<object> bd in this.coffHeader.headers)
					{
						bd.format(b);
					}
					b.Append(System.Environment.NewLine);

					b.Append("--------------------").Append(System.Environment.NewLine).Append("Optional header info").Append(System.Environment.NewLine).Append("--------------------").Append(System.Environment.NewLine);

					//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in C#:
					//ORIGINAL LINE: for (dorkbox.peParser.types.ByteDefinition<?> bd : this.optionalHeader.headers)
					foreach (ByteDefinition<object> bd in this.optionalHeader.headers)
					{
						bd.format(b);
					}
					b.Append(System.Environment.NewLine);


					b.Append(System.Environment.NewLine).Append("-------------").Append(System.Environment.NewLine).Append("Section Table").Append(System.Environment.NewLine).Append("-------------").Append(System.Environment.NewLine).Append(System.Environment.NewLine);

					foreach (SectionTableEntry section in this.sectionTable.sections)
					{
						//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in C#:
						//ORIGINAL LINE: for (dorkbox.peParser.types.ByteDefinition<?> bd : section.headers)
						foreach (ByteDefinition<object> bd in section.headers)
						{
							bd.format(b);
						}
					}

					b.Append(System.Environment.NewLine);
					return b.ToString();
				}
				else
				{
					return "PE signature not found. The given file is not a PE file." + System.Environment.NewLine;
				}
			}
		}

		private int PEOffset
		{
			get
			{
				this.fileBytes.mark();
				this.fileBytes.seek(PE_OFFSET_LOCATION);
				int read = this.fileBytes.readUShort(2).intValue();
				this.fileBytes.reset();
				return read;
			}
		}

		public virtual bool PE
		{
			get
			{
				if (invalidFile)
				{
					return false;
				}

				int saved = -1;
				try
				{
					// this can screw up if the length of the file is invalid...
					int offset = PEOffset;
					saved = this.fileBytes.position();

					// always have to start from zero if we ask this.
					this.fileBytes.seek(0);

					for (int i = 0; i < PE_SIG.Length; i++)
					{
						if (this.fileBytes.readRaw(offset + i) != PE_SIG[i])
						{
							return false;
						}
					}

					return true;
				}
				catch (Exception)
				{
					return false;
				}
				finally
				{
					if (saved != -1)
					{
						this.fileBytes.seek(saved);
					}
				}
			}
		}

		public virtual MemoryStream LargestResourceAsStream
		{
			get
			{
				foreach (ImageDataDir mainEntry in this.optionalHeader.tables)
				{
					if (mainEntry.Type == DirEntry.RESOURCE)
					{


						LinkedList<ResourceDirectoryEntry> directoryEntries = new LinkedList<ResourceDirectoryEntry>();
						LinkedList<ResourceDirectoryEntry> resourceEntries = new LinkedList<ResourceDirectoryEntry>();

						ResourceDirectoryEntry entry = null;
						ResourceDirectoryHeader root = (ResourceDirectoryHeader)mainEntry.data;

						foreach (ResourceDirectoryEntry rootEntry in root.entries)
						{
							collect(directoryEntries, resourceEntries, rootEntry);
							directoryEntries.AddLast(rootEntry);
						}

						var node = directoryEntries.First;
						while (node != null)
						{
							collect(directoryEntries, resourceEntries, node.Value);
							var next = node.Next;
							directoryEntries.Remove(node);
							node = next;
						}

						ResourceDataEntry largest = null;
						foreach (ResourceDirectoryEntry resourceEntry in resourceEntries)
						{
							ResourceDataEntry dataEntry = resourceEntry.resourceDataEntry;

							if (largest == null || largest.SIZE.get().longValue() < dataEntry.SIZE.get().longValue())
							{
								largest = dataEntry;
							}
						}

						// now return our resource, but it has to be wrapped in a new stream!
						return new MemoryStream(Array.ConvertAll(largest.getData(this.fileBytes), x => unchecked((byte)(x))));
					}
				}
				return null;
			}
		}

		//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
		//ORIGINAL LINE: public static String getVersion(String executablePath) throws Exception
		public static string getVersion(string executablePath)
		{
			PeFile pe = new PeFile(executablePath);

			if (pe.invalidFile)
			{
				throw new Exception("No version found:" + executablePath);
			}

			foreach (ImageDataDir mainEntry in pe.optionalHeader.tables)
			{
				if (mainEntry.Type == DirEntry.RESOURCE)
				{
					ResourceDirectoryHeader root = (ResourceDirectoryHeader)mainEntry.data;
					foreach (ResourceDirectoryEntry rootEntry in root.entries)
					{
						if ("Version".Equals(rootEntry.NAME.get()))
						{
							sbyte[] versionInfoData = rootEntry.directory.entries[0].directory.entries[0].resourceDataEntry.getData(pe.fileBytes);
							int fileVersionIndex = indexOf(versionInfoData, includeNulls("FileVersion")) + 26;
							int fileVersionEndIndex = indexOf(versionInfoData, new sbyte[] { 0x00, 0x00 }, fileVersionIndex);
							return removeNulls(StringHelper.NewString(versionInfoData, fileVersionIndex, fileVersionEndIndex - fileVersionIndex));
						}
					}
				}
			}

			throw new Exception("No version found:" + executablePath);
		}

		private static sbyte[] includeNulls(string str)
		{
			char[] chars = str.ToCharArray();
			sbyte[] result = new sbyte[chars.Length * 2];

			for (int i = 0, j = 0; i < result.Length; i += 2, j++)
			{
				result[i] = (sbyte)chars[j];
			}

			return result;
		}

		private static string removeNulls(string str)
		{
			return string.ReferenceEquals(str, null) ? null : str.Replace("\\x00", "");
		}

		public static int indexOf(sbyte[] outerArray, sbyte[] smallerArray)
		{
			return indexOf(outerArray, smallerArray, 0);
		}

		public static int indexOf(sbyte[] outerArray, sbyte[] smallerArray, int begin)
		{
			for (int i = begin; i < outerArray.Length - smallerArray.Length + 1; ++i)
			{
				bool found = true;
				for (int j = 0; j < smallerArray.Length; ++j)
				{
					if (outerArray[i + j] != smallerArray[j])
					{
						found = false;
						break;
					}
				}
				if (found)
				{
					return i;
				}
			}
			return -1;
		}

		private void collect(in LinkedList<ResourceDirectoryEntry> directoryEntries, in LinkedList<ResourceDirectoryEntry> resourceEntries, in ResourceDirectoryEntry entry)
		{
			if (entry.isDirectory)
			{
				foreach (ResourceDirectoryEntry dirEntry in entry.directory.entries)
				{
					directoryEntries.AddLast(dirEntry);
				}
			}
			else
			{
				resourceEntries.AddLast(entry);
			}
		}
	}
}