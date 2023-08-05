// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Data.Enums;

namespace Files.Core.ViewModels.Dialogs.FileSystemDialog
{
	public interface IFileSystemDialogConflictItemViewModel
	{
		string? SourcePath { get; }

		string? DestinationPath { get; }

		string? CustomName { get; }

		FileNameConflictResolveOptionType ConflictResolveOption { get; }
	}
}
