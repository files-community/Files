using System.IO;

namespace Files.Backend.ViewModels.Dialogs.FileSystemDialog
{
    public sealed class FileSystemDialogDefaultItemViewModel : BaseFileSystemDialogItemViewModel
    {
        public override string? SourcePath
        { 
            get => base.SourcePath; 
            set
            {
                if (base.SourcePath != value)
                {
                    base.SourcePath = value;

                    DisplayName = Path.GetFileName(value);
                }
            }
        }
    }
}
