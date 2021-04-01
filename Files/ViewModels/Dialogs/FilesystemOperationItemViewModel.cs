using Files.Enums;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.ViewModels.Dialogs
{
    public class FilesystemOperationItemViewModel : ObservableObject
    {
        public string OperationIconGlyph { get; set; }

        public string SourcePath { get; set; }

        public string ArrowIconGlyph { get; set; } // Either an arrow or a crossed arrow

        public Visibility PlusIconVisibility { get; set; } // Item will be created - show plus icon

        public string DestinationPath { get; set; }

        public bool IsConflict { get; set; }

        public FilesystemOperationType ItemOperation { get; set; }
    }
}
