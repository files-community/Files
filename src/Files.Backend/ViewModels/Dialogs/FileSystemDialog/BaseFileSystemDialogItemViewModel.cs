using CommunityToolkit.Mvvm.ComponentModel;
using Files.Backend.Models.Imaging;
using System.IO;

namespace Files.Backend.ViewModels.Dialogs.FileSystemDialog
{
    public abstract class BaseFileSystemDialogItemViewModel : ObservableObject
    {
        private string? _SourcePath;
        public virtual string? SourcePath
        {
            get => _SourcePath;
            set
            {
                if (SetProperty(ref _SourcePath, value))
                {
                    OnPropertyChanged(nameof(SourceDirectoryDisplayName));
                }
            }
        }

        private string? _DisplayName;
        public virtual string? DisplayName
        {
            get => _DisplayName;
            set => SetProperty(ref _DisplayName, value);
        }

        private ImageModel? _ItemIcon;
        public ImageModel? ItemIcon
        {
            get => _ItemIcon;
            set => SetProperty(ref _ItemIcon, value);
        }

        public virtual string? SourceDirectoryDisplayName
        {
            get => Path.GetFileName(Path.GetDirectoryName(SourcePath));
        }
    }
}