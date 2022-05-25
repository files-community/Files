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
	using UShort = dorkbox.bytes.UShort;

	public sealed class SubsystemType
	{
		public static readonly SubsystemType IMAGE_SYSTEM_UNKNOWN = new SubsystemType("IMAGE_SYSTEM_UNKNOWN", InnerEnum.IMAGE_SYSTEM_UNKNOWN, 0, "unknown subsystem");
		public static readonly SubsystemType IMAGE_SUBSYSTEM_NATIVE = new SubsystemType("IMAGE_SUBSYSTEM_NATIVE", InnerEnum.IMAGE_SUBSYSTEM_NATIVE, 1, "Device drivers and native Windows processes");
		public static readonly SubsystemType IMAGE_SUBSYSTEM_WINDOWS_GUI = new SubsystemType("IMAGE_SUBSYSTEM_WINDOWS_GUI", InnerEnum.IMAGE_SUBSYSTEM_WINDOWS_GUI, 2, "The Windows graphical user interface (GUI) subsystem");
		public static readonly SubsystemType IMAGE_SUBSYSTEM_WINDOWS_CUI = new SubsystemType("IMAGE_SUBSYSTEM_WINDOWS_CUI", InnerEnum.IMAGE_SUBSYSTEM_WINDOWS_CUI, 3, "The Windows character subsystem");
		public static readonly SubsystemType IMAGE_SUBSYSTEM_POSIX_CUI = new SubsystemType("IMAGE_SUBSYSTEM_POSIX_CUI", InnerEnum.IMAGE_SUBSYSTEM_POSIX_CUI, 7, "The Posix character subsystem");
		public static readonly SubsystemType IMAGE_SUBSYSTEM_WINDOWS_CE_GUI = new SubsystemType("IMAGE_SUBSYSTEM_WINDOWS_CE_GUI", InnerEnum.IMAGE_SUBSYSTEM_WINDOWS_CE_GUI, 9, "Windows CE");
		public static readonly SubsystemType IMAGE_SUBSYSTEM_EFI_APPLICATION = new SubsystemType("IMAGE_SUBSYSTEM_EFI_APPLICATION", InnerEnum.IMAGE_SUBSYSTEM_EFI_APPLICATION, 10, "An Extensible Firmware Interface (EFI) application");
		public static readonly SubsystemType IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER = new SubsystemType("IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER", InnerEnum.IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER, 11, "An EFI driver with boot services");
		public static readonly SubsystemType IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER = new SubsystemType("IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER", InnerEnum.IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER, 12, "An EFI driver with run-time services");
		public static readonly SubsystemType IMAGE_SUBSYSTEM_EFI_ROM = new SubsystemType("IMAGE_SUBSYSTEM_EFI_ROM", InnerEnum.IMAGE_SUBSYSTEM_EFI_ROM, 13, "An EFI ROM image");
		public static readonly SubsystemType IMAGE_SUBSYSTEM_XBOX = new SubsystemType("IMAGE_SUBSYSTEM_XBOX", InnerEnum.IMAGE_SUBSYSTEM_XBOX, 14, "XBOX");

		private static readonly List<SubsystemType> valueList = new List<SubsystemType>();

		static SubsystemType()
		{
			valueList.Add(IMAGE_SYSTEM_UNKNOWN);
			valueList.Add(IMAGE_SUBSYSTEM_NATIVE);
			valueList.Add(IMAGE_SUBSYSTEM_WINDOWS_GUI);
			valueList.Add(IMAGE_SUBSYSTEM_WINDOWS_CUI);
			valueList.Add(IMAGE_SUBSYSTEM_POSIX_CUI);
			valueList.Add(IMAGE_SUBSYSTEM_WINDOWS_CE_GUI);
			valueList.Add(IMAGE_SUBSYSTEM_EFI_APPLICATION);
			valueList.Add(IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER);
			valueList.Add(IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER);
			valueList.Add(IMAGE_SUBSYSTEM_EFI_ROM);
			valueList.Add(IMAGE_SUBSYSTEM_XBOX);
		}

		public enum InnerEnum
		{
			IMAGE_SYSTEM_UNKNOWN,
			IMAGE_SUBSYSTEM_NATIVE,
			IMAGE_SUBSYSTEM_WINDOWS_GUI,
			IMAGE_SUBSYSTEM_WINDOWS_CUI,
			IMAGE_SUBSYSTEM_POSIX_CUI,
			IMAGE_SUBSYSTEM_WINDOWS_CE_GUI,
			IMAGE_SUBSYSTEM_EFI_APPLICATION,
			IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER,
			IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER,
			IMAGE_SUBSYSTEM_EFI_ROM,
			IMAGE_SUBSYSTEM_XBOX
		}

		public readonly InnerEnum innerEnumValue;
		private readonly string nameValue;
		private readonly int ordinalValue;
		private static int nextOrdinal = 0;

		private readonly int intValue;
		private readonly string description;

		internal SubsystemType(string name, InnerEnum innerEnum, int intValue, string description)
		{
			this.intValue = intValue;
			this.description = description;

			nameValue = name;
			ordinalValue = nextOrdinal++;
			innerEnumValue = innerEnum;
		}

		public static SubsystemType get(dorkbox.bytes.UShort value)
		{
			int valueAsInt = value.intValue();

			foreach (SubsystemType c in values())
			{
				if (c.intValue == valueAsInt)
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

		public static SubsystemType[] values()
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

		public static SubsystemType valueOf(string name)
		{
			foreach (SubsystemType enumInstance in SubsystemType.valueList)
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