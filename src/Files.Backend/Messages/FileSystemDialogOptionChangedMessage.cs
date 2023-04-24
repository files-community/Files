// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.Messaging.Messages;
using Files.Backend.ViewModels.Dialogs.FileSystemDialog;

namespace Files.Backend.Messages
{
	public sealed class FileSystemDialogOptionChangedMessage : ValueChangedMessage<FileSystemDialogConflictItemViewModel>
	{
		public FileSystemDialogOptionChangedMessage(FileSystemDialogConflictItemViewModel value)
			: base(value)
		{
		}
	}
}
