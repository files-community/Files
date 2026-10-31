// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

internal static class FileWriteLease
{
	internal static FileStream Acquire(string path)
	{
		var expires = Environment.TickCount64 + 5000;
		while (true)
		{
			try
			{
				return new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
			}
			catch (IOException) when (Environment.TickCount64 < expires)
			{
				Thread.Sleep(20);
			}
		}
	}
}
