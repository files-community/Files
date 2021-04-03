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

        public string ArrowIconGlyph { get; set; } // Either an arrow or a crossed arrow

        private Brush arrowIconBrush = new SolidColorBrush();
        public Brush ArrowIconBrush
        {
            get => arrowIconBrush;
            set => SetProperty(ref arrowIconBrush, value);
        }

        public Visibility PlusIconVisibility { get; set; } // Item will be created - show plus icon

        public string DestinationPath { get; set; }

        private bool isConflict = false;
        /// <summary>
        /// Determines whether an item is or was a conflicting one
        /// <br/>
        /// <br/>
        /// If the item is no longer a conflicting file name, this property value should NOT be changed.
        /// </summary>
        public bool IsConflict 
        {
            get => isConflict;
            set
            {
                if (isConflict != value)
                {
                    isConflict = value;

                    ExclamationMarkVisibility = isConflict ? Visibility.Visible : Visibility.Collapsed;
                    ArrowIconBrush = isConflict ? new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)) : (SolidColorBrush)App.Current.Resources["ContentDialogContentFontForegroundColor"];
                }
            }
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
                if (conflictResolveOption != value && IsConflict)
                {
                    conflictResolveOption = value;
                }
            }
        }

        public FilesystemOperationType ItemOperation { get; set; }
    }
}
