using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Files.Backend.Messages;
using Files.Shared.Enums;
using System.IO;
using System.Windows.Input;

namespace Files.Backend.ViewModels.Dialogs.FileSystemDialog
{
    public sealed class FileSystemDialogConflictItemViewModel : BaseFileSystemDialogItemViewModel, IFileSystemDialogConflictItemViewModel
    {
        private string? _DestinationDisplayName;
        public string? DestinationDisplayName
        {
            get => _DestinationDisplayName;
            set => SetProperty(ref _DestinationDisplayName, value);
        }

        private string? _DestinationPath;
        public string? DestinationPath
        {
            get => _DestinationPath;
            set
            {
                if (SetProperty(ref _DestinationPath, value))
                {
                    OnPropertyChanged(nameof(DestinationDirectoryDisplayName));
                }
            }
        }

        public string DestinationDirectoryDisplayName
        {
            get => Path.GetFileName(Path.GetDirectoryName(DestinationPath));
        }

        public bool IsDefault
        {
            get => ConflictResolveOption == FileNameConflictResolveOptionType.GenerateNewName; // Default value
        }

        private FileNameConflictResolveOptionType _ConflictResolveOption;
        public FileNameConflictResolveOptionType ConflictResolveOption
        {
            get => _ConflictResolveOption;
            set
            {
                if (SetProperty(ref _ConflictResolveOption, value))
                {
                    Messenger.Send(new FileSystemDialogOptionChangedMessage(this));
                }
            }
        }

        public FileSystemDialogConflictItemViewModel(IMessenger messenger)
            : base(messenger)
        {
        }
    }
}
