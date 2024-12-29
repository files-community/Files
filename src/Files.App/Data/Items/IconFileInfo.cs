// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Items
{
	public sealed class IconFileInfo
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
