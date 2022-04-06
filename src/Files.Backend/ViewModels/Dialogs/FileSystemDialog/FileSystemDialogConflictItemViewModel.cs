using CommunityToolkit.Mvvm.Input;
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

        public override string? SourcePath
        {
            get => base.SourcePath;
            set
            {
                if (base.SourcePath != value)
                {
                    base.SourcePath = value;

                    OnPropertyChanged(nameof(SourceDirectoryDisplayName));
                }
            }
        }

        public string? SourceDirectoryDisplayName
        {
            get => !string.IsNullOrEmpty(DestinationPath) ? Path.GetFileName(Path.GetDirectoryName(SourcePath)) : Path.GetDirectoryName(SourcePath);
        }

        public string DestinationDirectoryDisplayName
        {
            get => Path.GetFileName(Path.GetDirectoryName(DestinationPath));
        }

        private bool _IsActionTaken;
        public bool IsActionTaken
        {
            get => _IsActionTaken;
            set => SetProperty(ref _IsActionTaken, value);
        }

        public FileNameConflictResolveOptionType ConflictResolveOption { get; set; }

        public ICommand GenerateNewNameCommand { get; }

        public ICommand ReplaceExistingCommand { get; }

        public ICommand SkipCommand { get; }

        public FileSystemDialogConflictItemViewModel()
        {
            GenerateNewNameCommand = new RelayCommand(() => TakeAction(FileNameConflictResolveOptionType.GenerateNewName));
            ReplaceExistingCommand = new RelayCommand(() => TakeAction(FileNameConflictResolveOptionType.ReplaceExisting));
            SkipCommand = new RelayCommand(() => TakeAction(FileNameConflictResolveOptionType.Skip));
        }

        public void TakeAction(FileNameConflictResolveOptionType conflictResolveOption)
        {
            IsActionTaken = true;
            ConflictResolveOption = conflictResolveOption;
        }
    }
}
