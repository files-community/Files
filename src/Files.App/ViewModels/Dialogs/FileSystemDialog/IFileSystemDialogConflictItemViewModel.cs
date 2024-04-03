// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

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
