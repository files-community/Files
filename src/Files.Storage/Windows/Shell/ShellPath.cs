// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.Storage;

public static class ShellPath
{
	/// <summary>Resolves a recycled backing file through the Recycle Bin namespace.</summary>
	public static string GetRecycleBinParsingName(string path)
		=> $@"::{{645FF040-5081-101B-9F08-00AA002F954E}}\{path}";

	public static bool GetStringAsPIDL(string pathOrPIDL, out ShellPidl? pPIDL)
	{
		if (pathOrPIDL.StartsWith(@"\\SHELL\", StringComparison.Ordinal))
		{
			var segments = pathOrPIDL.Replace(@"\\SHELL\", "", StringComparison.Ordinal)
				// Avoid confusion with path separator
				.Replace('_', '/')
				.Split('\\', StringSplitOptions.RemoveEmptyEntries)
				.Select(Convert.FromBase64String);
			pPIDL = ShellPidl.FromSegments(segments);

			return true;
		}
		else
		{
			pPIDL = null;

			return false;
		}
	}

	public static ShellItem Open(string pathOrPIDL)
	{
		return GetStringAsPIDL(pathOrPIDL, out var pPIDL) ? ShellItem.Open(pPIDL!) : ShellItem.Open(pathOrPIDL);
	}
}
