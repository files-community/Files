// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Items
{
	public class IconFileInfo
	{
		public byte[] IconData { get; }

		public int Index { get; }

		public IconFileInfo(byte[] iconData, int index)
		{
			IconData = iconData;
			Index = index;
		}
	}
}
