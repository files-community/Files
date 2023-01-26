using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Backend.Helpers
{
	public static class PathHelpers
	{
		public static string FormatName(string path = null)
		{
			string fileName;

			// Network Share path
			if (Path.GetPathRoot(path) == path && path.StartsWith(@"\\"))
			{
				fileName = path.Substring(path.LastIndexOf(@"\") + 1);
			}
			// Drive path
			else if (Path.GetPathRoot(path) == path)
			{
				fileName = path;
			}
			else
			{
				fileName = Path.GetFileName(path);
			}

			if (FileExtensionHelpers.IsShortcutOrUrlFile(fileName))
			{
				fileName = fileName.Remove(fileName.Length - 4);
			}

			return fileName;
		}
	}
}
