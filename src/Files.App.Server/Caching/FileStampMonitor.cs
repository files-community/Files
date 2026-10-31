// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

internal sealed class FileStampMonitor(Func<IEnumerable<string>> getPaths)
{
	private string? previous;

	internal bool HasChanged()
	{
		try
		{
			var stamp = string.Join("|", getPaths().Order(StringComparer.OrdinalIgnoreCase).Select(path =>
			{
				var file = new FileInfo(path);
				return $"{path}:{(file.Exists ? file.LastWriteTimeUtc.Ticks : 0)}:{(file.Exists ? file.Length : 0)}";
			}));
			var changed = stamp != previous;
			previous = stamp;
			return changed;
		}
		catch (IOException)
		{
			return true;
		}
		catch (UnauthorizedAccessException)
		{
			return true;
		}
	}
}
