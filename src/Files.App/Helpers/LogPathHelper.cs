// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Files.App.Helpers
{
	public static class LogPathHelper
	{
		public static string GetPathIdentifier(string? path)
		{
			if (string.IsNullOrEmpty(path))
				return "[Empty]";

			try
			{
				var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(path));

				//4 bytes, still low collision 
				var shortHash = Convert.ToHexStringLower(hashBytes, 0, 4);

				var extension = Path.GetExtension(path);

				return $"[hash:{shortHash}{extension}]";
			}
			catch
			{
				return "[?]";
			}
		}
	}
}
