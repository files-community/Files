//====================================================================================================
//The Free Edition of Java to C# Converter limits conversion output to 100 lines per file.

//To purchase the Premium Edition, visit our website:
//https://www.tangiblesoftwaresolutions.com/order/order-java-to-csharp.html
//====================================================================================================

using System.Collections.Generic;

/*
 * Copyright 2012 dorkbox, llc
 */
namespace dorkbox.peParser.headers
{

	using DirEntry = dorkbox.peParser.misc.DirEntry;
	using ByteArray = dorkbox.peParser.ByteArray;
	using MagicNumberType = dorkbox.peParser.misc.MagicNumberType;
	using dorkbox.peParser.types;
	using DWORD = dorkbox.peParser.types.DWORD;
	using HeaderDefinition = dorkbox.peParser.types.HeaderDefinition;
	using ImageBase = dorkbox.peParser.types.ImageBase;
	using ImageBase_Wide = dorkbox.peParser.types.ImageBase_Wide;
	using ImageDataDir = dorkbox.peParser.types.ImageDataDir;
	using ImageDataDirExtra = dorkbox.peParser.types.ImageDataDirExtra;
	using MagicNumber = dorkbox.peParser.types.MagicNumber;
	using DWORD_WIDE = dorkbox.peParser.types.DWORD_WIDE;
	using RVA = dorkbox.peParser.types.RVA;
	using DllCharacteristics = dorkbox.peParser.types.DllCharacteristics;
	using Subsystem = dorkbox.peParser.types.Subsystem;
	using WORD = dorkbox.peParser.types.WORD;
	using UInteger = dorkbox.bytes.UInteger;
	using ULong = dorkbox.bytes.ULong;

	public class OptionalHeader : Header
	{

		// see: http://msdn.microsoft.com/en-us/library/ms809762.aspx

		public IList<ImageDataDir> tables = new List<ImageDataDir>(0);

		//
		// Standard fields.
		//
		public readonly MagicNumber MAGIC_NUMBER;
		public readonly WORD MAJOR_LINKER_VERSION;
		public readonly WORD MINOR_LINKER_VERSION;
		public readonly DWORD SIZE_OF_CODE;
		public readonly DWORD SIZE_OF_INIT_DATA;
		public readonly DWORD SIZE_OF_UNINIT_DATA;
		public readonly DWORD ADDR_OF_ENTRY_POINT;
		public readonly DWORD BASE_OF_CODE;
		public readonly DWORD BASE_OF_DATA;

		private bool IS_32_BIT;

		//
		// NT additional fields.
		//
		public readonly ByteDefinition IMAGE_BASE;
		public readonly DWORD SECTION_ALIGNMENT;
		public readonly DWORD FILE_ALIGNMENT;
		public readonly WORD MAJOR_OS_VERSION;
		public readonly WORD MINOR_OS_VERSION;
		public readonly WORD MAJOR_IMAGE_VERSION;
		public readonly WORD MINOR_IMAGE_VERSION;
		public readonly WORD MAJOR_SUBSYSTEM_VERSION;
		public readonly WORD MINOR_SUBSYSTEM_VERSION;
		public readonly DWORD WIN32_VERSION_VALUE;
		public readonly DWORD SIZE_OF_IMAGE;
		public readonly DWORD SIZE_OF_HEADERS;
		public readonly DWORD CHECKSUM;
		public readonly Subsystem SUBSYSTEM;
		public readonly DllCharacteristics DLL_CHARACTERISTICS;
		public readonly ByteDefinition SIZE_OF_STACK_RESERVE;
		public readonly ByteDefinition SIZE_OF_STACK_COMMIT;
		public readonly ByteDefinition SIZE_OF_HEAP_RESERVE;
		public readonly ByteDefinition SIZE_OF_HEAP_COMMIT;
		public readonly DWORD LOADER_FLAGS;
		public readonly RVA NUMBER_OF_RVA_AND_SIZES;


