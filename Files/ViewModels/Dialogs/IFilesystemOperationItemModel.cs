using Files.Enums;

namespace Files.ViewModels.Dialogs
{
    public interface IFilesystemOperationItemModel
    {
        FileNameConflictResolveOptionType ConflictResolveOption { get; }
        string DestinationPath { get; }
        bool IsConflicting { get; }
        FilesystemOperationType ItemOperation { get; }
        string SourcePath { get; }
    }
}