using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Il modello di elemento Controllo utente è documentato all'indirizzo https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class DataGridHeader : UserControl, INotifyPropertyChanged
    {
        public ICommand Command { get; set; }
        public object CommandParameter { get; set; }

        private string header;

        public string Header
        {
            get { return header; }
            set
            {
                if (value != header)
                {
                    header = value;
                    NotifyPropertyChanged(nameof(Header));
                }
            }
        }

        private bool canBeSorted = true;

        public bool CanBeSorted
        {
            get { return canBeSorted; }
            set
            {
                if (value != canBeSorted)
                {
                    canBeSorted = value;
                    NotifyPropertyChanged(nameof(CanBeSorted));
                }
            }
        }

        private SortDirection? columnSortOption;

        public SortDirection? ColumnSortOption
        {
            get { return columnSortOption; }
            set
            {        
                if (value != columnSortOption)
                {
                    switch (value)
                    {
                        case SortDirection.Ascending:
                            VisualStateManager.GoToState(this, "SortAscending", true);
                            break;
                        case SortDirection.Descending:
                            VisualStateManager.GoToState(this, "SortDescending", true);
                            break;
                        default:
                            VisualStateManager.GoToState(this, "Unsorted", true);
                            break;
                    }
                    columnSortOption = value;
                }
            }
        }

        public DataGridHeader()
        {
            this.InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
