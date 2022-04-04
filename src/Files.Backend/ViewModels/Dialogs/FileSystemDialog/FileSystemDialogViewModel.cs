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

        private string? _Description;
        public string? Description
        {
            get => _Description;
            set => SetProperty(ref _Description, value);
        }

        private bool _DeletePermanentlyVisible;
        public bool DeletePermanentlyVisible
        {
            get => _DeletePermanentlyVisible;
            set => SetProperty(ref _DeletePermanentlyVisible, value);
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

        public FileSystemDialogViewModel()
        {
            _dialogClosingCts = new();
            Items = new();

            PrimaryButtonClickCommand = new RelayCommand(PrimaryButtonClick);
        }

        private void PrimaryButtonClick()
        {
            if (!DeletePermanentlyVisible)
            {

            }
        }

        public void ApplyConflictOptionToAll(FileNameConflictResolveOptionType e)
        {
            foreach (var item in Items)
            {
                if (item is FileSystemDialogConflictItemViewModel conflictItem && !conflictItem.IsActionTaken)
                {
                    conflictItem.TakeAction(e);
                }
            }
        }
    }
}
