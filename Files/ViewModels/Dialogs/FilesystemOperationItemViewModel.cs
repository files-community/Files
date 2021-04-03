using Files.Enums;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Files.ViewModels.Dialogs
{
    public class FilesystemOperationItemViewModel : ObservableObject
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

        public FilesystemOperationType ItemOperation { get; set; }
    }
}
