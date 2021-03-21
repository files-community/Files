using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.ViewModels
{
    public struct ContextMenuFlyoutItemViewModel
    {
        public bool ShowItem { get; set; }
        public RelayCommand Command {get; set;}
        public string Glyph { get; set; }
        public string Text { get; set; }
        public object Tag { get; set; }
        public bool IsSeparator { get; set; }
        public bool IsSubItem { get; set; }
        public List<ContextMenuFlyoutItemViewModel> Items { get; set; }
        public BitmapImage BitmapIcon { get; set; }
        public Action<object, RoutedEventArgs> Click { get; set; }
    }
}
