// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.Messaging.Messages;
using Files.Core.ViewModels.Dialogs.FileSystemDialog;

namespace Files.Core.Data.Messages
{
	/// <summary>
	/// Represents a messenger for FileSystemDialog option changed.
	/// </summary>
	public sealed class FileSystemDialogOptionChangedMessage
		: ValueChangedMessage<FileSystemDialogConflictItemViewModel>
	{
		/// <summary>
		/// Initializes a class.
		/// </summary>
		public FileSystemDialogOptionChangedMessage(FileSystemDialogConflictItemViewModel value)
			: base(value)
		{
		}
	}
}
