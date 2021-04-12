using Files.Enums;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Windows.UI.Xaml;

namespace Files.ViewModels.Dialogs
{
    public class FilesystemOperationItemViewModel : ObservableObject, IFilesystemOperationItemModel
    {
        public string OperationIconGlyph { get; set; }

        public string SourcePath { get; set; }

        public string DestinationPath { get; set; }

        public string SourceDirectoryDisplayName
        {
            get => System.IO.Path.GetDirectoryName(SourcePath);
        }

        public string DestinationDirectoryDisplayName
        {
            get => System.IO.Path.GetDirectoryName(DestinationPath);
        }

        public string SourceFileNameDisplayName
        {
            get => System.IO.Path.GetFileName(SourcePath);
        }

        public string DestinationFileNameDisplayName
        {
            get => System.IO.Path.GetFileName(DestinationPath);
        }

        public Visibility DestinationLocationVisibility
        {
            get => string.IsNullOrEmpty(DestinationPath) ? Visibility.Collapsed : Visibility.Visible;
        }

        private Visibility exclamationMarkVisibility = Visibility.Collapsed;

        public Visibility ExclamationMarkVisibility
        {
            get => exclamationMarkVisibility;
            set => SetProperty(ref exclamationMarkVisibility, value);
        }

        private FileNameConflictResolveOptionType conflictResolveOption = FileNameConflictResolveOptionType.NotAConflict;

        public FileNameConflictResolveOptionType ConflictResolveOption
        {
            get => conflictResolveOption;
            set
            {
                if (SetProperty(ref conflictResolveOption, value))
                {
                    ConflictResolveOptionIndex = (int)(uint)conflictResolveOption;
                }
            }
        }

        private int conflictResolveOptionIndex = 0;
        public int ConflictResolveOptionIndex
        {
            get => conflictResolveOptionIndex;
            set
            {
                if (SetProperty(ref conflictResolveOptionIndex, value))
                {
                    ConflictResolveOption = (FileNameConflictResolveOptionType)(uint)conflictResolveOptionIndex;
                }
            }
        }

        public FilesystemOperationType ItemOperation { get; set; }
    }
}