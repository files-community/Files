using System;
using System.Collections.Generic;

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
namespace dorkbox.peParser.headers.flags
{

	using UShort = dorkbox.bytes.UShort;

	public sealed class Characteristics
	{

		public static readonly Characteristics IMAGE_FILE_RELOCS_STRIPPED = new Characteristics("IMAGE_FILE_RELOCS_STRIPPED", InnerEnum.IMAGE_FILE_RELOCS_STRIPPED, "1", "Resource information is stripped from the file");
		public static readonly Characteristics IMAGE_FILE_EXECUTABLE_IMAGE = new Characteristics("IMAGE_FILE_EXECUTABLE_IMAGE", InnerEnum.IMAGE_FILE_EXECUTABLE_IMAGE, "2", "The file is executable (no unresoled external references");
		public static readonly Characteristics IMAGE_FILE_LINE_NUMS_STRIPPED = new Characteristics("IMAGE_FILE_LINE_NUMS_STRIPPED", InnerEnum.IMAGE_FILE_LINE_NUMS_STRIPPED, "4", "COFF line numbers are stripped from the file (DEPRECATED)");
		public static readonly Characteristics IMAGE_FILE_LOCAL_SYMS_STRIPPED = new Characteristics("IMAGE_FILE_LOCAL_SYMS_STRIPPED", InnerEnum.IMAGE_FILE_LOCAL_SYMS_STRIPPED, "8", "COFF local symbols are stripped form the file (DEPRECATED)");
		public static readonly Characteristics IMAGE_FILE_AGGRESSIVE_WS_TRIM = new Characteristics("IMAGE_FILE_AGGRESSIVE_WS_TRIM", InnerEnum.IMAGE_FILE_AGGRESSIVE_WS_TRIM, "10", "Aggressively trim working set (DEPRECATED for Windows 2000 and later)");
		public static readonly Characteristics IMAGE_FILE_LARGE_ADDRESS_AWARE = new Characteristics("IMAGE_FILE_LARGE_ADDRESS_AWARE", InnerEnum.IMAGE_FILE_LARGE_ADDRESS_AWARE, "20", "Application can handle larger than 2 GB addresses.");
		public static readonly Characteristics IMAGE_FILE_RESERVED = new Characteristics("IMAGE_FILE_RESERVED", InnerEnum.IMAGE_FILE_RESERVED, "40", "Use of this flag is reserved.");
		public static readonly Characteristics IMAGE_FILE_BYTES_REVERSED_LO = new Characteristics("IMAGE_FILE_BYTES_REVERSED_LO", InnerEnum.IMAGE_FILE_BYTES_REVERSED_LO, "80", "Bytes of the word are reversed (REVERSED LO)");
		public static readonly Characteristics IMAGE_FILE_32BIT_MACHINE = new Characteristics("IMAGE_FILE_32BIT_MACHINE", InnerEnum.IMAGE_FILE_32BIT_MACHINE, "100", "Machine is based on a 32-bit-word architecture.");
		public static readonly Characteristics IMAGE_FILE_DEBUG_STRIPPED = new Characteristics("IMAGE_FILE_DEBUG_STRIPPED", InnerEnum.IMAGE_FILE_DEBUG_STRIPPED, "200", "Debugging is removed from the file.");
		public static readonly Characteristics IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP = new Characteristics("IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP", InnerEnum.IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP, "400", "If the image is on removable media, fully load it and copy it to the swap file.");
		public static readonly Characteristics IMAGE_FILE_NET_RUN_FROM_SWAP = new Characteristics("IMAGE_FILE_NET_RUN_FROM_SWAP", InnerEnum.IMAGE_FILE_NET_RUN_FROM_SWAP, "800", "If the image is on network media, fully load it and copy it to the swap file.");
		public static readonly Characteristics IMAGE_FILE_SYSTEM = new Characteristics("IMAGE_FILE_SYSTEM", InnerEnum.IMAGE_FILE_SYSTEM, "1000", "The image file is a system file, (such as a driver) and not a user program.");
		public static readonly Characteristics IMAGE_FILE_DLL = new Characteristics("IMAGE_FILE_DLL", InnerEnum.IMAGE_FILE_DLL, "2000", "The image file is a dynamic-link library (DLL). Such files are considered executable files for almost all purposes, although they cannot be directly run.");
		public static readonly Characteristics IMAGE_FILE_UP_SYSTEM_ONLY = new Characteristics("IMAGE_FILE_UP_SYSTEM_ONLY", InnerEnum.IMAGE_FILE_UP_SYSTEM_ONLY, "4000", "The file should be run only on a uniprocessor machine.");
		public static readonly Characteristics IMAGE_FILE_BYTES_REVERSED_HI = new Characteristics("IMAGE_FILE_BYTES_REVERSED_HI", InnerEnum.IMAGE_FILE_BYTES_REVERSED_HI, "8000", "Bytes of the word are reversed (REVERSED HI)");

