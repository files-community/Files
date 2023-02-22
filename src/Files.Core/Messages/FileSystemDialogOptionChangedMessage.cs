using CommunityToolkit.Mvvm.Messaging.Messages;
using Files.Core.ViewModels.Dialogs.FileSystemDialog;

namespace Files.Core.Messages
{
	public sealed class FileSystemDialogOptionChangedMessage : ValueChangedMessage<FileSystemDialogConflictItemViewModel>
	{
		public FileSystemDialogOptionChangedMessage(FileSystemDialogConflictItemViewModel value)
			: base(value)
		{
		}
	}
}
