using System.IO;

namespace Files.Backend.ViewModels.Dialogs.FileSystemDialog
{
    public sealed class FileSystemDialogConflictItemViewModel : BaseFileSystemDialogItemViewModel
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
            set => SetProperty(ref _DestinationPath, value);
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
    }
}
