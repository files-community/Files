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

	public sealed class DllCharacteristicsType
	{
		public static readonly DllCharacteristicsType IMAGE_DLL_CHARACTERISTICS_DYNAMIC_BASE = new DllCharacteristicsType("IMAGE_DLL_CHARACTERISTICS_DYNAMIC_BASE", InnerEnum.IMAGE_DLL_CHARACTERISTICS_DYNAMIC_BASE, "40", "DLL can be relocated at load time.");
		public static readonly DllCharacteristicsType IMAGE_DLL_CHARACTERISTICS_FORCE_INTEGRITY = new DllCharacteristicsType("IMAGE_DLL_CHARACTERISTICS_FORCE_INTEGRITY", InnerEnum.IMAGE_DLL_CHARACTERISTICS_FORCE_INTEGRITY, "80", "Code Integrity checks are enforced.");
		public static readonly DllCharacteristicsType IMAGE_DLL_CHARACTERISTICS_NX_COMPAT = new DllCharacteristicsType("IMAGE_DLL_CHARACTERISTICS_NX_COMPAT", InnerEnum.IMAGE_DLL_CHARACTERISTICS_NX_COMPAT, "100", "Image is NX compatible.");
		public static readonly DllCharacteristicsType IMAGE_DLL_CHARACTERISTICS_ISOLATION = new DllCharacteristicsType("IMAGE_DLL_CHARACTERISTICS_ISOLATION", InnerEnum.IMAGE_DLL_CHARACTERISTICS_ISOLATION, "200", "Isolation aware, but do not isolate the image.");
		public static readonly DllCharacteristicsType IMAGE_DLLCHARACTERISTICS_NO_SEH = new DllCharacteristicsType("IMAGE_DLLCHARACTERISTICS_NO_SEH", InnerEnum.IMAGE_DLLCHARACTERISTICS_NO_SEH, "400", "Does not use structured exception (SE) handling. No SE handler may be called in this image.");
		public static readonly DllCharacteristicsType IMAGE_DLLCHARACTERISTICS_NO_BIND = new DllCharacteristicsType("IMAGE_DLLCHARACTERISTICS_NO_BIND", InnerEnum.IMAGE_DLLCHARACTERISTICS_NO_BIND, "800", "Do not bind the image.");
		public static readonly DllCharacteristicsType IMAGE_DLLCHARACTERISTICS_WDM_DRIVER = new DllCharacteristicsType("IMAGE_DLLCHARACTERISTICS_WDM_DRIVER", InnerEnum.IMAGE_DLLCHARACTERISTICS_WDM_DRIVER, "2000", "A WDM driver.");
		public static readonly DllCharacteristicsType IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE = new DllCharacteristicsType("IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE", InnerEnum.IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE, "8000", "Terminal Server aware.");

		private static readonly List<DllCharacteristicsType> valueList = new List<DllCharacteristicsType>();

		static DllCharacteristicsType()
		{
			valueList.Add(IMAGE_DLL_CHARACTERISTICS_DYNAMIC_BASE);
			valueList.Add(IMAGE_DLL_CHARACTERISTICS_FORCE_INTEGRITY);
			valueList.Add(IMAGE_DLL_CHARACTERISTICS_NX_COMPAT);
			valueList.Add(IMAGE_DLL_CHARACTERISTICS_ISOLATION);
			valueList.Add(IMAGE_DLLCHARACTERISTICS_NO_SEH);
			valueList.Add(IMAGE_DLLCHARACTERISTICS_NO_BIND);
			valueList.Add(IMAGE_DLLCHARACTERISTICS_WDM_DRIVER);
			valueList.Add(IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE);
		}

		public enum InnerEnum
		{
			IMAGE_DLL_CHARACTERISTICS_DYNAMIC_BASE,
			IMAGE_DLL_CHARACTERISTICS_FORCE_INTEGRITY,
			IMAGE_DLL_CHARACTERISTICS_NX_COMPAT,
			IMAGE_DLL_CHARACTERISTICS_ISOLATION,
			IMAGE_DLLCHARACTERISTICS_NO_SEH,
			IMAGE_DLLCHARACTERISTICS_NO_BIND,
			IMAGE_DLLCHARACTERISTICS_WDM_DRIVER,
			IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE
		}

		public readonly InnerEnum innerEnumValue;
		private readonly string nameValue;
		private readonly int ordinalValue;
		private static int nextOrdinal = 0;

		private readonly string hexValue;
		private readonly string description;

		internal DllCharacteristicsType(string name, InnerEnum innerEnum, string hexValue, string description)
		{
			this.hexValue = hexValue;
			this.description = description;

			nameValue = name;
			ordinalValue = nextOrdinal++;
			innerEnumValue = innerEnum;
		}

		public static DllCharacteristicsType[] get(dorkbox.bytes.UShort key)
		{
			IList<DllCharacteristicsType> chars = new List<DllCharacteristicsType>(0);
			int keyAsInt = key.intValue();

			foreach (DllCharacteristicsType c in values())
			{
				long mask = Convert.ToInt64(c.hexValue, 16);
				if ((keyAsInt & mask) != 0)
				{
					chars.Add(c);
				}
			}

			return ((List<DllCharacteristicsType>)chars).ToArray();
		}

		public string Description
		{
			get
			{
				return this.description;
			}
		}

		public static DllCharacteristicsType[] values()
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

		public static DllCharacteristicsType valueOf(string name)
		{
			foreach (DllCharacteristicsType enumInstance in DllCharacteristicsType.valueList)
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