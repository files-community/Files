using Files.Enums;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Files.ViewModels.Dialogs
{
    public class FilesystemOperationItemViewModel : ObservableObject, IFilesystemOperationItemModel
    {
        public string OperationIconGlyph { get; set; }

        public string SourcePath { get; set; }

        public Visibility PlusIconVisibility { get; set; } // Item will be created - show plus icon

        public string DestinationPath { get; set; }

        private bool isConflicting = false;
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

        private Visibility arrowIconVisibility = Visibility.Visible;
        public Visibility ArrowIconVisibility
        {
            get => arrowIconVisibility;
            set => SetProperty(ref arrowIconVisibility, value);
        }

        private Visibility exclamationMarkVisibility = Visibility.Collapsed;
        public Visibility ExclamationMarkVisibility
        {
            get => exclamationMarkVisibility;
            set => SetProperty(ref exclamationMarkVisibility, value);
        }

        private FileNameConflictResolveOptionType conflictResolveOption = FileNameConflictResolveOptionType.None;
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

        public FilesystemOperationType ItemOperation { get; set; }
    }
}
