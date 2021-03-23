using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.ViewModels
{
    public class ContextMenuFlyoutItemViewModel
    {
        public bool ShowItem { get; set; }
        public ICommand Command {get; set; }
        public object CommandParameter { get; set; }
        public string Glyph { get; set; }
        public string Text { get; set; }
        public object Tag { get; set; }
        public ItemType ItemType { get; set; }
        public bool IsSubItem { get; set; }
        public List<ContextMenuFlyoutItemViewModel> Items { get; set; } = new List<ContextMenuFlyoutItemViewModel>();
        public BitmapImage BitmapIcon { get; set; }
        public RoutedEventHandler Click { get; set; }
    }

    public enum ItemType
    {
        Item,
        Separator,
    }
}
