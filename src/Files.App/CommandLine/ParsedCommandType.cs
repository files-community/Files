// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.CommandLine
{
	internal enum ParsedCommandType
	{
		Unknown,
		OpenDirectory,
		OpenPath,
		ExplorerShellCommand,
		OutputPath,
		SelectItem,
		TagFiles
	}
}
