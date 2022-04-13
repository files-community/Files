using CommunityToolkit.Mvvm.Messaging;

namespace Files.Backend.ViewModels.Dialogs.FileSystemDialog
{
    public sealed class FileSystemDialogDefaultItemViewModel : BaseFileSystemDialogItemViewModel
    {
        public FileSystemDialogDefaultItemViewModel(IMessenger messenger)
            : base(messenger)
        {
        }
    }
}