		private static readonly List<Characteristics> valueList = new List<Characteristics>();

		static Characteristics()
		{
			valueList.Add(IMAGE_FILE_RELOCS_STRIPPED);
			valueList.Add(IMAGE_FILE_EXECUTABLE_IMAGE);
			valueList.Add(IMAGE_FILE_LINE_NUMS_STRIPPED);
			valueList.Add(IMAGE_FILE_LOCAL_SYMS_STRIPPED);
			valueList.Add(IMAGE_FILE_AGGRESSIVE_WS_TRIM);
			valueList.Add(IMAGE_FILE_LARGE_ADDRESS_AWARE);
			valueList.Add(IMAGE_FILE_RESERVED);
			valueList.Add(IMAGE_FILE_BYTES_REVERSED_LO);
			valueList.Add(IMAGE_FILE_32BIT_MACHINE);
			valueList.Add(IMAGE_FILE_DEBUG_STRIPPED);
			valueList.Add(IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP);
			valueList.Add(IMAGE_FILE_NET_RUN_FROM_SWAP);
			valueList.Add(IMAGE_FILE_SYSTEM);
			valueList.Add(IMAGE_FILE_DLL);
			valueList.Add(IMAGE_FILE_UP_SYSTEM_ONLY);
			valueList.Add(IMAGE_FILE_BYTES_REVERSED_HI);
		}

		public enum InnerEnum
		{
			IMAGE_FILE_RELOCS_STRIPPED,
			IMAGE_FILE_EXECUTABLE_IMAGE,
			IMAGE_FILE_LINE_NUMS_STRIPPED,
			IMAGE_FILE_LOCAL_SYMS_STRIPPED,
			IMAGE_FILE_AGGRESSIVE_WS_TRIM,
			IMAGE_FILE_LARGE_ADDRESS_AWARE,
			IMAGE_FILE_RESERVED,
			IMAGE_FILE_BYTES_REVERSED_LO,
			IMAGE_FILE_32BIT_MACHINE,
			IMAGE_FILE_DEBUG_STRIPPED,
			IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP,
			IMAGE_FILE_NET_RUN_FROM_SWAP,
			IMAGE_FILE_SYSTEM,
			IMAGE_FILE_DLL,
			IMAGE_FILE_UP_SYSTEM_ONLY,
			IMAGE_FILE_BYTES_REVERSED_HI
		}

		public readonly InnerEnum innerEnumValue;
		private readonly string nameValue;
		private readonly int ordinalValue;
		private static int nextOrdinal = 0;

		private readonly string hexValue;
		private readonly string description;

		internal Characteristics(string name, InnerEnum innerEnum, string hexValue, string description)
		{
			this.hexValue = hexValue;
			this.description = description;

			nameValue = name;
			ordinalValue = nextOrdinal++;
			innerEnumValue = innerEnum;
		}

		public static Characteristics[] get(dorkbox.bytes.UShort key)
		{
	//        byte[] value = Arrays.copyOfRange(headerbytes, byteDefinition.getByteStart(), byteDefinition.getByteStart() + byteDefinition.getLength());
	//        int key = LittleEndian.Int_.fromBytes(value[0], value[1], (byte)0, (byte)0);

			IList<Characteristics> chars = new List<Characteristics>(0);
			int keyAsInt = key.intValue();

			foreach (Characteristics c in values())
			{
				long mask = Convert.ToInt64(c.hexValue, 16);
				if ((keyAsInt & mask) != 0)
				{
					chars.Add(c);
				}
			}

			return ((List<Characteristics>)chars).ToArray();
		}

		public string Description
		{
			get
			{
				return this.description;
			}
		}

		public static Characteristics[] values()
		{
			return valueList.ToArray();
		}

		public int ordinal()
		{
			return ordinalValue;
		}

		public override string ToString()
		{
			return nameValue;
		}

		public static Characteristics valueOf(string name)
		{
			foreach (Characteristics enumInstance in Characteristics.valueList)
			{
				if (enumInstance.nameValue == name)
				{
					return enumInstance;
				}
			}
			throw new System.ArgumentException(name);
		}
	}

}