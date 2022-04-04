using CommunityToolkit.Mvvm.ComponentModel;
using Files.Backend.Models.Imaging;

namespace Files.Backend.ViewModels.Dialogs.FileSystemDialog
{
    public abstract class BaseFileSystemDialogItemViewModel : ObservableObject
    {
        private string? _SourcePath;
        public virtual string? SourcePath
        {
            get => _SourcePath;
            set => SetProperty(ref _SourcePath, value);
        }

        private string? _DisplayName;
        public string? DisplayName
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
    }
}
