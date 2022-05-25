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
	using UInteger = dorkbox.bytes.UInteger;

	public sealed class ImageBaseType
	{
		public static readonly ImageBaseType IMAGE_BASE_DEFAULT = new ImageBaseType("IMAGE_BASE_DEFAULT", InnerEnum.IMAGE_BASE_DEFAULT, 0x10000000L, "DLL default");
		public static readonly ImageBaseType IMAGE_BASE_WIN_CE = new ImageBaseType("IMAGE_BASE_WIN_CE", InnerEnum.IMAGE_BASE_WIN_CE, 0x00010000L, "default for Windows CE EXEs");
		public static readonly ImageBaseType IMAGE_BASE_WIN = new ImageBaseType("IMAGE_BASE_WIN", InnerEnum.IMAGE_BASE_WIN, 0x00400000L, "default for Windows NT, 2000, XP, 95, 98 and Me");

		private static readonly List<ImageBaseType> valueList = new List<ImageBaseType>();

		static ImageBaseType()
		{
			valueList.Add(IMAGE_BASE_DEFAULT);
			valueList.Add(IMAGE_BASE_WIN_CE);
			valueList.Add(IMAGE_BASE_WIN);
		}

		public enum InnerEnum
		{
			IMAGE_BASE_DEFAULT,
			IMAGE_BASE_WIN_CE,
			IMAGE_BASE_WIN
		}

		public readonly InnerEnum innerEnumValue;
		private readonly string nameValue;
		private readonly int ordinalValue;
		private static int nextOrdinal = 0;

		private readonly long value;
		private readonly string description;

		internal ImageBaseType(string name, InnerEnum innerEnum, long value, string description)
		{
			this.value = value;
			this.description = description;

			nameValue = name;
			ordinalValue = nextOrdinal++;
			innerEnumValue = innerEnum;
		}

		public static ImageBaseType get(dorkbox.bytes.UInteger key)
		{
			long keyAsLong = key.longValue();

			foreach (ImageBaseType c in values())
			{
				if (keyAsLong == c.value)
				{
					return c;
				}
			}

			return null;
		}

		public string Description
		{
			get
			{
				return this.description;
			}
		}

		public static ImageBaseType[] values()
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

		public static ImageBaseType valueOf(string name)
		{
			foreach (ImageBaseType enumInstance in ImageBaseType.valueList)
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