		public readonly ImageDataDir EXPORT_TABLE;
		public readonly ImageDataDir IMPORT_TABLE;
		public readonly ImageDataDir RESOURCE_TABLE;
		public readonly ImageDataDir EXCEPTION_TABLE;
		public readonly ImageDataDir CERTIFICATE_TABLE;
		public readonly ImageDataDir BASE_RELOCATION_TABLE;

		public readonly ImageDataDir DEBUG;
		public readonly ImageDataDir COPYRIGHT;
		public readonly ImageDataDir GLOBAL_PTR;
		public readonly ImageDataDir TLS_TABLE;
		public readonly ImageDataDir LOAD_CONFIG_TABLE;

		public readonly ImageDataDirExtra BOUND_IMPORT;
		public readonly ImageDataDirExtra IAT;
		public readonly ImageDataDirExtra DELAY_IMPORT_DESCRIPTOR;
		public readonly ImageDataDirExtra CLR_RUNTIME_HEADER;

		public OptionalHeader(ByteArray bytes)
		{
			// the header length is variable.

			//
			// Standard fields.
			//
			h(new HeaderDefinition("Standard fields"));
			bytes.mark();

			this.MAGIC_NUMBER = h(new MagicNumber(bytes.readUShort(2), "magic number"));
			this.MAJOR_LINKER_VERSION = h(new WORD(bytes.readUShort(1), "major linker version"));
			this.MINOR_LINKER_VERSION = h(new WORD(bytes.readUShort(1), "minor linker version"));
			this.SIZE_OF_CODE = h(new DWORD(bytes.readUInt(4), "size of code"));
			this.SIZE_OF_INIT_DATA = h(new DWORD(bytes.readUInt(4), "size of initialized data"));
			this.SIZE_OF_UNINIT_DATA = h(new DWORD(bytes.readUInt(4), "size of unitialized data"));
			this.ADDR_OF_ENTRY_POINT = h(new DWORD(bytes.readUInt(4), "address of entry point"));
			this.BASE_OF_CODE = h(new DWORD(bytes.readUInt(4), "address of base of code"));
			this.BASE_OF_DATA = h(new DWORD(bytes.readUInt(4), "address of base of data"));

			this.IS_32_BIT = this.MAGIC_NUMBER.get() == MagicNumberType.PE32;

			bytes.reset();
			if (this.IS_32_BIT)
			{
				bytes.skip(28);
			}
			else
			{
				bytes.skip(24);
			}


			//
			// Standard fields.
			//
			h(new HeaderDefinition("Windows specific fields"));

			if (this.IS_32_BIT)
			{
				this.IMAGE_BASE = h(new ImageBase(bytes.readUInt(4), "image base"));
			}
			else
			{
				this.IMAGE_BASE = h(new ImageBase_Wide(bytes.readULong(8), "image base"));
			}

			this.SECTION_ALIGNMENT = h(new DWORD(bytes.readUInt(4), "section alignment in bytes"));
			this.FILE_ALIGNMENT = h(new DWORD(bytes.readUInt(4), "file alignment in bytes"));

			this.MAJOR_OS_VERSION = h(new WORD(bytes.readUShort(2), "major operating system version"));
			this.MINOR_OS_VERSION = h(new WORD(bytes.readUShort(2), "minor operating system version"));
			this.MAJOR_IMAGE_VERSION = h(new WORD(bytes.readUShort(2), "major image version"));
			this.MINOR_IMAGE_VERSION = h(new WORD(bytes.readUShort(2), "minor image version"));
			this.MAJOR_SUBSYSTEM_VERSION = h(new WORD(bytes.readUShort(2), "major subsystem version"));
			this.MINOR_SUBSYSTEM_VERSION = h(new WORD(bytes.readUShort(2), "minor subsystem version"));

			this.WIN32_VERSION_VALUE = h(new DWORD(bytes.readUInt(4), "win32 version value (reserved, must be zero)"));
			this.SIZE_OF_IMAGE = h(new DWORD(bytes.readUInt(4), "size of image in bytes"));
			this.SIZE_OF_HEADERS = h(new DWORD(bytes.readUInt(4), "size of headers (MS DOS stub, PE header, and section headers)"));
			this.CHECKSUM = h(new DWORD(bytes.readUInt(4), "checksum"));
			this.SUBSYSTEM = h(new Subsystem(bytes.readUShort(2), "subsystem"));
			this.DLL_CHARACTERISTICS = h(new DllCharacteristics(bytes.readUShort(2), "dll characteristics"));

			if (this.IS_32_BIT)
			{
				this.SIZE_OF_STACK_RESERVE = h(new DWORD(bytes.readUInt(4), "size of stack reserve"));
				this.SIZE_OF_STACK_COMMIT = h(new DWORD(bytes.readUInt(4), "size of stack commit"));
				this.SIZE_OF_HEAP_RESERVE = h(new DWORD(bytes.readUInt(4), "size of heap reserve"));
				this.SIZE_OF_HEAP_COMMIT = h(new DWORD(bytes.readUInt(4), "size of heap commit"));
			}
			else
			{
				this.SIZE_OF_STACK_RESERVE = h(new DWORD_WIDE(bytes.readULong(8), "size of stack reserve"));
				this.SIZE_OF_STACK_COMMIT = h(new DWORD_WIDE(bytes.readULong(8), "size of stack commit"));
				this.SIZE_OF_HEAP_RESERVE = h(new DWORD_WIDE(bytes.readULong(8), "size of heap reserve"));
				this.SIZE_OF_HEAP_COMMIT = h(new DWORD_WIDE(bytes.readULong(8), "size of heap commit"));
			}

			this.LOADER_FLAGS = h(new DWORD(bytes.readUInt(4), "loader flags (reserved, must be zero)"));
			this.NUMBER_OF_RVA_AND_SIZES = h(new RVA(bytes.readUInt(4), "number of rva and sizes"));


			bytes.reset();
			if (this.IS_32_BIT)
			{
				bytes.skip(96);
			}
			else
			{
				bytes.skip(112);
			}

			//
			// Data directories
			//
			h(new HeaderDefinition("Data Directories"));
			this.EXPORT_TABLE = table(h(new ImageDataDir(bytes, DirEntry.EXPORT)));
			this.IMPORT_TABLE = table(h(new ImageDataDir(bytes, DirEntry.IMPORT)));
			this.RESOURCE_TABLE = table(h(new ImageDataDir(bytes, DirEntry.RESOURCE)));
			this.EXCEPTION_TABLE = table(h(new ImageDataDir(bytes, DirEntry.EXCEPTION)));
			this.CERTIFICATE_TABLE = table(h(new ImageDataDir(bytes, DirEntry.SECURITY)));
			this.BASE_RELOCATION_TABLE = table(h(new ImageDataDir(bytes, DirEntry.BASERELOC)));

			this.DEBUG = table(h(new ImageDataDir(bytes, DirEntry.DEBUG)));
			this.COPYRIGHT = table(h(new ImageDataDir(bytes, DirEntry.COPYRIGHT)));
			this.GLOBAL_PTR = table(h(new ImageDataDir(bytes, DirEntry.GLOBALPTR)));
			this.TLS_TABLE = table(h(new ImageDataDir(bytes, DirEntry.TLS)));
			this.LOAD_CONFIG_TABLE = table(h(new ImageDataDir(bytes, DirEntry.LOAD_CONFIG)));

			this.BOUND_IMPORT = h(new ImageDataDirExtra(bytes, "bound import"));
			this.IAT = h(new ImageDataDirExtra(bytes, "IAT"));
			this.DELAY_IMPORT_DESCRIPTOR = h(new ImageDataDirExtra(bytes, "delay import descriptor"));
			this.CLR_RUNTIME_HEADER = h(new ImageDataDirExtra(bytes, "COM+ runtime header"));

			// reserved 8 bytes!!
			bytes.skip(8);
		}

		private T table<T>(T obj) where T : ImageDataDir
		{
			this.tables.Add(obj);
			return obj;
		}
	}
}