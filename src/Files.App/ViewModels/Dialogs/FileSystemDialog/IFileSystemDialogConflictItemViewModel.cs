// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

using Files.App.Data.Enums;

namespace Files.App.ViewModels.Dialogs.FileSystemDialog
{
	public interface IFileSystemDialogConflictItemViewModel
	{
		string? SourcePath { get; }

		string? DestinationPath { get; }

		string? CustomName { get; }

		FileNameConflictResolveOptionType ConflictResolveOption { get; }
	}
}
