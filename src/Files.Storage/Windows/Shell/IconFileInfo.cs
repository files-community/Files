// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.Storage
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
