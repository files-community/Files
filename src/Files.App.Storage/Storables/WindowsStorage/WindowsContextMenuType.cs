// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Storage
{
	[Flags]
	public enum WindowsContextMenuType : uint
	{
		Bitmap = 0x00000004,

		MenuBarBreak = 0x00000020,

		MenuBreak = 0x00000040,

		OwnerDraw = 0x00000100,

		RadioCheck = 0x00000200,

		RightJustify = 0x00004000,

		RightOrder = 0x00002000,

		Separator = 0x00000800,

		String = 0x00000000,
	}
}
