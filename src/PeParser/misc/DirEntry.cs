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
namespace dorkbox.peParser.misc
{
	public sealed class DirEntry
	{

		public static readonly DirEntry EXPORT = new DirEntry("EXPORT", InnerEnum.EXPORT, "Export Directory");
		public static readonly DirEntry IMPORT = new DirEntry("IMPORT", InnerEnum.IMPORT, "Import Directory");
		public static readonly DirEntry RESOURCE = new DirEntry("RESOURCE", InnerEnum.RESOURCE, "Resource Directory");
		public static readonly DirEntry EXCEPTION = new DirEntry("EXCEPTION", InnerEnum.EXCEPTION, "Exception Directory");
		public static readonly DirEntry SECURITY = new DirEntry("SECURITY", InnerEnum.SECURITY, "Security Directory");
		public static readonly DirEntry BASERELOC = new DirEntry("BASERELOC", InnerEnum.BASERELOC, "Base Relocation Table");
		public static readonly DirEntry DEBUG = new DirEntry("DEBUG", InnerEnum.DEBUG, "Debug Directory");
		public static readonly DirEntry COPYRIGHT = new DirEntry("COPYRIGHT", InnerEnum.COPYRIGHT, "Description String");
		public static readonly DirEntry GLOBALPTR = new DirEntry("GLOBALPTR", InnerEnum.GLOBALPTR, "Machine Value (MIPS GP)");
		public static readonly DirEntry TLS = new DirEntry("TLS", InnerEnum.TLS, "TLS Directory");
		public static readonly DirEntry LOAD_CONFIG = new DirEntry("LOAD_CONFIG", InnerEnum.LOAD_CONFIG, "Load Configuration Directory");

		private static readonly List<DirEntry> valueList = new List<DirEntry>();

		static DirEntry()
		{
			valueList.Add(EXPORT);
			valueList.Add(IMPORT);
			valueList.Add(RESOURCE);
			valueList.Add(EXCEPTION);
			valueList.Add(SECURITY);
			valueList.Add(BASERELOC);
			valueList.Add(DEBUG);
			valueList.Add(COPYRIGHT);
			valueList.Add(GLOBALPTR);
			valueList.Add(TLS);
			valueList.Add(LOAD_CONFIG);
		}

		public enum InnerEnum
		{
			EXPORT,
			IMPORT,
			RESOURCE,
			EXCEPTION,
			SECURITY,
			BASERELOC,
			DEBUG,
			COPYRIGHT,
			GLOBALPTR,
			TLS,
			LOAD_CONFIG
		}

		public readonly InnerEnum innerEnumValue;
		private readonly string nameValue;
		private readonly int ordinalValue;
		private static int nextOrdinal = 0;

		private readonly string description;

		internal DirEntry(string name, InnerEnum innerEnum, string description)
		{
			this.description = description;

			nameValue = name;
			ordinalValue = nextOrdinal++;
			innerEnumValue = innerEnum;
		}

		public string Description
		{
			get
			{
				return this.description;
			}
		}

		public static DirEntry[] values()
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

		public static DirEntry valueOf(string name)
		{
			foreach (DirEntry enumInstance in DirEntry.valueList)
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