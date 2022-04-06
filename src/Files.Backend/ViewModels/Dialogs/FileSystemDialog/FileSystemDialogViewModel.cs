using CommunityToolkit.Mvvm.Input;
using Files.Shared.Enums;
using System.Collections.ObjectModel;
using System.Threading;

namespace Files.Backend.ViewModels.Dialogs.FileSystemDialog
{
    public sealed class FileSystemDialogViewModel : BaseDialogViewModel
    {
        private readonly CancellationTokenSource _dialogClosingCts;

        public ObservableCollection<BaseFileSystemDialogItemViewModel> Items { get; }

        public FileSystemDialogMode FileSystemDialogMode { get; }

        private string? _Description;
        public string? Description
        {
            get => _Description;
            set => SetProperty(ref _Description, value);
        }

        private bool _DeletePermanently;
        public bool DeletePermanently
        {
            get => _DeletePermanently;
            set => SetProperty(ref _DeletePermanently, value);
        }

        private bool _IsDeletePermanentlyEnabled;
        public bool IsDeletePermanentlyEnabled
        {
            get => _IsDeletePermanentlyEnabled;
            set => SetProperty(ref _IsDeletePermanentlyEnabled, value);
        }

        public FileSystemDialogViewModel(FileSystemDialogMode fileSystemDialogMode)
        {
            this.FileSystemDialogMode = fileSystemDialogMode;
            _dialogClosingCts = new();
            Items = new();

            PrimaryButtonClickCommand = new RelayCommand(PrimaryButtonClick);
            SecondaryButtonClickCommand = new RelayCommand(SecondaryButtonClick);
        }

        private void PrimaryButtonClick()
        {
            if (!FileSystemDialogMode.IsInDeleteMode)
            {
                ApplyConflictOptionToAll(FileNameConflictResolveOptionType.GenerateNewName);
            }
        }

        private void SecondaryButtonClick()
        {
            if (FileSystemDialogMode.ConflictsExist)
            {
                foreach (var item in Items)
                {
                    // Don't do anything, skip
                    if (item is FileSystemDialogConflictItemViewModel conflictItem)
                    {
                        conflictItem.ConflictResolveOption = FileNameConflictResolveOptionType.Skip;
                    }
                }
            }
        }

        public void ApplyConflictOptionToAll(FileNameConflictResolveOptionType e)
        {
            if (!FileSystemDialogMode.IsInDeleteMode)
            {
                foreach (var item in Items)
                {
                    if (item is FileSystemDialogConflictItemViewModel conflictItem && !conflictItem.IsActionTaken)
                    {
                        conflictItem.TakeAction(e);
                    }
                }

                PrimaryButtonEnabled = true;
            }
        }
    }

    public sealed class FileSystemDialogMode
    {
        public bool IsInDeleteMode { get; init; }

        public bool ConflictsExist { get; init; }
    }
}
