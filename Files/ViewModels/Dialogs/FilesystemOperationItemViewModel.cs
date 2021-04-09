using Files.Enums;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Windows.UI.Xaml;

namespace Files.ViewModels.Dialogs
{
    public class FilesystemOperationItemViewModel : ObservableObject, IFilesystemOperationItemModel
    {
        private Visibility arrowIconVisibility = Visibility.Visible;
        private FileNameConflictResolveOptionType conflictResolveOption = FileNameConflictResolveOptionType.None;
        private Visibility exclamationMarkVisibility = Visibility.Collapsed;
        private bool isConflicting = false;

        public Visibility ArrowIconVisibility
        {
            get => arrowIconVisibility;
            set => SetProperty(ref arrowIconVisibility, value);
        }

        public FileNameConflictResolveOptionType ConflictResolveOption
        {
            get => conflictResolveOption;
            set
            {
                if (conflictResolveOption != value && IsConflicting)
                {
                    conflictResolveOption = value;
                }
            }
        }

        public string DestinationPath { get; set; }

        public Visibility ExclamationMarkVisibility
        {
            get => exclamationMarkVisibility;
            set => SetProperty(ref exclamationMarkVisibility, value);
        }

        /// <summary>
        /// Determines whether an item is or was a conflicting one
        /// <br/>
        /// <br/>
        /// If the item is no longer a conflicting file name, this property value should NOT be changed.
        /// </summary>
        public bool IsConflicting
        {
            get => isConflicting;
            set
            {
                if (isConflicting != value)
                {
                    isConflicting = value;

                    ExclamationMarkVisibility = isConflicting ? Visibility.Visible : Visibility.Collapsed;
                    ArrowIconVisibility = isConflicting ? Visibility.Collapsed : Visibility.Visible;
                }
            }
        }

        public FilesystemOperationType ItemOperation { get; set; }
        public string OperationIconGlyph { get; set; }

        public Visibility PlusIconVisibility { get; set; }
        public string SourcePath { get; set; }

        // Item will be created - show plus icon
    }
}