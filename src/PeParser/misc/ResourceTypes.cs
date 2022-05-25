//====================================================================================================
//The Free Edition of Java to C# Converter limits conversion output to 100 lines per file.

//To purchase the Premium Edition, visit our website:
//https://www.tangiblesoftwaresolutions.com/order/order-java-to-csharp.html
//====================================================================================================

using System.Collections.Generic;

/*
 * Copyright 2012 dorkbox, llc
 */
namespace dorkbox.peParser.misc
{
	using UInteger = dorkbox.bytes.UInteger;

	public sealed class ResourceTypes
	{
		public static readonly ResourceTypes N_1 = new ResourceTypes("N_1", InnerEnum.N_1, "???_0");
		public static readonly ResourceTypes CURSOR = new ResourceTypes("CURSOR", InnerEnum.CURSOR, "Cursor");
		public static readonly ResourceTypes BITMAP = new ResourceTypes("BITMAP", InnerEnum.BITMAP, "Bitmap");
		public static readonly ResourceTypes ICON = new ResourceTypes("ICON", InnerEnum.ICON, "Icon");
		public static readonly ResourceTypes MENU = new ResourceTypes("MENU", InnerEnum.MENU, "Menu");
		public static readonly ResourceTypes DLG_BOX = new ResourceTypes("DLG_BOX", InnerEnum.DLG_BOX, "Fialog Box");
		public static readonly ResourceTypes STRING_TABLE_ENTRY = new ResourceTypes("STRING_TABLE_ENTRY", InnerEnum.STRING_TABLE_ENTRY, "String");
		public static readonly ResourceTypes FONT_DIR = new ResourceTypes("FONT_DIR", InnerEnum.FONT_DIR, "Font Directory");
		public static readonly ResourceTypes FONT = new ResourceTypes("FONT", InnerEnum.FONT, "Font");
		public static readonly ResourceTypes ACCEL_TABLE = new ResourceTypes("ACCEL_TABLE", InnerEnum.ACCEL_TABLE, "Accelerators");
		public static readonly ResourceTypes RAW_DATA = new ResourceTypes("RAW_DATA", InnerEnum.RAW_DATA, "application defined resource (raw data)");
		public static readonly ResourceTypes MESSAGE_TABLE_ENTRY = new ResourceTypes("MESSAGE_TABLE_ENTRY", InnerEnum.MESSAGE_TABLE_ENTRY, "Message entry");
		public static readonly ResourceTypes GROUP_CURSOR = new ResourceTypes("GROUP_CURSOR", InnerEnum.GROUP_CURSOR, "Group Cursor");
		public static readonly ResourceTypes N_13 = new ResourceTypes("N_13", InnerEnum.N_13, "???_13");
		public static readonly ResourceTypes GROUP_ICON = new ResourceTypes("GROUP_ICON", InnerEnum.GROUP_ICON, "Group Icon");
		public static readonly ResourceTypes N_15 = new ResourceTypes("N_15", InnerEnum.N_15, "???_15");
		public static readonly ResourceTypes VER_INFO = new ResourceTypes("VER_INFO", InnerEnum.VER_INFO, "Version");
		public static readonly ResourceTypes DLG_INCLUDE = new ResourceTypes("DLG_INCLUDE", InnerEnum.DLG_INCLUDE, "dlginclude");
		public static readonly ResourceTypes N_18 = new ResourceTypes("N_18", InnerEnum.N_18, "???_18");
		public static readonly ResourceTypes PNP_RESOURCE = new ResourceTypes("PNP_RESOURCE", InnerEnum.PNP_RESOURCE, "Plug and Play Resource");
		public static readonly ResourceTypes VXD = new ResourceTypes("VXD", InnerEnum.VXD, "VXD");
		public static readonly ResourceTypes ANIM_CURSOR = new ResourceTypes("ANIM_CURSOR", InnerEnum.ANIM_CURSOR, "Animated Cursor");
		public static readonly ResourceTypes ANIM_ICON = new ResourceTypes("ANIM_ICON", InnerEnum.ANIM_ICON, "Animated Icon");
		public static readonly ResourceTypes HTML = new ResourceTypes("HTML", InnerEnum.HTML, "HTML");
		public static readonly ResourceTypes MANIFEST = new ResourceTypes("MANIFEST", InnerEnum.MANIFEST, "Manifest");

		private static readonly List<ResourceTypes> valueList = new List<ResourceTypes>();

		static ResourceTypes()
		{
			valueList.Add(N_1);
			valueList.Add(CURSOR);
			valueList.Add(BITMAP);
			valueList.Add(ICON);
			valueList.Add(MENU);
			valueList.Add(DLG_BOX);
			valueList.Add(STRING_TABLE_ENTRY);
			valueList.Add(FONT_DIR);
			valueList.Add(FONT);
			valueList.Add(ACCEL_TABLE);
			valueList.Add(RAW_DATA);
			valueList.Add(MESSAGE_TABLE_ENTRY);
			valueList.Add(GROUP_CURSOR);
			valueList.Add(N_13);
			valueList.Add(GROUP_ICON);
			valueList.Add(N_15);
			valueList.Add(VER_INFO);
			valueList.Add(DLG_INCLUDE);
			valueList.Add(N_18);
			valueList.Add(PNP_RESOURCE);
			valueList.Add(VXD);
			valueList.Add(ANIM_CURSOR);
			valueList.Add(ANIM_ICON);
			valueList.Add(HTML);
			valueList.Add(MANIFEST);
		}

		public enum InnerEnum
		{
			N_1,
			CURSOR,
			BITMAP,
			ICON,
			MENU,
			DLG_BOX,
			STRING_TABLE_ENTRY,
			FONT_DIR,
			FONT,
			ACCEL_TABLE,
			RAW_DATA,
			MESSAGE_TABLE_ENTRY,
			GROUP_CURSOR,
			N_13,
			GROUP_ICON,
			N_15,
			VER_INFO,
			DLG_INCLUDE,
			N_18,
			PNP_RESOURCE,
			VXD,
			ANIM_CURSOR,
			ANIM_ICON,
			HTML,
			MANIFEST
		}

		public readonly InnerEnum innerEnumValue;
		private readonly string nameValue;
		private readonly int ordinalValue;
		private static int nextOrdinal = 0;

		private readonly string detailedInfo;

		internal ResourceTypes(string name, InnerEnum innerEnum, string detailedInfo)
		{
			this.detailedInfo = detailedInfo;

			nameValue = name;
			ordinalValue = nextOrdinal++;
			innerEnumValue = innerEnum;
		}

		public string DetailedInfo
		{
			get
			{
				return this.detailedInfo;
			}
		}

		public static ResourceTypes get(dorkbox.bytes.UInteger valueInt)
		{
			int valueAsInt = valueInt.intValue();

			foreach (ResourceTypes t in values())
			{
				if (valueAsInt == t.ordinal())
				{
					return t;
				}
			}

			return null;
		}

		public static ResourceTypes[] values()
		{
			return valueList.ToArray();
		}

		public int ordinal()
		{
			return ordinalValue;
		}
	}
}