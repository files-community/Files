// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Drawing;
using System.Drawing.Imaging;
using Windows.Win32;

namespace Files.App.Server.Cloud;

internal sealed record IconFileInfo(byte[] IconData, int Index);

internal static class ServerIconHelper
{
	internal static IList<IconFileInfo> ExtractSelectedIconsFromDLL(string file, IList<int> indexes, int iconSize = 48)
	{
		var result = new List<IconFileInfo>();
		foreach (var index in indexes)
		{
			var hr = PInvoke.SHDefExtractIcon(file, -index, 0, out var large, out var small, (uint)iconSize);
			using var largeIcon = large;
			using var smallIcon = small;
			if (hr.Failed || largeIcon.IsInvalid)
				continue;

			using var icon = (Icon)Icon.FromHandle(largeIcon.DangerousGetHandle()).Clone();
			using var bitmap = icon.ToBitmap();
			using var stream = new MemoryStream();
			bitmap.Save(stream, ImageFormat.Png);
			result.Add(new(stream.ToArray(), index));
		}
		return result;
	}
}
